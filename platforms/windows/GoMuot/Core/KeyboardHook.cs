using System.Runtime.InteropServices;

namespace GoMuot.Core;

/// <summary>
/// Low-level Windows keyboard hook for system-wide key interception
/// Similar to CGEventTap on macOS
/// </summary>
public class KeyboardHook : IDisposable
{
    #region Win32 Constants

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint LLKHF_INJECTED = 0x10;

    #endregion

    #region Win32 Imports

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    #endregion

    #region Fields

    private LowLevelKeyboardProc? _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    // Flag to prevent recursive processing of injected keys
    private bool _isProcessing;

    // Once Win+Space toggles the IME, swallow the remaining key sequence so
    // Windows does not open Start or the language switcher on key release.
    private bool _suppressWindowsToggleSequence;

    // Identifier for our injected keys (to skip processing them)
    private static readonly IntPtr InjectedKeyMarker = new IntPtr(0x474E4820); // "GNH " in hex

    #endregion

    #region Events

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;
    public event Action? ToggleRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Start the keyboard hook
    /// </summary>
    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _proc = HookCallback;

        // WH_KEYBOARD_LL callback executes in-process; using IntPtr.Zero avoids
        // module handle issues with single-file published .NET apps.
        _hookId = SetWindowsHookEx(
            WH_KEYBOARD_LL,
            _proc,
            IntPtr.Zero,
            0);

        if (_hookId == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new System.ComponentModel.Win32Exception(error, $"Failed to install keyboard hook. Error: {error}");
        }
    }

    /// <summary>
    /// Stop the keyboard hook
    /// </summary>
    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    #endregion

    #region Private Methods

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Don't process if already processing (prevents recursion)
        if (_isProcessing)
        {
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
        bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

        if (nCode >= 0 && (isKeyDown || isKeyUp))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            // Skip our own injected keys
            if (hookStruct.dwExtraInfo == InjectedKeyMarker)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Skip injected keys from other sources (optional, for safety)
            if ((hookStruct.flags & LLKHF_INJECTED) != 0)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            ushort keyCode = (ushort)hookStruct.vkCode;

            // Never run IME transformations while one of GoMuot's own windows
            // has focus. Otherwise injected replacements are sent back into the
            // app UI itself and can trigger buttons, links, or menu behavior.
            var foreground = ForegroundWindowInfo.Capture();
            if (foreground.IsCurrentProcess)
            {
                RustBridge.ClearAll();
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            if (_suppressWindowsToggleSequence)
            {
                if (keyCode == KeyCodes.VK_SPACE ||
                    keyCode == KeyCodes.VK_LWIN ||
                    keyCode == KeyCodes.VK_RWIN)
                {
                    if (isKeyUp && (keyCode == KeyCodes.VK_LWIN || keyCode == KeyCodes.VK_RWIN))
                    {
                        _suppressWindowsToggleSequence = false;
                    }

                    return (IntPtr)1;
                }

                if (!IsWindowsDown())
                {
                    _suppressWindowsToggleSequence = false;
                }
            }

            if (!isKeyDown)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            bool shift = IsShiftDown();
            bool capsLock = IsCapsLockOn();
            bool ctrl = IsControlDown();
            bool alt = IsAltDown();
            bool windows = IsWindowsDown();

            if (keyCode == KeyCodes.VK_SPACE && windows && !ctrl && !alt && !shift)
            {
                RustBridge.ClearAll();
                _suppressWindowsToggleSequence = true;
                ToggleRequested?.Invoke();
                return (IntPtr)1;
            }

            // Issue #150: Control key alone clears buffer (rhythm break like EVKey)
            if (KeyCodes.IsControlKey(keyCode))
            {
                RustBridge.Clear();
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Cursor movement or editor commands that mutate surrounding text can
            // invalidate both the live buffer and word history.
            if (KeyCodes.ShouldHardResetState(keyCode))
            {
                RustBridge.ClearAll();
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Only process keys that the Rust engine understands.
            if (!KeyCodes.IsRelevantKey(keyCode))
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Skip if Ctrl or Alt is pressed (shortcuts / AltGr)
            if (ctrl || alt)
            {
                RustBridge.ClearAll();
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            var args = new KeyPressedEventArgs(keyCode, shift, capsLock);

            try
            {
                _isProcessing = true;
                KeyPressed?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _isProcessing = false;
            }

            // Block the original key if handled
            if (args.Handled)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool IsControlDown()
    {
        return IsKeyDown(KeyCodes.VK_CONTROL) ||
               IsKeyDown(KeyCodes.VK_LCONTROL) ||
               IsKeyDown(KeyCodes.VK_RCONTROL);
    }

    private static bool IsAltDown()
    {
        return IsKeyDown(KeyCodes.VK_MENU) ||
               IsKeyDown(KeyCodes.VK_LMENU) ||
               IsKeyDown(KeyCodes.VK_RMENU);
    }

    private static bool IsShiftDown()
    {
        return IsKeyDown(KeyCodes.VK_SHIFT) ||
               IsKeyDown(KeyCodes.VK_LSHIFT) ||
               IsKeyDown(KeyCodes.VK_RSHIFT);
    }

    private static bool IsWindowsDown()
    {
        return IsKeyDown(KeyCodes.VK_LWIN) ||
               IsKeyDown(KeyCodes.VK_RWIN);
    }

    private static bool IsKeyDown(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    private static bool IsCapsLockOn()
    {
        return (GetKeyState(KeyCodes.VK_CAPITAL) & 0x0001) != 0;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }

    ~KeyboardHook()
    {
        Dispose(false);
    }

    #endregion

    /// <summary>
    /// Get the marker used to identify injected keys from this application
    /// </summary>
    public static IntPtr GetInjectedKeyMarker() => InjectedKeyMarker;
}

/// <summary>
/// Event args for key press events
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    public ushort VirtualKeyCode { get; }
    public bool Shift { get; }
    public bool CapsLock { get; }
    public bool Handled { get; set; }

    public KeyPressedEventArgs(ushort vkCode, bool shift, bool capsLock)
    {
        VirtualKeyCode = vkCode;
        Shift = shift;
        CapsLock = capsLock;
        Handled = false;
    }
}
