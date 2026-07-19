// swift-tools-version: 6.1

import PackageDescription

let package = Package(
    name: "betterGI-mac",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .executable(name: "betterGI-mac", targets: ["MacGI"])
    ],
    dependencies: [],
    targets: [
        .executableTarget(
            name: "MacGI",
            dependencies: [],
            path: "Sources/MacGI",
            resources: [
                .copy("Resources")
            ],
            linkerSettings: [
            ]
        ),
        .testTarget(
            name: "MacGITests",
            dependencies: ["MacGI"],
            path: "Tests/MacGITests"
        )
    ]
)
