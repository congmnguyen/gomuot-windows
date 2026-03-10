using System.Runtime.InteropServices;
using System.Text;

namespace GoMuot.Core;

internal readonly record struct FocusedElementInfo(
    uint ProcessId,
    string Name,
    string ClassName)
{
    private static readonly object CacheLock = new();
    private static FocusedElementInfo _cached;
    private static uint _cachedProcessId;
    private static long _expiresAt;

    public bool IsBrowserAutocompleteField =>
        ClassName.StartsWith("Chrome_AutocompleteEditView", StringComparison.OrdinalIgnoreCase) ||
        MatchesAutocompleteHint(Name, ClassName);

    public static FocusedElementInfo Capture(uint expectedProcessId)
    {
        if (expectedProcessId == 0)
        {
            return default;
        }

        long now = Environment.TickCount64;
        lock (CacheLock)
        {
            if (_cachedProcessId == expectedProcessId && now < _expiresAt)
            {
                return _cached;
            }
        }

        FocusedElementInfo snapshot = default;
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return default;
            }

            uint processId = 0;
            uint threadId = GetWindowThreadProcessId(foregroundWindow, ref processId);
            if (processId != expectedProcessId)
            {
                return default;
            }

            IntPtr target = foregroundWindow;
            if (threadId != 0)
            {
                var info = new GUITHREADINFO
                {
                    cbSize = Marshal.SizeOf<GUITHREADINFO>()
                };

                if (GetGUIThreadInfo(threadId, ref info))
                {
                    if (info.hwndFocus != IntPtr.Zero)
                    {
                        target = info.hwndFocus;
                    }
                    else if (info.hwndActive != IntPtr.Zero)
                    {
                        target = info.hwndActive;
                    }
                }
            }

            if (target == IntPtr.Zero)
            {
                return default;
            }

            snapshot = new FocusedElementInfo(
                processId,
                GetWindowText(target),
                GetClassName(target));
        }
        catch
        {
            snapshot = default;
        }

        lock (CacheLock)
        {
            _cachedProcessId = expectedProcessId;
            _cached = snapshot;
            _expiresAt = now + 150;
        }

        return snapshot;
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        int length = GetWindowTextLengthW(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = GetWindowTextW(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        return GetClassNameW(hwnd, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    private static bool MatchesAutocompleteHint(params string[] values)
    {
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Contains("address", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("search", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("omnibox", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
}
