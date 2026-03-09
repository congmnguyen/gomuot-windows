using System.Runtime.InteropServices;
using System.Text;

namespace GoMuot.Core;

/// <summary>
/// P/Invoke bridge to Rust core library (gomuot_core.dll)
/// FFI contract matches core/src/lib.rs exports
/// </summary>
public static class RustBridge
{
    private const string DllName = "gomuot_core.dll";

    #region Native Imports

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_init();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_clear();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_clear_all();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_free(IntPtr result);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_method(byte method);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_enabled([MarshalAs(UnmanagedType.U1)] bool enabled);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_modern([MarshalAs(UnmanagedType.U1)] bool modern);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void ime_skip_w_shortcut([MarshalAs(UnmanagedType.U1)] bool skip);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ime_key_ext(
        ushort keycode,
        [MarshalAs(UnmanagedType.U1)] bool capslock,
        [MarshalAs(UnmanagedType.U1)] bool ctrl,
        [MarshalAs(UnmanagedType.U1)] bool shift);

    #endregion

    #region Public API

    /// <summary>
    /// Initialize the IME engine. Call once at startup.
    /// </summary>
    public static void Initialize() => ime_init();

    /// <summary>
    /// Clear the typing buffer.
    /// </summary>
    public static void Clear()
    {
        ime_clear();
    }

    /// <summary>
    /// Clear the typing buffer and word history.
    /// </summary>
    public static void ClearAll()
    {
        ime_clear_all();
    }

    /// <summary>
    /// Set input method (Telex=0, VNI=1)
    /// </summary>
    public static void SetMethod(InputMethod method) => ime_method((byte)method);

    /// <summary>
    /// Enable or disable IME processing
    /// </summary>
    public static void SetEnabled(bool enabled) => ime_enabled(enabled);

    /// <summary>
    /// Set tone style (modern=true: hòa, old=false: hoà)
    /// </summary>
    public static void SetModernTone(bool modern) => ime_modern(modern);

    /// <summary>
    /// Use Simple Telex behavior: keep standalone "w" as-is while preserving ow/uw.
    /// </summary>
    public static void SetSkipWShortcut(bool skip) => ime_skip_w_shortcut(skip);

    /// <summary>
    /// Process a keystroke and get the result
    /// </summary>
    public static ImeResult ProcessKey(ushort keycode, bool capslock, bool ctrl, bool shift)
    {
        IntPtr ptr = ime_key_ext(keycode, capslock, ctrl, shift);
        if (ptr == IntPtr.Zero)
        {
            return ImeResult.Empty;
        }

        try
        {
            var native = Marshal.PtrToStructure<NativeResult>(ptr);
            return ImeResult.FromNative(native);
        }
        finally
        {
            ime_free(ptr);
        }
    }

    #endregion
}

/// <summary>
/// Input method type
/// </summary>
public enum InputMethod : byte
{
    Telex = 0,
    VNI = 1
}

/// <summary>
/// IME action type
/// </summary>
public enum ImeAction : byte
{
    None = 0,    // No action needed
    Send = 1,    // Send text replacement
    Restore = 2  // Restore original text
}

/// <summary>
/// Native result structure from Rust (must match core/src/lib.rs)
/// Size: 256 UInt32 chars (1024 bytes) + 4 bytes = 1028 bytes
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeResult
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public uint[] chars;
    public byte action;
    public byte backspace;
    public byte count;
    public byte flags;
}

/// <summary>
/// Managed IME result
/// </summary>
public readonly struct ImeResult
{
    private const byte KeyConsumedFlag = 0x01;

    public readonly ImeAction Action;
    public readonly byte Backspace;
    public readonly byte Count;
    public readonly bool KeyConsumed;
    private readonly uint[] _chars;

    public static readonly ImeResult Empty = new(ImeAction.None, 0, 0, false, Array.Empty<uint>());

    private ImeResult(ImeAction action, byte backspace, byte count, bool keyConsumed, uint[] chars)
    {
        Action = action;
        Backspace = backspace;
        Count = count;
        KeyConsumed = keyConsumed;
        _chars = chars;
    }

    internal static ImeResult FromNative(NativeResult native)
    {
        return new ImeResult(
            (ImeAction)native.action,
            native.backspace,
            native.count,
            (native.flags & KeyConsumedFlag) != 0,
            native.chars ?? Array.Empty<uint>()
        );
    }

    /// <summary>
    /// Get the result text as a string
    /// </summary>
    public string GetText()
    {
        if (Count == 0 || _chars == null)
            return string.Empty;

        var sb = new StringBuilder(Count);
        for (int i = 0; i < Count && i < _chars.Length; i++)
        {
            if (_chars[i] > 0)
            {
                sb.Append(char.ConvertFromUtf32((int)_chars[i]));
            }
        }
        return sb.ToString();
    }
}
