using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GoMuot.Core;

/// <summary>
/// Sends text to the active window using Windows SendInput API
/// Handles backspace deletion and Unicode character insertion
/// </summary>
public static class TextSender
{
    private enum ReplacementMethod
    {
        Backspace,
        EmptyCharPrefix,
        ClipboardPaste
    }

    private enum DispatchMode
    {
        Batched,
        PairSequential,
        EventSequential
    }

    private readonly record struct DispatchProfile(
        ReplacementMethod Method,
        DispatchMode DispatchMode,
        int RetryCount,
        int KeyDelayMs,
        int PhaseDelayMs,
        int ClipboardRestoreDelayMs);

    private const string EmptyCharPrefix = "\u202F";
    private const int ClipboardRetryCount = 5;
    private const int ClipboardRetryDelayMs = 10;

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

        if (profile.Method == ReplacementMethod.ClipboardPaste &&
            TrySendTextWithClipboard(text, backspaces, trailingText, trailingVirtualKey, profile, marker))
        {
            return;
        }

        var prefixInputs = new List<INPUT>();
        var backspaceInputs = new List<INPUT>();
        var textInputs = new List<INPUT>();
        var trailingInputs = new List<INPUT>();

        if (profile.Method == ReplacementMethod.EmptyCharPrefix &&
            backspaces > 0 &&
            !string.IsNullOrEmpty(text))
        {
            AddUnicodeText(prefixInputs, EmptyCharPrefix, marker);
            backspaces += 1;
        }

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

        DispatchInputGroup(prefixInputs, profile);

        if (prefixInputs.Count > 0 && backspaceInputs.Count > 0 && profile.PhaseDelayMs > 0)
        {
            Thread.Sleep(profile.PhaseDelayMs);
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

    private static bool TrySendTextWithClipboard(
        string text,
        int backspaces,
        string? trailingText,
        ushort? trailingVirtualKey,
        DispatchProfile profile,
        IntPtr marker)
    {
        string pasteText = text + (trailingText ?? string.Empty);

        if (!TryCaptureClipboard(out IDataObject? originalClipboard))
        {
            return false;
        }

        bool clipboardUpdated = false;

        try
        {
            var backspaceInputs = new List<INPUT>();
            for (int i = 0; i < backspaces; i++)
            {
                AddVirtualKey(backspaceInputs, KeyCodes.VK_BACK, marker);
            }

            DispatchInputGroup(backspaceInputs, profile);

            // Once backspaces are sent we must not return false — the caller
            // would re-send backspaces and cause double-deletion.  From this
            // point on we always return true regardless of clipboard errors.
            bool backspacesSent = backspaceInputs.Count > 0;

            if (!string.IsNullOrEmpty(pasteText))
            {
                if (backspacesSent && profile.PhaseDelayMs > 0)
                {
                    Thread.Sleep(profile.PhaseDelayMs);
                }

                if (!TrySetClipboardText(pasteText))
                {
                    return backspacesSent;
                }

                clipboardUpdated = true;

                var pasteInputs = new List<INPUT>();
                AddModifiedVirtualKey(pasteInputs, KeyCodes.VK_CONTROL, KeyCodes.VK_V, marker);
                DispatchInputGroup(pasteInputs, profile);
            }

            if (trailingVirtualKey.HasValue)
            {
                if (!string.IsNullOrEmpty(pasteText) && profile.PhaseDelayMs > 0)
                {
                    Thread.Sleep(profile.PhaseDelayMs);
                }

                var trailingInputs = new List<INPUT>();
                AddVirtualKey(trailingInputs, trailingVirtualKey.Value, marker);
                DispatchInputGroup(trailingInputs, profile);
            }

            return true;
        }
        finally
        {
            if (clipboardUpdated)
            {
                if (profile.ClipboardRestoreDelayMs > 0)
                {
                    Thread.Sleep(profile.ClipboardRestoreDelayMs);
                }

                _ = TryRestoreClipboard(originalClipboard);
            }
        }
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

    private static void AddModifiedVirtualKey(List<INPUT> inputs, ushort modifierVirtualKey, ushort virtualKey, IntPtr marker)
    {
        AddKeyEvent(inputs, modifierVirtualKey, false, marker);
        AddKeyEvent(inputs, virtualKey, false, marker);
        AddKeyEvent(inputs, virtualKey, true, marker);
        AddKeyEvent(inputs, modifierVirtualKey, true, marker);
    }

    private static void AddKeyEvent(List<INPUT> inputs, ushort virtualKey, bool keyUp, IntPtr marker)
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
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = marker
                }
            }
        });
    }

    private static DispatchProfile ResolveDispatchProfile()
    {
        var foreground = ForegroundWindowInfo.Capture();

        if (foreground.IsKnownChromiumHost)
        {
            FocusedElementInfo focusedElement = FocusedElementInfo.Capture(foreground.ProcessId);
            if (foreground.IsLikelyChromiumBrowserChromeUi || focusedElement.IsBrowserAutocompleteField)
            {
                return new DispatchProfile(
                    Method: ReplacementMethod.EmptyCharPrefix,
                    DispatchMode: DispatchMode.PairSequential,
                    RetryCount: 4,
                    KeyDelayMs: 2,
                    PhaseDelayMs: 8,
                    ClipboardRestoreDelayMs: 0);
            }
        }

        if (foreground.IsWindowsTerminal)
        {
            return new DispatchProfile(
                Method: ReplacementMethod.ClipboardPaste,
                DispatchMode: DispatchMode.EventSequential,
                RetryCount: 6,
                KeyDelayMs: 12,
                PhaseDelayMs: 30,
                ClipboardRestoreDelayMs: 120);
        }

        if (foreground.IsKnownTerminalHost || foreground.IsKnownEditorHost)
        {
            return new DispatchProfile(
                Method: ReplacementMethod.Backspace,
                DispatchMode: DispatchMode.PairSequential,
                RetryCount: 5,
                KeyDelayMs: 2,
                PhaseDelayMs: 6,
                ClipboardRestoreDelayMs: 0);
        }

        return new DispatchProfile(
            Method: ReplacementMethod.Backspace,
            DispatchMode: DispatchMode.Batched,
            RetryCount: 3,
            KeyDelayMs: 0,
            PhaseDelayMs: 0,
            ClipboardRestoreDelayMs: 0);
    }

    private static void DispatchInputGroup(List<INPUT> inputs, DispatchProfile profile)
    {
        if (inputs.Count == 0)
        {
            return;
        }

        if (profile.DispatchMode == DispatchMode.Batched)
        {
            SendAllWithRetry(inputs.ToArray(), profile.RetryCount);
            return;
        }

        if (profile.DispatchMode == DispatchMode.EventSequential)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                SendAllWithRetry(new[] { inputs[i] }, profile.RetryCount);

                if (profile.KeyDelayMs > 0 && i + 1 < inputs.Count)
                {
                    Thread.Sleep(profile.KeyDelayMs);
                }
            }

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

    private static bool TryCaptureClipboard(out IDataObject? dataObject)
    {
        for (int attempt = 0; attempt <= ClipboardRetryCount; attempt++)
        {
            try
            {
                dataObject = Clipboard.GetDataObject();
                return true;
            }
            catch (ExternalException)
            {
                if (attempt == ClipboardRetryCount)
                {
                    break;
                }

                Thread.Sleep(ClipboardRetryDelayMs);
            }
        }

        dataObject = null;
        return false;
    }

    private static bool TrySetClipboardText(string text)
    {
        for (int attempt = 0; attempt <= ClipboardRetryCount; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch (ExternalException)
            {
                if (attempt == ClipboardRetryCount)
                {
                    break;
                }

                Thread.Sleep(ClipboardRetryDelayMs);
            }
        }

        return false;
    }

    private static bool TryRestoreClipboard(IDataObject? dataObject)
    {
        for (int attempt = 0; attempt <= ClipboardRetryCount; attempt++)
        {
            try
            {
                if (dataObject is null)
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetDataObject(dataObject, true);
                }

                return true;
            }
            catch (ExternalException)
            {
                if (attempt == ClipboardRetryCount)
                {
                    break;
                }

                Thread.Sleep(ClipboardRetryDelayMs);
            }
        }

        return false;
    }
}
