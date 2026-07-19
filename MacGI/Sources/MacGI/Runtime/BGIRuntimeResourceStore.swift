import Foundation

struct BGIRuntimeResourceStore: Equatable, Sendable {
    let rootURL: URL

    var userURL: URL { rootURL.appendingPathComponent("User", isDirectory: true) }
    var reposURL: URL { rootURL.appendingPathComponent("Repos", isDirectory: true) }
    var cacheURL: URL { rootURL.appendingPathComponent("Cache", isDirectory: true) }
    var logURL: URL { rootURL.appendingPathComponent("log", isDirectory: true) }
    var assetsURL: URL { rootURL.appendingPathComponent("Assets", isDirectory: true) }
    var runURL: URL { rootURL.appendingPathComponent("Run", isDirectory: true) }
    var downloadCacheURL: URL { cacheURL.appendingPathComponent("Downloads", isDirectory: true) }
    var modelCacheURL: URL { cacheURL.appendingPathComponent("Model", isDirectory: true) }
    var mapsURL: URL { assetsURL.appendingPathComponent("Map", isDirectory: true) }
    var resolvedRootURL: URL { rootURL.resolvingSymlinksInPath() }

    static func defaultStore(fileManager: FileManager = .default) -> BGIRuntimeResourceStore {
        BGIRuntimeResourceStore(rootURL: defaultRootURL(fileManager: fileManager))
    }

    static func defaultRootURL(fileManager: FileManager = .default) -> URL {
        let appSupport = fileManager.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Library/Application Support", isDirectory: true)
        return appSupport.appendingPathComponent("betterGI-mac", isDirectory: true)
    }

    static func defaultSearchRoots(fileManager: FileManager = .default) -> [URL] {
        [defaultRootURL(fileManager: fileManager)]
    }

    func url(forAssetPath path: String, resolvingSymlinks: Bool = false) -> URL {
        let baseURL = resolvingSymlinks ? resolvedRootURL : rootURL
        return baseURL.appendingPathComponent(path.trimmingCharacters(in: CharacterSet(charactersIn: "/")))
    }

    func userScriptGroupURL(for name: String) -> URL {
        userURL.appendingPathComponent("ScriptGroup/\(name).json")
    }

    func createDirectorySkeleton(fileManager: FileManager = .default) throws {
        for directory in requiredDirectories {
            try fileManager.createDirectory(at: directory, withIntermediateDirectories: true)
        }
    }

    var requiredDirectories: [URL] {
        [
            rootURL,
            userURL,
            userURL.appendingPathComponent("JsScript", isDirectory: true),
            userURL.appendingPathComponent("AutoPathing", isDirectory: true),
            userURL.appendingPathComponent("AutoFight", isDirectory: true),
            userURL.appendingPathComponent("AutoGeniusInvokation", isDirectory: true),
            userURL.appendingPathComponent("KeyMouseScript", isDirectory: true),
            userURL.appendingPathComponent("ScriptGroup", isDirectory: true),
            userURL.appendingPathComponent("OneDragon", isDirectory: true),
            userURL.appendingPathComponent("Subscriptions", isDirectory: true),
            userURL.appendingPathComponent("Temp", isDirectory: true),
            userURL
                .appendingPathComponent("Cache", isDirectory: true)
                .appendingPathComponent("MemoryFileCache", isDirectory: true),
            reposURL,
            cacheURL,
            downloadCacheURL,
            modelCacheURL,
            assetsURL,
            mapsURL,
            logURL,
            runURL
        ]
    }
}
