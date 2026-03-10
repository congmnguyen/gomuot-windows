using System.Reflection;

namespace GoMuot.Core;

/// <summary>
/// Centralized app metadata - matches macOS AppMetadata.swift
/// All project metadata in one place for consistency
/// </summary>
public static class AppMetadata
{
    // App Info
    public static readonly string Name = "GoMuot";
    public static readonly string DisplayName = "GoMuot - Gõ Mượt";
    public static readonly string Tagline = "Bộ gõ tiếng Việt gọn và mượt cho Windows";

    // Version
    public static string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
        }
    }

    // Author
    public static readonly string Author = "Cong Nguyen";
    public static readonly string AuthorEmail = "";
    public static readonly string AuthorLinkedin = "";

    // Links
    public static readonly string Website = "https://github.com/congmnguyen/gomuot-windows";
    public static readonly string Repository = "https://github.com/congmnguyen/gomuot-windows";
    public static readonly string IssuesUrl = "https://github.com/congmnguyen/gomuot-windows/issues";

    // Legal
    public static readonly string Copyright = $"Copyright (c) 2025 {Author}. All rights reserved.";
    public static readonly string License = "GPL-3.0-or-later";

    // Tech
    public static readonly string TechStack = "Rust + WinForms";
    public static readonly string ToggleHotkeyDisplay = "Ctrl+Space";
}

/// <summary>
/// Input method descriptions - matches macOS InputMode
/// </summary>
public static class InputMethodInfo
{
    public static string GetName(InputMethod method) => method switch
    {
        InputMethod.Telex => "Simple Telex",
        InputMethod.VNI => "VNI",
        _ => "Unknown"
    };

    public static string GetShortName(InputMethod method) => method switch
    {
        InputMethod.Telex => "T",
        InputMethod.VNI => "V",
        _ => "?"
    };

    public static string GetDescription(InputMethod method) => method switch
    {
        InputMethod.Telex => "dd, aw, aa, ow, uw, s, f, r, x, j",
        InputMethod.VNI => "a8, o9, 1-5",
        _ => ""
    };

    public static string GetFullDescription(InputMethod method) => method switch
    {
        InputMethod.Telex => $"Simple Telex ({GetDescription(method)})",
        InputMethod.VNI => $"VNI ({GetDescription(method)})",
        _ => ""
    };
}
