using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GoMuot.Core;

internal readonly record struct ForegroundWindowInfo(
    uint ProcessId,
    string ProcessName,
    string WindowTitle,
    string FocusedClassName)
{
    public bool IsKnownTerminalHost =>
        ProcessName is "windowsterminal" or
            "openconsole" or
            "conhost" or
            "wezterm" or
            "wezterm-gui" or
            "conemu64" or
            "mintty" or
            "alacritty";

    public bool IsKnownEditorHost =>
        ProcessName is "code" or
            "code - insiders" or
            "cursor" or
            "windsurf" or
            "vscodium";

    public bool IsWindowsTerminal =>
        ProcessName is "windowsterminal" or "openconsole";

    public bool IsKnownChromiumHost =>
        ProcessName is "brave" or
            "chrome" or
            "chromium" or
            "msedge" or
            "vivaldi" or
            "opera";

    public bool IsLikelyChromiumBrowserChromeUi =>
        IsKnownChromiumHost &&
        !string.IsNullOrEmpty(FocusedClassName) &&
        !FocusedClassName.Equals("Chrome_RenderWidgetHostHWND", StringComparison.OrdinalIgnoreCase) &&
        (FocusedClassName.StartsWith("Chrome_WidgetWin_", StringComparison.OrdinalIgnoreCase) ||
         FocusedClassName.StartsWith("Chrome_AutocompleteEditView", StringComparison.OrdinalIgnoreCase));

    public bool IsCurrentProcess =>
        ProcessId != 0 && ProcessId == (uint)Environment.ProcessId;

    public static ForegroundWindowInfo Capture()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return default;
        }

        uint processId = 0;
        uint threadId = GetWindowThreadProcessId(hwnd, ref processId);

        string processName = string.Empty;
        if (processId != 0)
        {
            try
            {
                processName = Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                processName = string.Empty;
            }
        }

        int length = GetWindowTextLengthW(hwnd);
        var titleBuilder = new StringBuilder(length + 1);
        _ = GetWindowTextW(hwnd, titleBuilder, titleBuilder.Capacity);

        string focusedClassName = CaptureFocusedClassName(threadId, hwnd);

        return new ForegroundWindowInfo(
            processId,
            NormalizeProcessName(processName),
            titleBuilder.ToString(),
            focusedClassName);
    }

    private static string NormalizeProcessName(string processName)
    {
        return processName.Trim().ToLowerInvariant();
    }

    private static string CaptureFocusedClassName(uint threadId, IntPtr foregroundWindow)
    {
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
            return string.Empty;
        }

        var builder = new StringBuilder(256);
        return GetClassNameW(target, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
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
