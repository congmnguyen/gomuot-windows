using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GoMuot.Core;

namespace GoMuot.Views;

/// <summary>
/// System tray icon with context menu
/// Matches macOS MenuBarController flow exactly
/// </summary>
public class TrayIcon : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _headerItem;
    private bool _isEnabled = true;
    private bool _disposed;

    #region Events

    public event Action? OnExitRequested;
    public event Action<bool>? OnEnabledChanged;

    #endregion

    /// <summary>
    /// Initialize the system tray icon
    /// </summary>
    public void Initialize(bool isEnabled)
    {
        _isEnabled = isEnabled;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Font = new Font("Consolas", 9F);
        _contextMenu.ShowCheckMargin = true;
        _contextMenu.ShowImageMargin = false;

        // Header with toggle (like macOS)
        _headerItem = new ToolStripMenuItem();
        _headerItem.Enabled = false; // Non-clickable, just display
        _contextMenu.Items.Add(_headerItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        var methodItem = new ToolStripMenuItem("Simple Telex")
        {
            Enabled = false
        };
        _contextMenu.Items.Add(methodItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Toggle enabled
        var toggleItem = new ToolStripMenuItem("Bật/Tắt");
        toggleItem.ShortcutKeyDisplayString = AppMetadata.ToggleHotkeyDisplay;
        toggleItem.Click += (s, e) => ToggleEnabled();
        _contextMenu.Items.Add(toggleItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // About (like macOS "Giới thiệu GoMuot")
        var aboutItem = new ToolStripMenuItem($"Giới thiệu {AppMetadata.Name}");
        aboutItem.Click += (s, e) => ShowAbout();
        _contextMenu.Items.Add(aboutItem);

        // Feedback/Issues (like macOS "Góp ý & Báo lỗi")
        var feedbackItem = new ToolStripMenuItem("Góp ý && Báo lỗi");
        feedbackItem.Click += (s, e) => OpenFeedback();
        _contextMenu.Items.Add(feedbackItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit (like macOS "Thoát")
        var exitItem = new ToolStripMenuItem("Thoát");
        exitItem.ShortcutKeys = Keys.Control | Keys.Q;
        exitItem.Click += (s, e) => OnExitRequested?.Invoke();
        _contextMenu.Items.Add(exitItem);

        // Create tray icon
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        // Double-click to toggle (like macOS)
        _notifyIcon.DoubleClick += (s, e) => ToggleEnabled();

        // Update initial state
        UpdateState(isEnabled);
    }

    /// <summary>
    /// Update tray icon and menu state
    /// </summary>
    public void UpdateState(bool isEnabled)
    {
        _isEnabled = isEnabled;

        // Update header text
        if (_headerItem != null)
        {
            string status = isEnabled ? "ON" : "OFF";
            _headerItem.Text = $"{AppMetadata.Name}  [{status}]";
        }

        UpdateIcon(isEnabled);
        UpdateTooltip(isEnabled);
    }

    private void ToggleEnabled()
    {
        _isEnabled = !_isEnabled;
        UpdateState(_isEnabled);
        OnEnabledChanged?.Invoke(_isEnabled);
    }

    private void UpdateIcon(bool isEnabled)
    {
        if (_notifyIcon == null) return;

        try
        {
            _notifyIcon.Icon = IconHelper.CreateTrayIcon(isEnabled ? "V" : "E", isEnabled);
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }
    }

    private void UpdateTooltip(bool isEnabled)
    {
        if (_notifyIcon == null) return;

        string status = isEnabled ? "Bật" : "Tắt";
        string methodName = InputMethodInfo.GetName(InputMethod.Telex);
        _notifyIcon.Text = $"{AppMetadata.Name} [{methodName}] - {status}";
    }

    private void ShowAbout()
    {
        var about = new AboutWindow();
        about.ShowDialog();
    }

    private void OpenFeedback()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppMetadata.IssuesUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening browser
        }
    }

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
            if (disposing)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }
            _disposed = true;
        }
    }

    ~TrayIcon()
    {
        Dispose(false);
    }

    #endregion
}
