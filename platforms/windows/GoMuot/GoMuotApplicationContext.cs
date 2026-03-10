using System;
using System.Threading;
using System.Windows.Forms;
using GoMuot.Core;
using GoMuot.Services;
using GoMuot.Views;

namespace GoMuot;

internal sealed class GoMuotApplicationContext : ApplicationContext
{
    private TrayIcon? _trayIcon;
    private KeyboardHookHost? _keyboardHookHost;
    private readonly SettingsService _settings = new();
    private Mutex? _mutex;
    private System.Windows.Forms.Timer? _startupTimer;
    private SynchronizationContext? _syncContext;

    public GoMuotApplicationContext()
    {
        _startupTimer = new System.Windows.Forms.Timer
        {
            Interval = 1
        };
        _startupTimer.Tick += OnStartupTick;
        _startupTimer.Start();
    }

    private void OnStartupTick(object? sender, EventArgs e)
    {
        _startupTimer?.Stop();
        _startupTimer?.Dispose();
        _startupTimer = null;

        _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        try
        {
            if (!EnsureSingleInstance())
            {
                ExitThread();
                return;
            }

            RustBridge.Initialize();

            _settings.Load();
            ApplySettings();

            _keyboardHookHost = new KeyboardHookHost();
            _keyboardHookHost.KeyPressed += OnKeyPressed;
            _keyboardHookHost.ToggleRequested += OnToggleRequested;
            _keyboardHookHost.Start();

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
            MessageBox.Show(
                $"GoMuot khởi động lỗi.\n\n{ex.Message}",
                AppMetadata.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ExitThread();
        }
    }

    private bool EnsureSingleInstance()
    {
        _mutex = new Mutex(true, "GoMuot_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                $"{AppMetadata.Name} đang chạy.\nKiểm tra khay hệ thống (system tray).",
                AppMetadata.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
        if (!_settings.IsEnabled)
        {
            return;
        }

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

            string replacementText = result.GetText();
            e.Handled = true;

            // Run SendInput after the low-level hook callback returns. Some
            // Chromium-based surfaces appear to drop or reorder synthesized
            // input when it is emitted inline from the hook.
            if (!(_keyboardHookHost?.TryPost(() =>
                    TextSender.SendText(replacementText, result.Backspace, trailingText, trailingVirtualKey)) ?? false))
            {
                TextSender.SendText(replacementText, result.Backspace, trailingText, trailingVirtualKey);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void ShowOnboarding()
    {
        using var onboarding = new OnboardingForm(_settings);
        onboarding.ShowDialog();

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
        SynchronizationContext syncContext = _syncContext ?? new WindowsFormsSynchronizationContext();
        syncContext.Post(_ => ToggleEnabled(!_settings.IsEnabled), null);
    }

    private void ExitApplication()
    {
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _startupTimer?.Stop();
        _startupTimer?.Dispose();
        if (_keyboardHookHost is not null)
        {
            _keyboardHookHost.KeyPressed -= OnKeyPressed;
            _keyboardHookHost.ToggleRequested -= OnToggleRequested;
            _keyboardHookHost.Dispose();
            _keyboardHookHost = null;
        }
        _trayIcon?.Dispose();
        RustBridge.Clear();
        _mutex?.Dispose();
        base.ExitThreadCore();
    }
}
