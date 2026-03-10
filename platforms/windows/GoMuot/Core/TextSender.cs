using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GoMuot.Core;

/// <summary>
/// Sends text to the active window using Windows SendInput API
/// Handles backspace deletion and Unicode character insertion
/// </summary>
public static class TextSender
{
    private readonly record struct DispatchProfile(bool Sequential, int RetryCount, int KeyDelayMs, int PhaseDelayMs);

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

        var marker = KeyboardHook.GetInjectedKeyMarker();
        var profile = ResolveDispatchProfile();

        var backspaceInputs = new List<INPUT>();
        var textInputs = new List<INPUT>();
        var trailingInputs = new List<INPUT>();

        for (int i = 0; i < backspaces; i++)
        {
            AddVirtualKey(backspaceInputs, KeyCodes.VK_BACK, marker);
        }

        AddUnicodeText(textInputs, text, marker);

        if (!string.IsNullOrEmpty(trailingText))
        {
            AddUnicodeText(trailingInputs, trailingText, marker);
        }

        if (trailingVirtualKey.HasValue)
        {
            AddVirtualKey(trailingInputs, trailingVirtualKey.Value, marker);
        }

        DispatchInputGroup(backspaceInputs, profile);

        if (backspaceInputs.Count > 0 && (textInputs.Count > 0 || trailingInputs.Count > 0) && profile.PhaseDelayMs > 0)
        {
            Thread.Sleep(profile.PhaseDelayMs);
        }

        DispatchInputGroup(textInputs, profile);

        if (textInputs.Count > 0 && trailingInputs.Count > 0 && profile.PhaseDelayMs > 0)
        {
            Thread.Sleep(profile.PhaseDelayMs);
        }

        DispatchInputGroup(trailingInputs, profile);
    }

    private static void AddUnicodeText(List<INPUT> inputs, string text, IntPtr marker)
    {
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (rune.IsBmp)
            {
                AddUnicodeChar(inputs, (char)rune.Value, marker);
                continue;
            }

            foreach (char surrogate in rune.ToString())
            {
                AddUnicodeChar(inputs, surrogate, marker);
            }
        }
    }

    private static void AddUnicodeChar(List<INPUT> inputs, char c, IntPtr marker)
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

    private static DispatchProfile ResolveDispatchProfile()
    {
        var foreground = ForegroundWindowInfo.Capture();

        if (foreground.IsKnownTerminalHost)
        {
            return new DispatchProfile(
                Sequential: true,
                RetryCount: 5,
                KeyDelayMs: 2,
                PhaseDelayMs: 6);
        }

        if (foreground.IsKnownEditorHost && foreground.MentionsClaudeCode)
        {
            return new DispatchProfile(
                Sequential: true,
                RetryCount: 4,
                KeyDelayMs: 2,
                PhaseDelayMs: 4);
        }

        return new DispatchProfile(
            Sequential: false,
            RetryCount: 3,
            KeyDelayMs: 0,
            PhaseDelayMs: 0);
    }

    private static void DispatchInputGroup(List<INPUT> inputs, DispatchProfile profile)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        if (!profile.Sequential)
        {
            SendAllWithRetry(inputs.ToArray(), profile.RetryCount);
            return;
        }

        // Every action is emitted as a down/up pair. Sending those pairs one by
        // one with a small delay is slower, but terminal-style apps are less
        // likely to drop or reorder them than a large batched SendInput call.
        for (int i = 0; i < inputs.Count; i += 2)
        {
            int packetSize = Math.Min(2, inputs.Count - i);
            var packet = new INPUT[packetSize];
            inputs.CopyTo(i, packet, 0, packetSize);
            SendAllWithRetry(packet, profile.RetryCount);

            if (profile.KeyDelayMs > 0 && i + packetSize < inputs.Count)
            {
                Thread.Sleep(profile.KeyDelayMs);
            }
        }
    }

    private static void SendAllWithRetry(INPUT[] inputs, int retryCount)
    {
        if (inputs.Length == 0)
        {
            return;
        }

        int inputSize = Marshal.SizeOf<INPUT>();
        int offset = 0;
        int attempt = 0;

        while (offset < inputs.Length && attempt <= retryCount)
        {
            int remainingCount = inputs.Length - offset;
            var remaining = new INPUT[remainingCount];
            Array.Copy(inputs, offset, remaining, 0, remainingCount);

            uint sent = SendInput((uint)remaining.Length, remaining, inputSize);
            if (sent == remaining.Length)
            {
                return;
            }

            if (sent > 0)
            {
                offset += (int)sent;
            }

            int error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine(
                $"SendInput partial failure sent={sent}/{remaining.Length} error={error} attempt={attempt + 1}/{retryCount + 1}");

            attempt++;
            if (attempt <= retryCount)
            {
                Thread.Sleep(2);
            }
        }
    }
}
