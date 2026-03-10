using System.Threading;
using System.Windows.Forms;

namespace GoMuot.Core;

internal sealed class KeyboardHookHost : IDisposable
{
    private readonly ManualResetEventSlim _started = new(false);
    private readonly ManualResetEventSlim _stopped = new(true);
    private Thread? _thread;
    private SynchronizationContext? _syncContext;
    private KeyboardHook? _keyboardHook;
    private Exception? _startupException;
    private bool _disposed;

    public event EventHandler<KeyPressedEventArgs>? KeyPressed;
    public event Action? ToggleRequested;

    public void Start()
    {
        ThrowIfDisposed();
        if (_thread is not null)
        {
            return;
        }

        _startupException = null;
        _started.Reset();
        _stopped.Reset();

        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "GoMuot Keyboard Hook"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        _started.Wait();

        if (_startupException is not null)
        {
            Thread? thread = _thread;
            _thread = null;
            thread?.Join(TimeSpan.FromSeconds(1));
            throw new InvalidOperationException("Failed to start keyboard hook host.", _startupException);
        }
    }

    public void Stop()
    {
        if (_thread is null)
        {
            return;
        }

        SynchronizationContext? syncContext = _syncContext;
        if (syncContext is not null)
        {
            syncContext.Post(_ => Application.ExitThread(), null);
        }

        _stopped.Wait(TimeSpan.FromSeconds(2));
        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
    }

    public bool TryPost(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        SynchronizationContext? syncContext = _syncContext;
        if (syncContext is null)
        {
            return false;
        }

        syncContext.Post(static state => ((Action)state!).Invoke(), action);
        return true;
    }

    private void ThreadMain()
    {
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        _syncContext = SynchronizationContext.Current;

        try
        {
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyPressed += OnKeyPressed;
            _keyboardHook.ToggleRequested += OnToggleRequested;
            _keyboardHook.Start();
        }
        catch (Exception ex)
        {
            _startupException = ex;
            _started.Set();
            _stopped.Set();
            return;
        }

        _started.Set();

        try
        {
            Application.Run();
        }
        finally
        {
            if (_keyboardHook is not null)
            {
                _keyboardHook.KeyPressed -= OnKeyPressed;
                _keyboardHook.ToggleRequested -= OnToggleRequested;
                _keyboardHook.Stop();
                _keyboardHook.Dispose();
                _keyboardHook = null;
            }

            _syncContext = null;
            _stopped.Set();
        }
    }

    private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
    {
        KeyPressed?.Invoke(sender, e);
    }

    private void OnToggleRequested()
    {
        ToggleRequested?.Invoke();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(KeyboardHookHost));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _started.Dispose();
        _stopped.Dispose();
        _disposed = true;
    }
}
