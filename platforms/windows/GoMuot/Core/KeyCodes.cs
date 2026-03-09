namespace GoMuot.Core;

/// <summary>
/// Windows Virtual Key Codes
/// Maps Windows virtual keys to the Rust core's internal key codes.
/// Reference: https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
/// </summary>
public static class KeyCodes
{
    // Letters A-Z (0x41 - 0x5A)
    public const ushort VK_A = 0x41;
    public const ushort VK_B = 0x42;
    public const ushort VK_C = 0x43;
    public const ushort VK_D = 0x44;
    public const ushort VK_E = 0x45;
    public const ushort VK_F = 0x46;
    public const ushort VK_G = 0x47;
    public const ushort VK_H = 0x48;
    public const ushort VK_I = 0x49;
    public const ushort VK_J = 0x4A;
    public const ushort VK_K = 0x4B;
    public const ushort VK_L = 0x4C;
    public const ushort VK_M = 0x4D;
    public const ushort VK_N = 0x4E;
    public const ushort VK_O = 0x4F;
    public const ushort VK_P = 0x50;
    public const ushort VK_Q = 0x51;
    public const ushort VK_R = 0x52;
    public const ushort VK_S = 0x53;
    public const ushort VK_T = 0x54;
    public const ushort VK_U = 0x55;
    public const ushort VK_V = 0x56;
    public const ushort VK_W = 0x57;
    public const ushort VK_X = 0x58;
    public const ushort VK_Y = 0x59;
    public const ushort VK_Z = 0x5A;

    // Numbers 0-9 (0x30 - 0x39)
    public const ushort VK_0 = 0x30;
    public const ushort VK_1 = 0x31;
    public const ushort VK_2 = 0x32;
    public const ushort VK_3 = 0x33;
    public const ushort VK_4 = 0x34;
    public const ushort VK_5 = 0x35;
    public const ushort VK_6 = 0x36;
    public const ushort VK_7 = 0x37;
    public const ushort VK_8 = 0x38;
    public const ushort VK_9 = 0x39;

    // Special keys
    public const ushort VK_BACK = 0x08;      // Backspace
    public const ushort VK_TAB = 0x09;
    public const ushort VK_RETURN = 0x0D;    // Enter
    public const ushort VK_SHIFT = 0x10;
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_MENU = 0x12;      // Alt
    public const ushort VK_PAUSE = 0x13;
    public const ushort VK_CAPITAL = 0x14;   // Caps Lock
    public const ushort VK_ESCAPE = 0x1B;
    public const ushort VK_SPACE = 0x20;
    public const ushort VK_PRIOR = 0x21;     // Page Up
    public const ushort VK_NEXT = 0x22;      // Page Down
    public const ushort VK_END = 0x23;
    public const ushort VK_HOME = 0x24;
    public const ushort VK_LEFT = 0x25;
    public const ushort VK_UP = 0x26;
    public const ushort VK_RIGHT = 0x27;
    public const ushort VK_DOWN = 0x28;
    public const ushort VK_INSERT = 0x2D;
    public const ushort VK_DELETE = 0x2E;

    // Punctuation (US keyboard layout)
    public const ushort VK_OEM_1 = 0xBA;     // ;:
    public const ushort VK_OEM_PLUS = 0xBB;  // =+
    public const ushort VK_OEM_COMMA = 0xBC; // ,<
    public const ushort VK_OEM_MINUS = 0xBD; // -_
    public const ushort VK_OEM_PERIOD = 0xBE;// .>
    public const ushort VK_OEM_2 = 0xBF;     // /?
    public const ushort VK_OEM_3 = 0xC0;     // `~
    public const ushort VK_OEM_4 = 0xDB;     // [{
    public const ushort VK_OEM_5 = 0xDC;     // \|
    public const ushort VK_OEM_6 = 0xDD;     // ]}
    public const ushort VK_OEM_7 = 0xDE;     // '"

    // Numpad
    public const ushort VK_NUMPAD0 = 0x60;
    public const ushort VK_NUMPAD1 = 0x61;
    public const ushort VK_NUMPAD2 = 0x62;
    public const ushort VK_NUMPAD3 = 0x63;
    public const ushort VK_NUMPAD4 = 0x64;
    public const ushort VK_NUMPAD5 = 0x65;
    public const ushort VK_NUMPAD6 = 0x66;
    public const ushort VK_NUMPAD7 = 0x67;
    public const ushort VK_NUMPAD8 = 0x68;
    public const ushort VK_NUMPAD9 = 0x69;
    public const ushort VK_LSHIFT = 0xA0;
    public const ushort VK_RSHIFT = 0xA1;
    public const ushort VK_LCONTROL = 0xA2;
    public const ushort VK_RCONTROL = 0xA3;
    public const ushort VK_LMENU = 0xA4;
    public const ushort VK_RMENU = 0xA5;
    public const ushort VK_LWIN = 0x5B;
    public const ushort VK_RWIN = 0x5C;

    // Rust core internal key codes (core/src/data/keys.rs)
    private const ushort ENGINE_A = 0;
    private const ushort ENGINE_S = 1;
    private const ushort ENGINE_D = 2;
    private const ushort ENGINE_F = 3;
    private const ushort ENGINE_H = 4;
    private const ushort ENGINE_G = 5;
    private const ushort ENGINE_Z = 6;
    private const ushort ENGINE_X = 7;
    private const ushort ENGINE_C = 8;
    private const ushort ENGINE_V = 9;
    private const ushort ENGINE_B = 11;
    private const ushort ENGINE_Q = 12;
    private const ushort ENGINE_W = 13;
    private const ushort ENGINE_E = 14;
    private const ushort ENGINE_R = 15;
    private const ushort ENGINE_Y = 16;
    private const ushort ENGINE_T = 17;
    private const ushort ENGINE_1 = 18;
    private const ushort ENGINE_2 = 19;
    private const ushort ENGINE_3 = 20;
    private const ushort ENGINE_4 = 21;
    private const ushort ENGINE_6 = 22;
    private const ushort ENGINE_5 = 23;
    private const ushort ENGINE_EQUAL = 24;
    private const ushort ENGINE_9 = 25;
    private const ushort ENGINE_7 = 26;
    private const ushort ENGINE_MINUS = 27;
    private const ushort ENGINE_8 = 28;
    private const ushort ENGINE_0 = 29;
    private const ushort ENGINE_RBRACKET = 30;
    private const ushort ENGINE_O = 31;
    private const ushort ENGINE_U = 32;
    private const ushort ENGINE_LBRACKET = 33;
    private const ushort ENGINE_I = 34;
    private const ushort ENGINE_P = 35;
    private const ushort ENGINE_RETURN = 36;
    private const ushort ENGINE_L = 37;
    private const ushort ENGINE_J = 38;
    private const ushort ENGINE_QUOTE = 39;
    private const ushort ENGINE_K = 40;
    private const ushort ENGINE_SEMICOLON = 41;
    private const ushort ENGINE_BACKSLASH = 42;
    private const ushort ENGINE_COMMA = 43;
    private const ushort ENGINE_SLASH = 44;
    private const ushort ENGINE_N = 45;
    private const ushort ENGINE_M = 46;
    private const ushort ENGINE_DOT = 47;
    private const ushort ENGINE_TAB = 48;
    private const ushort ENGINE_SPACE = 49;
    private const ushort ENGINE_BACKQUOTE = 50;
    private const ushort ENGINE_DELETE = 51;
    private const ushort ENGINE_ESC = 53;
    private const ushort ENGINE_ENTER = 76;
    private const ushort ENGINE_LEFT = 123;
    private const ushort ENGINE_RIGHT = 124;
    private const ushort ENGINE_DOWN = 125;
    private const ushort ENGINE_UP = 126;

    /// <summary>
    /// Check if a key code is a letter (A-Z)
    /// </summary>
    public static bool IsLetter(ushort keyCode) => keyCode >= VK_A && keyCode <= VK_Z;

    /// <summary>
    /// Check if a key code is a number (0-9)
    /// </summary>
    public static bool IsNumber(ushort keyCode) => keyCode >= VK_0 && keyCode <= VK_9;

    /// <summary>
    /// Check if a Windows key can be forwarded to the Rust engine.
    /// </summary>
    public static bool IsRelevantKey(ushort keyCode) => TryMapToEngineKey(keyCode, out _);

    public static bool IsControlKey(ushort keyCode)
    {
        return keyCode == VK_CONTROL ||
               keyCode == VK_LCONTROL ||
               keyCode == VK_RCONTROL;
    }

    public static bool IsAltKey(ushort keyCode)
    {
        return keyCode == VK_MENU ||
               keyCode == VK_LMENU ||
               keyCode == VK_RMENU;
    }

    public static bool ShouldHardResetState(ushort keyCode)
    {
        return keyCode == VK_DELETE ||
               keyCode == VK_INSERT ||
               keyCode == VK_HOME ||
               keyCode == VK_END ||
               keyCode == VK_PRIOR ||
               keyCode == VK_NEXT ||
               keyCode == VK_PAUSE;
    }

    /// <summary>
    /// Break keys whose original behavior should be replayed after a replacement.
    /// Space is excluded because the Rust core already appends it when needed.
    /// </summary>
    public static bool ShouldReplayOriginalBreakKey(ushort keyCode, bool shift, bool keyConsumed)
    {
        if (keyCode == VK_SPACE)
        {
            return false;
        }

        return !keyConsumed && (TryGetReplayText(keyCode, shift, out _) || TryGetReplayVirtualKey(keyCode, out _));
    }

    public static bool TryGetReplayVirtualKey(ushort keyCode, out ushort virtualKey)
    {
        switch (keyCode)
        {
            case VK_RETURN:
                virtualKey = VK_RETURN;
                return true;
            case VK_TAB:
                virtualKey = VK_TAB;
                return true;
            default:
                virtualKey = 0;
                return false;
        }
    }

    public static bool TryGetReplayText(ushort keyCode, bool shift, out string text)
    {
        switch (keyCode)
        {
            case VK_SPACE:
                text = " ";
                return true;
            case VK_OEM_1:
                text = shift ? ":" : ";";
                return true;
            case VK_OEM_PLUS:
                text = shift ? "+" : "=";
                return true;
            case VK_OEM_COMMA:
                text = shift ? "<" : ",";
                return true;
            case VK_OEM_MINUS:
                text = shift ? "_" : "-";
                return true;
            case VK_OEM_PERIOD:
                text = shift ? ">" : ".";
                return true;
            case VK_OEM_2:
                text = shift ? "?" : "/";
                return true;
            case VK_OEM_3:
                text = shift ? "~" : "`";
                return true;
            case VK_OEM_4:
                text = shift ? "{" : "[";
                return true;
            case VK_OEM_5:
                text = shift ? "|" : "\\";
                return true;
            case VK_OEM_6:
                text = shift ? "}" : "]";
                return true;
            case VK_OEM_7:
                text = shift ? "\"" : "'";
                return true;
            default:
                text = string.Empty;
                return false;
        }
    }

    public static bool TryMapToEngineKey(ushort keyCode, out ushort engineKeyCode)
    {
        switch (keyCode)
        {
            case VK_A: engineKeyCode = ENGINE_A; return true;
            case VK_B: engineKeyCode = ENGINE_B; return true;
            case VK_C: engineKeyCode = ENGINE_C; return true;
            case VK_D: engineKeyCode = ENGINE_D; return true;
            case VK_E: engineKeyCode = ENGINE_E; return true;
            case VK_F: engineKeyCode = ENGINE_F; return true;
            case VK_G: engineKeyCode = ENGINE_G; return true;
            case VK_H: engineKeyCode = ENGINE_H; return true;
            case VK_I: engineKeyCode = ENGINE_I; return true;
            case VK_J: engineKeyCode = ENGINE_J; return true;
            case VK_K: engineKeyCode = ENGINE_K; return true;
            case VK_L: engineKeyCode = ENGINE_L; return true;
            case VK_M: engineKeyCode = ENGINE_M; return true;
            case VK_N: engineKeyCode = ENGINE_N; return true;
            case VK_O: engineKeyCode = ENGINE_O; return true;
            case VK_P: engineKeyCode = ENGINE_P; return true;
            case VK_Q: engineKeyCode = ENGINE_Q; return true;
            case VK_R: engineKeyCode = ENGINE_R; return true;
            case VK_S: engineKeyCode = ENGINE_S; return true;
            case VK_T: engineKeyCode = ENGINE_T; return true;
            case VK_U: engineKeyCode = ENGINE_U; return true;
            case VK_V: engineKeyCode = ENGINE_V; return true;
            case VK_W: engineKeyCode = ENGINE_W; return true;
            case VK_X: engineKeyCode = ENGINE_X; return true;
            case VK_Y: engineKeyCode = ENGINE_Y; return true;
            case VK_Z: engineKeyCode = ENGINE_Z; return true;

            case VK_0:
            case VK_NUMPAD0:
                engineKeyCode = ENGINE_0;
                return true;
            case VK_1:
            case VK_NUMPAD1:
                engineKeyCode = ENGINE_1;
                return true;
            case VK_2:
            case VK_NUMPAD2:
                engineKeyCode = ENGINE_2;
                return true;
            case VK_3:
            case VK_NUMPAD3:
                engineKeyCode = ENGINE_3;
                return true;
            case VK_4:
            case VK_NUMPAD4:
                engineKeyCode = ENGINE_4;
                return true;
            case VK_5:
            case VK_NUMPAD5:
                engineKeyCode = ENGINE_5;
                return true;
            case VK_6:
            case VK_NUMPAD6:
                engineKeyCode = ENGINE_6;
                return true;
            case VK_7:
            case VK_NUMPAD7:
                engineKeyCode = ENGINE_7;
                return true;
            case VK_8:
            case VK_NUMPAD8:
                engineKeyCode = ENGINE_8;
                return true;
            case VK_9:
            case VK_NUMPAD9:
                engineKeyCode = ENGINE_9;
                return true;

            case VK_BACK: engineKeyCode = ENGINE_DELETE; return true;
            case VK_TAB: engineKeyCode = ENGINE_TAB; return true;
            case VK_RETURN: engineKeyCode = ENGINE_RETURN; return true;
            case VK_ESCAPE: engineKeyCode = ENGINE_ESC; return true;
            case VK_SPACE: engineKeyCode = ENGINE_SPACE; return true;
            case VK_LEFT: engineKeyCode = ENGINE_LEFT; return true;
            case VK_UP: engineKeyCode = ENGINE_UP; return true;
            case VK_RIGHT: engineKeyCode = ENGINE_RIGHT; return true;
            case VK_DOWN: engineKeyCode = ENGINE_DOWN; return true;

            case VK_OEM_1: engineKeyCode = ENGINE_SEMICOLON; return true;
            case VK_OEM_PLUS: engineKeyCode = ENGINE_EQUAL; return true;
            case VK_OEM_COMMA: engineKeyCode = ENGINE_COMMA; return true;
            case VK_OEM_MINUS: engineKeyCode = ENGINE_MINUS; return true;
            case VK_OEM_PERIOD: engineKeyCode = ENGINE_DOT; return true;
            case VK_OEM_2: engineKeyCode = ENGINE_SLASH; return true;
            case VK_OEM_3: engineKeyCode = ENGINE_BACKQUOTE; return true;
            case VK_OEM_4: engineKeyCode = ENGINE_LBRACKET; return true;
            case VK_OEM_5: engineKeyCode = ENGINE_BACKSLASH; return true;
            case VK_OEM_6: engineKeyCode = ENGINE_RBRACKET; return true;
            case VK_OEM_7: engineKeyCode = ENGINE_QUOTE; return true;

            default:
                engineKeyCode = 0;
                return false;
        }
    }
}
