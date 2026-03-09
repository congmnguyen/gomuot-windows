using System.Runtime.InteropServices;

namespace GoMuot.Core;

/// <summary>
/// Sends text to the active window using Windows SendInput API
/// Handles backspace deletion and Unicode character insertion
/// </summary>
public static class TextSender
{
    #region Win32 Constants

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    #endregion

    #region Win32 Imports

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    #endregion

    /// <summary>
    /// Send text replacement: delete characters, insert replacement text,
    /// then optionally replay a trailing key/text that should still appear.
    /// </summary>
    /// <param name="text">Text to insert</param>
    /// <param name="backspaces">Number of backspaces to send first</param>
    public static void SendText(
        string text,
        int backspaces,
        string? trailingText = null,
        ushort? trailingVirtualKey = null)
    {
        if (string.IsNullOrEmpty(text) &&
            backspaces == 0 &&
            string.IsNullOrEmpty(trailingText) &&
            trailingVirtualKey == null)
        {
            return;
        }

        var inputs = new List<INPUT>();
        var marker = KeyboardHook.GetInjectedKeyMarker();

        // Add backspaces
        for (int i = 0; i < backspaces; i++)
        {
            // Key down
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = KeyCodes.VK_BACK,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = marker
                    }
                }
            });

            // Key up
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = KeyCodes.VK_BACK,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = marker
                    }
                }
            });
        }

        AddUnicodeText(inputs, text, marker);

        if (!string.IsNullOrEmpty(trailingText))
        {
            AddUnicodeText(inputs, trailingText, marker);
        }

        if (trailingVirtualKey.HasValue)
        {
            AddVirtualKey(inputs, trailingVirtualKey.Value, marker);
        }

        if (inputs.Count > 0)
        {
            var inputArray = inputs.ToArray();
            int inputSize = Marshal.SizeOf<INPUT>();
            uint sent = SendInput((uint)inputArray.Length, inputArray, inputSize);
            if (sent != inputArray.Length)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine(
                    $"SendInput partial failure sent={sent}/{inputArray.Length} error={error} inputSize={inputSize}");
            }
        }
    }

    private static void AddUnicodeText(List<INPUT> inputs, string text, IntPtr marker)
    {
        foreach (char c in text)
        {
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = marker
                    }
                }
            });
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = marker
                    }
                }
            });
        }
    }

    private static void AddVirtualKey(List<INPUT> inputs, ushort virtualKey, IntPtr marker)
    {
        inputs.Add(new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = marker
                }
            }
        });

        inputs.Add(new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = marker
                }
            }
        });
    }
}
