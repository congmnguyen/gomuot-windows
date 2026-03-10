using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using DrawingColor = System.Drawing.Color;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingLinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using WpfImageSource = System.Windows.Media.ImageSource;
using WpfBitmapSizeOptions = System.Windows.Media.Imaging.BitmapSizeOptions;

namespace GoMuot.Core;

/// <summary>
/// Generates runtime icons for the tray and WPF windows.
/// </summary>
public static class IconHelper
{
    private static readonly DrawingColor TrayEnabledBackground = DrawingColor.FromArgb(193, 95, 60);
    private static readonly DrawingColor TrayEnabledText = DrawingColor.FromArgb(244, 243, 238);
    private static readonly DrawingColor AccentTop = DrawingColor.FromArgb(240, 160, 80);
    private static readonly DrawingColor AccentBottom = DrawingColor.FromArgb(216, 119, 6);
    private static readonly DrawingColor DisabledGray = DrawingColor.FromArgb(209, 213, 219);
    private static readonly DrawingColor DisabledText = DrawingColor.FromArgb(75, 85, 99);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Creates a compact tray icon for the enabled/disabled states.
    /// </summary>
    public static Icon CreateTrayIcon(string text, bool isEnabled)
    {
        const int size = 16;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(DrawingColor.Transparent);

            var rect = new Rectangle(0, 0, size - 1, size - 1);
            using var path = RoundedRect(rect, 3);

            using var backgroundBrush = new SolidBrush(isEnabled ? TrayEnabledBackground : DisabledGray);
            g.FillPath(backgroundBrush, path);

            using var font = new Font("Segoe UI", 9, DrawingFontStyle.Bold);
            using var textBrush = new SolidBrush(isEnabled ? TrayEnabledText : DisabledText);
            var textSize = g.MeasureString(text, font);
            g.DrawString(text, font, textBrush, (size - textSize.Width) / 2, (size - textSize.Height) / 2);
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var tempIcon = Icon.FromHandle(hIcon);
            return (Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// Creates a WPF window icon for title bars and taskbar previews.
    /// </summary>
    public static WpfImageSource CreateWindowIcon(int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(DrawingColor.Transparent);

            var rect = new Rectangle(0, 0, size - 1, size - 1);
            using var path = RoundedRect(rect, Math.Max(4, size / 5));
            using var brush = new DrawingLinearGradientBrush(rect, AccentTop, AccentBottom, LinearGradientMode.Vertical);
            g.FillPath(brush, path);

            float fontSize = size * 0.52f;
            using var font = new Font("Segoe UI", fontSize, DrawingFontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(DrawingColor.White);
            var textSize = g.MeasureString("G", font);
            g.DrawString("G", font, textBrush, (size - textSize.Width) / 2, (size - textSize.Height) / 2);
        }

        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                WpfBitmapSizeOptions.FromEmptyOptions());

            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
