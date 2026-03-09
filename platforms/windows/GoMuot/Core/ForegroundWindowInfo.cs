using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GoMuot.Core;

internal readonly record struct ForegroundWindowInfo(uint ProcessId, string ProcessName, string WindowTitle)
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

    public bool MentionsClaudeCode =>
        WindowTitle.Contains("claude code", StringComparison.OrdinalIgnoreCase) ||
        WindowTitle.Contains("claude", StringComparison.OrdinalIgnoreCase);

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
        GetWindowThreadProcessId(hwnd, ref processId);

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

        return new ForegroundWindowInfo(
            processId,
            NormalizeProcessName(processName),
            titleBuilder.ToString());
    }

    private static string NormalizeProcessName(string processName)
    {
        return processName.Trim().ToLowerInvariant();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);
}
