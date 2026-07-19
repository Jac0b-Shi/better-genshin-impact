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
    dependencies: [
        .package(url: "https://github.com/microsoft/onnxruntime-swift-package-manager", exact: "1.24.2")
    ],
    targets: [
        .executableTarget(
            name: "MacGI",
            dependencies: [
                .product(name: "onnxruntime", package: "onnxruntime-swift-package-manager")
            ],
            path: "Sources/MacGI",
            exclude: [
                // Historical Swift task translations are deliberately excluded from the App.
                // BetterGI C# Core is the only production owner of these business decisions.
                "Runtime/BGIAutoArtifactSalvageService.swift",
                "Runtime/BGIAutoBossAndDomainService.swift",
                "Runtime/BGIAutoEatService.swift",
                "Runtime/BGIAutoFightService.swift",
                "Runtime/BGIAutoFishingService.swift",
                "Runtime/BGIAutoLeyLineOutcropService.swift",
                "Runtime/BGIAutoMusicAndWoodService.swift",
                "Runtime/BGIAutoOpenChestService.swift",
                "Runtime/BGIAutoPickService.swift",
                "Runtime/BGICameraRotateService.swift",
                "Runtime/BGIChooseTalkOptionService.swift",
                "Runtime/BGIExitAndReloginService.swift",
                "Runtime/BGIPartySwitchService.swift",
                "Runtime/BGIReturnMainUIService.swift",
                "Runtime/BGISetTimeService.swift",
                "Runtime/BGISmallTaskServices.swift"
            ],
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
