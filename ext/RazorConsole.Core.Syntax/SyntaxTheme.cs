// Copyright (c) RazorConsole. All rights reserved.

using System.Collections.ObjectModel;
using ColorCode.Common;
using Spectre.Console;

namespace RazorConsole.Core.Rendering.Syntax;

/// <summary>
/// Represents a mapping between ColorCode scopes and Spectre.Console styles.
/// </summary>
public class SyntaxTheme
{
    private readonly IReadOnlyDictionary<string, Style> _scopedStyles;

    protected SyntaxTheme(string name, Style defaultStyle, IReadOnlyDictionary<string, Style>? scopedStyles)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DefaultStyle = defaultStyle;
        _scopedStyles = scopedStyles ?? new ReadOnlyDictionary<string, Style>(new Dictionary<string, Style>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the name of the theme.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the default style when a scope is not matched.
    /// </summary>
    public Style DefaultStyle { get; }

    /// <summary>
    /// Gets the style for the specified ColorCode scope name.
    /// </summary>
    public virtual Style GetStyle(string? scopeName)
    {
        if (string.IsNullOrEmpty(scopeName))
        {
            return DefaultStyle;
        }

        if (_scopedStyles.TryGetValue(scopeName, out var style))
        {
            return style;
        }

        // Allow lookups using canonical scope names even if the stored keys differ in casing.
        if (_scopedStyles.TryGetValue(scopeName.Trim(), out style))
        {
            return style;
        }

        return DefaultStyle;
    }

    /// <summary>
    /// Creates the default syntax theme.
    /// </summary>
    public static SyntaxTheme CreateDefault()
    {
        var defaultStyle = Style.Plain;
        var styles = new Dictionary<string, Style>(StringComparer.OrdinalIgnoreCase)
        {
            // --- COMMON & C# ---
            [ScopeName.Comment] = new Style(Color.FromHex("#6A9955"), decoration: Decoration.Italic),
            [ScopeName.Keyword] = new Style(Color.FromHex("#569CD6")),
            [ScopeName.ControlKeyword] = new Style(Color.FromHex("#C586C0")),
            [ScopeName.PreprocessorKeyword] = new Style(Color.FromHex("#C586C0")),
            [ScopeName.String] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.StringCSharpVerbatim] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.Number] = new Style(Color.FromHex("#B5CEA8")),
            [ScopeName.Operator] = new Style(Color.FromHex("#D4D4D4")),
            [ScopeName.Delimiter] = new Style(Color.FromHex("#D4D4D4")),

            // --- TYPES & MEMBERS ---
            [ScopeName.ClassName] = new Style(Color.FromHex("#4EC9B0")),
            [ScopeName.Type] = new Style(Color.FromHex("#4EC9B0")),
            [ScopeName.TypeVariable] = new Style(Color.FromHex("#4EC9B0")),
            [ScopeName.NameSpace] = new Style(Color.FromHex("#D4D4D4")),
            [ScopeName.Constructor] = new Style(Color.FromHex("#4EC9B0")),
            [ScopeName.Attribute] = new Style(Color.FromHex("#D7BA7D")),
            [ScopeName.BuiltinFunction] = new Style(Color.FromHex("#DCDCAA")),
            [ScopeName.BuiltinValue] = new Style(Color.FromHex("#569CD6")),
            [ScopeName.SpecialCharacter] = new Style(Color.FromHex("#D7BA7D")),

            // --- RAZOR (Custom Scopes) ---
            ["RazorTransition"] = new Style(Color.FromHex("#C586C0")),
            ["RazorDirective"] = new Style(Color.FromHex("#C586C0")),
            ["RazorControlKeyword"] = new Style(Color.FromHex("#C586C0")),
            ["RazorCodeBlock"] = new Style(background: Color.FromHex("#1E1E1E")),

            // --- HTML & XML ---
            [ScopeName.HtmlElementName] = new Style(Color.FromHex("#569CD6")),
            [ScopeName.HtmlAttributeName] = new Style(Color.FromHex("#9CDCFE")),
            [ScopeName.HtmlAttributeValue] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.HtmlTagDelimiter] = new Style(Color.FromHex("#808080")),
            [ScopeName.HtmlComment] = new Style(Color.FromHex("#6A9955"), decoration: Decoration.Italic),
            [ScopeName.HtmlOperator] = new Style(Color.FromHex("#D4D4D4")),
            [ScopeName.XmlAttribute] = new Style(Color.FromHex("#9CDCFE")),
            [ScopeName.XmlAttributeValue] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.XmlDelimiter] = new Style(Color.FromHex("#808080")),
            [ScopeName.XmlDocComment] = new Style(Color.FromHex("#6A9955")),
            [ScopeName.XmlDocTag] = new Style(Color.FromHex("#569CD6")),

            // --- JSON ---
            [ScopeName.JsonKey] = new Style(Color.FromHex("#9CDCFE")),
            [ScopeName.JsonString] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.JsonNumber] = new Style(Color.FromHex("#B5CEA8")),
            [ScopeName.JsonConst] = new Style(Color.FromHex("#569CD6")),

            // --- JS & TS (Uses common scopes + specific) ---
            [ScopeName.PseudoKeyword] = new Style(Color.FromHex("#569CD6")),

            // --- CSS ---
            [ScopeName.CssPropertyName] = new Style(Color.FromHex("#9CDCFE")),
            [ScopeName.CssPropertyValue] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.CssSelector] = new Style(Color.FromHex("#D7BA7D")),

            // --- SQL ---
            [ScopeName.SqlSystemFunction] = new Style(Color.FromHex("#C586C0"), decoration: Decoration.Bold),

            // --- POWERSHELL ---
            [ScopeName.PowerShellCommand] = new Style(Color.FromHex("#DCDCAA")),
            [ScopeName.PowerShellParameter] = new Style(Color.FromHex("#9CDCFE")),
            [ScopeName.PowerShellVariable] = new Style(Color.FromHex("#9CDCFE")),

            // --- MARKDOWN ---
            [ScopeName.MarkdownHeader] = new Style(Color.FromHex("#569CD6"), decoration: Decoration.Bold),
            [ScopeName.MarkdownCode] = new Style(Color.FromHex("#CE9178")),
            [ScopeName.MarkdownListItem] = new Style(Color.FromHex("#6796E6")),
            [ScopeName.MarkdownEmph] = new Style(decoration: Decoration.Italic),
            [ScopeName.MarkdownBold] = new Style(decoration: Decoration.Bold),
        };

        return new SyntaxTheme("default", defaultStyle, new ReadOnlyDictionary<string, Style>(styles));
    }
}
