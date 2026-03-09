using System.Windows;
using GoMuot.Core;
using GoMuot.Services;
using GoMuot.Views;

namespace GoMuot;

/// <summary>
/// GoMuot - Vietnamese Input Method for Windows
/// Main application entry point
/// Matches macOS App.swift flow
/// </summary>
public partial class App : System.Windows.Application
{
    private TrayIcon? _trayIcon;
    private KeyboardHook? _keyboardHook;
    private readonly SettingsService _settings = new();
    private System.Threading.Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }

            RustBridge.Initialize();

            _settings.Load();
            ApplySettings();

            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += OnKeyPressed;
            _keyboardHook.ToggleRequested += OnToggleRequested;
            _keyboardHook.Start();

            _trayIcon = new TrayIcon();
            _trayIcon.OnExitRequested += ExitApplication;
            _trayIcon.OnEnabledChanged += ToggleEnabled;
            _trayIcon.Initialize(_settings.IsEnabled);

            if (_settings.IsFirstRun)
            {
                ShowOnboarding();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"GoMuot khởi động lỗi.\n\n{ex.Message}",
                AppMetadata.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private bool EnsureSingleInstance()
    {
        _mutex = new System.Threading.Mutex(true, "GoMuot_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show(
                $"{AppMetadata.Name} đang chạy.\nKiểm tra khay hệ thống (system tray).",
                AppMetadata.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }
        return true;
    }

    private void ApplySettings()
    {
        _settings.CurrentMethod = InputMethod.Telex;
        RustBridge.SetMethod(InputMethod.Telex);
        RustBridge.SetEnabled(_settings.IsEnabled);
        RustBridge.SetModernTone(_settings.UseModernTone);
        RustBridge.SetSkipWShortcut(true);
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        if (!_settings.IsEnabled) return;
        try
        {
            if (!KeyCodes.TryMapToEngineKey(e.VirtualKeyCode, out ushort engineKeyCode))
            {
                return;
            }

            bool caps = e.Shift || e.CapsLock;
            var result = RustBridge.ProcessKey(engineKeyCode, caps, ctrl: false, shift: e.Shift);

            if (result.Action == ImeAction.None)
            {
                return;
            }

            string? trailingText = null;
            ushort? trailingVirtualKey = null;

            if (KeyCodes.ShouldReplayOriginalBreakKey(e.VirtualKeyCode, e.Shift, result.KeyConsumed))
            {
                if (KeyCodes.TryGetReplayText(e.VirtualKeyCode, e.Shift, out string replayText))
                {
                    trailingText = replayText;
                }
                else if (KeyCodes.TryGetReplayVirtualKey(e.VirtualKeyCode, out ushort replayVirtualKey))
                {
                    trailingVirtualKey = replayVirtualKey;
                }
            }

            e.Handled = true;
            TextSender.SendText(result.GetText(), result.Backspace, trailingText, trailingVirtualKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void ShowOnboarding()
    {
        var onboarding = new OnboardingWindow(_settings);
        onboarding.ShowDialog();

        // Save settings after onboarding
        _settings.IsFirstRun = false;
        _settings.Save();

        ApplySettings();
        _trayIcon?.UpdateState(_settings.IsEnabled);
    }

    private void ToggleEnabled(bool enabled)
    {
        _settings.IsEnabled = enabled;
        _settings.Save();
        RustBridge.SetEnabled(enabled);
        _trayIcon?.UpdateState(enabled);
    }

    private void OnToggleRequested()
    {
        Dispatcher.Invoke(() => ToggleEnabled(!_settings.IsEnabled));
    }

    private void ExitApplication()
    {
        _keyboardHook?.Stop();
        _keyboardHook?.Dispose();
        _trayIcon?.Dispose();
        RustBridge.Clear();
        _mutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _keyboardHook?.Dispose();
        _trayIcon?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
