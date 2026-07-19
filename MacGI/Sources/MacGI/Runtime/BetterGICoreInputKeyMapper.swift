import Foundation
import CoreGraphics

/// Translates BetterGI Core's semantic key payloads into macOS input codes.
/// This is a platform adapter only; key/mouse execution semantics remain in C# Core.
enum BetterGICoreInputKeyMapper {
    static func keyCode(fromWindowsVirtualKey virtualKey: Int) -> KeyCode? {
        if (0x30...0x39).contains(virtualKey) {
            return KeyCode(rawValue: "digit\(virtualKey - 0x30)")
        }
        if (0x41...0x5A).contains(virtualKey), let scalar = UnicodeScalar(virtualKey) {
            return KeyCode(rawValue: String(Character(scalar)).lowercased())
        }
        if (0x70...0x7B).contains(virtualKey) {
            return KeyCode(rawValue: "f\(virtualKey - 0x6F)")
        }
        return switch virtualKey {
        case 0x08: .backspace; case 0x09: .tab; case 0x0D: .return
        case 0x10, 0xA0, 0xA1: .leftShift
        case 0x11, 0xA2, 0xA3: .leftControl
        case 0x12, 0xA4, 0xA5: .leftOption
        case 0x14: .capsLock; case 0x1B: .escape; case 0x20: .space
        case 0x21: .pageUp; case 0x22: .pageDown; case 0x23: .end; case 0x24: .home
        case 0x25: .leftArrow; case 0x26: .upArrow; case 0x27: .rightArrow; case 0x28: .downArrow
        case 0x2E: .delete
        case 0xBA: .semicolon; case 0xBB: .equal; case 0xBC: .comma; case 0xBD: .minus
        case 0xBE: .period; case 0xBF: .slash; case 0xC0: .grave
        case 0xDB: .leftBracket; case 0xDC: .backslash; case 0xDD: .rightBracket; case 0xDE: .apostrophe
        default: nil
        }
    }

    static func keyCode(from rawKey: String) -> KeyCode? {
        let key = rawKey.trimmingCharacters(in: .whitespacesAndNewlines)
            .replacingOccurrences(of: " ", with: "_").uppercased()
        if key.count == 1, let character = key.first {
            if character >= "A", character <= "Z" { return KeyCode(rawValue: character.lowercased()) }
            if character >= "0", character <= "9" { return KeyCode(rawValue: "digit\(character)") }
        }
        if key.hasPrefix("F"), let number = Int(key.dropFirst()), (1...12).contains(number) {
            return KeyCode(rawValue: "f\(number)")
        }
        return switch key {
        case "VK_ESCAPE", "ESCAPE", "ESC": .escape
        case "VK_RETURN", "RETURN", "ENTER": .return
        case "VK_SPACE", "SPACE": .space
        case "VK_TAB", "TAB": .tab
        case "VK_BACK", "BACKSPACE": .backspace
        case "VK_DELETE", "DELETE", "DEL": .delete
        case "VK_LEFT", "LEFT": .leftArrow
        case "VK_RIGHT", "RIGHT": .rightArrow
        case "VK_UP", "UP": .upArrow
        case "VK_DOWN", "DOWN": .downArrow
        case "VK_SHIFT", "VK_LSHIFT", "SHIFT", "LSHIFT": .leftShift
        case "VK_CONTROL", "VK_LCONTROL", "VK_CTRL", "VK_LCTRL", "CONTROL", "CTRL": .leftControl
        case "VK_MENU", "VK_LMENU", "VK_ALT", "ALT": .leftOption
        case "VK_OEM_COMMA", "COMMA", ",": .comma
        case "VK_OEM_MINUS", "MINUS", "-": .minus
        case "VK_OEM_PLUS", "EQUAL", "=": .equal
        case "VK_OEM_PERIOD", "PERIOD", ".": .period
        case "VK_OEM_2", "SLASH", "/": .slash
        case "VK_OEM_5", "BACKSLASH", "\\": .backslash
        case "VK_OEM_1", "SEMICOLON", ";": .semicolon
        case "VK_OEM_4", "LBRACKET", "[": .leftBracket
        case "VK_OEM_6", "RBRACKET", "]": .rightBracket
        case "VK_OEM_3", "GRAVE", "TILDE", "`": .grave
        default: key.hasPrefix("VK_") ? keyCode(from: String(key.dropFirst(3))) : nil
        }
    }

    static func mouseButton(from rawKey: String) -> CGMouseButton? {
        switch rawKey.trimmingCharacters(in: .whitespacesAndNewlines).uppercased() {
        case "LEFT", "LEFTBUTTON", "MOUSELEFT": .left
        case "RIGHT", "RIGHTBUTTON", "MOUSERIGHT": .right
        case "MIDDLE", "MIDDLEBUTTON", "MOUSEMIDDLE": .center
        case "XBUTTON1", "MOUSE4": CGMouseButton(rawValue: 3)
        case "XBUTTON2", "MOUSE5": CGMouseButton(rawValue: 4)
        default: nil
        }
    }
}
