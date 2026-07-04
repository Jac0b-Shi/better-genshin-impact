#!/bin/bash
set -euo pipefail

# build-opencvsharp-extern-macos-arm64.sh
# Reproducible build of libOpenCvSharpExtern.dylib for macOS arm64.
# Must pass: fresh git clone -> ./scripts/build-opencvsharp-extern-macos-arm64.sh -> 15/15 assertions.
#
# Prerequisites: brew install cmake opencv
# OpenCvSharp must match NuGet package version in BetterGenshinImpact.Core.csproj.

OPENCVSHARP_TAG="4.11.0.20250507"
OPENCVSHARP_COMMIT="2360cafffe47273dc659fb45dbb5bf07bdd65f85"
REQUIRED_OPENCV_MAJOR=4
REQUIRED_OPENCV_MINOR=13

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="$REPO_ROOT/.build/opencvsharp-extern"
PATCH_FILE="$SCRIPT_DIR/patches/opencvsharp-4.11-opencv-4.13-subdiv2d.patch"
ARTIFACTS_DIR="$REPO_ROOT/artifacts/native/osx-arm64"
VERIF_PROJ="$REPO_ROOT/Test/BetterGenshinImpact.Core.Verification/BetterGenshinImpact.Core.Verification.csproj"

echo "=== OpenCvSharpExtern macOS arm64 Builder ==="
echo ""

# ==== Preflight ====
if [ "$(uname -m)" != "arm64" ]; then
    echo "ERROR: This script must run on arm64 macOS." >&2; exit 1
fi

for cmd in cmake git; do
    if ! command -v "$cmd" &>/dev/null; then
        echo "ERROR: '$cmd' not found." >&2; exit 1
    fi
done

OPENCV_PREFIX="$(brew --prefix opencv 2>/dev/null || true)"
if [ -z "$OPENCV_PREFIX" ] || [ ! -d "$OPENCV_PREFIX" ]; then
    echo "ERROR: Homebrew opencv not installed. Run: brew install opencv" >&2; exit 1
fi

OPENCV_VERSION="$(pkg-config --modversion opencv4 2>/dev/null || echo "0")"
OPENCV_MAJOR=$(echo "$OPENCV_VERSION" | cut -d. -f1)
OPENCV_MINOR=$(echo "$OPENCV_VERSION" | cut -d. -f2)
echo "  arm64:       OK"
echo "  OpenCV:      $OPENCV_PREFIX  (version $OPENCV_VERSION)"
if [ "$OPENCV_MAJOR" != "$REQUIRED_OPENCV_MAJOR" ] || [ "$OPENCV_MINOR" != "$REQUIRED_OPENCV_MINOR" ]; then
    echo "ERROR: OpenCV version $OPENCV_VERSION does not match required $REQUIRED_OPENCV_MAJOR.$REQUIRED_OPENCV_MINOR.x" >&2
    echo "       Compatibility matrix is only confirmed for OpenCV 4.13.x." >&2
    exit 1
fi

# ==== Clone / checkout OpenCvSharp ====
mkdir -p "$BUILD_DIR"
OPENCVSHARP_SRC="$BUILD_DIR/src"
if [ ! -d "$OPENCVSHARP_SRC/.git" ]; then
    echo "  Cloning OpenCvSharp tag $OPENCVSHARP_TAG ..."
    git clone --depth 1 --branch "$OPENCVSHARP_TAG" \
        https://github.com/shimat/opencvsharp.git "$OPENCVSHARP_SRC"
else
    echo "  OpenCvSharp source exists, fetching tag $OPENCVSHARP_TAG ..."
    git -C "$OPENCVSHARP_SRC" fetch --depth 1 origin tag "$OPENCVSHARP_TAG" 2>/dev/null || true
    git -C "$OPENCVSHARP_SRC" checkout --force "$OPENCVSHARP_TAG"
fi

ACTUAL_COMMIT=$(git -C "$OPENCVSHARP_SRC" rev-parse HEAD)
if [ "$ACTUAL_COMMIT" != "$OPENCVSHARP_COMMIT" ]; then
    echo "ERROR: commit mismatch. Expected $OPENCVSHARP_COMMIT, got $ACTUAL_COMMIT" >&2
    echo "       The tag may have been moved or the clone is corrupt." >&2
    exit 1
fi
echo "  commit:      $ACTUAL_COMMIT (verified)"

# ==== Apply patch ====
if [ -f "$PATCH_FILE" ]; then
    echo "  Applying patch: $(basename "$PATCH_FILE")"
    if git -C "$OPENCVSHARP_SRC" apply --reverse --check "$PATCH_FILE" 2>/dev/null; then
        echo "    (already applied)"
    elif git -C "$OPENCVSHARP_SRC" apply --check "$PATCH_FILE" 2>/dev/null; then
        git -C "$OPENCVSHARP_SRC" apply "$PATCH_FILE"
        echo "    applied OK"
    else
        echo "ERROR: patch cannot be applied cleanly. Check OpenCV version compatibility." >&2
        exit 1
    fi
else
    echo "  No patch file at $PATCH_FILE — skipping"
fi

# ==== Configure & Build ====
CMAKE_BUILD_DIR="$BUILD_DIR/build"
echo "  Configuring CMake ..."
cmake -S "$OPENCVSHARP_SRC/src/OpenCvSharpExtern" \
    -B "$CMAKE_BUILD_DIR" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -DOpenCV_DIR="$OPENCV_PREFIX/lib/cmake/opencv4"

echo "  Building ..."
cmake --build "$CMAKE_BUILD_DIR" --config Release -j"$(sysctl -n hw.logicalcpu)"

DYLIB="$CMAKE_BUILD_DIR/libOpenCvSharpExtern.dylib"
if [ ! -f "$DYLIB" ]; then
    echo "ERROR: Build did not produce $DYLIB" >&2; exit 1
fi

# ==== Verify dylib ====
echo "  Verifying dylib ..."
file "$DYLIB"

echo "  Dependencies (full):"
otool -L "$DYLIB"

if otool -L "$DYLIB" | grep -q '/opt/homebrew/Cellar/'; then
    echo "ERROR: dylib contains Homebrew Cellar paths — must use /opt/homebrew/opt/ symlinks" >&2
    exit 1
fi
echo "  (no Cellar hardcodes — OK)"

echo "  rpath headers:"
otool -l "$DYLIB" | grep -A3 LC_RPATH

# ==== Deploy ====
echo "  Deploying to artifacts ..."
mkdir -p "$ARTIFACTS_DIR"
cp "$DYLIB" "$ARTIFACTS_DIR/"
echo "  artifact:    $ARTIFACTS_DIR/libOpenCvSharpExtern.dylib"

# ==== Run Verification ====
echo ""
echo "=== Building Verification ==="
export DOTNET_ROOT="/usr/local/share/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
cd "$REPO_ROOT"
dotnet build "$VERIF_PROJ" -c Debug --nologo

# Copy dylib AFTER build (build may recreate output dirs)
# Resolve the actual build output path (dotnet may vary)
BUILD_OUT_BASE="$REPO_ROOT/Test/BetterGenshinImpact.Core.Verification/bin/Debug/net8.0"
NATIVE_RUNTIME="$BUILD_OUT_BASE/runtimes/osx-arm64/native"
mkdir -p "$NATIVE_RUNTIME"
cp "$DYLIB" "$NATIVE_RUNTIME/"
echo "  runtime:     $NATIVE_RUNTIME/libOpenCvSharpExtern.dylib"

# Verify dylib is where OpenCvSharp expects it
if [ ! -f "$NATIVE_RUNTIME/libOpenCvSharpExtern.dylib" ]; then
    echo "ERROR: Failed to copy dylib to $NATIVE_RUNTIME" >&2; exit 1
fi
echo "  verified:    $(file "$NATIVE_RUNTIME/libOpenCvSharpExtern.dylib" | cut -d: -f2)"

echo ""
echo "=== Running Verification ==="
export DYLD_LIBRARY_PATH="$NATIVE_RUNTIME:${DYLD_LIBRARY_PATH:-}"
dotnet run --project "$VERIF_PROJ" --no-build --no-launch-profile
EXIT=$?

echo ""
echo "=== Complete (exit code $EXIT) ==="
exit $EXIT
