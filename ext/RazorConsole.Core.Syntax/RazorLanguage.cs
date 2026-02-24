// Copyright (c) RazorConsole. All rights reserved.

namespace RazorConsole.Core.Rendering.Syntax;

using ColorCode;
using ColorCode.Common;

public class RazorLanguage : ILanguage
{
    public bool HasAlias(string lang) => lang == "razor";

    public string Id => "razor";
    public string Name => "Razor";
    public string CssClassName => "razor";
    public string FirstLinePattern => string.Empty;

    public IList<LanguageRule> Rules => new List<LanguageRule>
    {
        // 1. Razor Comments: @* ... *@
        new LanguageRule(@"@\*[\s\S]*?\*@", new Dictionary<int, string> { { 0, ScopeName.Comment } }),

        // 2. Razor Directives & Control Structures
        new LanguageRule(@"(?i)@(code|functions|using|inject|model|page|inherits|section|addTagHelper|layout|attribute|if|else|foreach|for|switch|while|do|lock|try|catch|finally)",
            new Dictionary<int, string> { { 0, "RazorControlKeyword" } }),

        // 3. Razor Transitions
        new LanguageRule(@"(@\{|@\(|@:|@|\{|\}|(?<=@\(.*)\))",
            new Dictionary<int, string> { { 0, "RazorTransition" } }),

        // 4. HTML Elements
        new LanguageRule(@"<[/?]?(?i:([a-z][a-z0-9\-_]*))", new Dictionary<int, string> { { 1, ScopeName.HtmlElementName } }),
        new LanguageRule(@"(?i:([a-z][a-z0-9\-_]*))=", new Dictionary<int, string> { { 1, ScopeName.HtmlAttributeName } }),

        // 5. C# Delimiters
        new LanguageRule(@"[\(\)\[\]]", new Dictionary<int, string> { { 0, ScopeName.Delimiter } }),

        // 6. C# Keywords
        new LanguageRule(@"(?i)\b(var|string|int|bool|decimal|float|double|object|dynamic|Task|void|new|public|private|protected|internal|return|await|async)\b",
            new Dictionary<int, string> { { 0, ScopeName.Keyword } }),

        // 7. Strings, Numbers & Comments
        new LanguageRule(@"""[^""]*""", new Dictionary<int, string> { { 0, ScopeName.String } }),
        new LanguageRule(@"\b\d+\b", new Dictionary<int, string> { { 0, ScopeName.Number } }),
        new LanguageRule(@"//.*", new Dictionary<int, string> { { 0, ScopeName.Comment } }),
    };
}
