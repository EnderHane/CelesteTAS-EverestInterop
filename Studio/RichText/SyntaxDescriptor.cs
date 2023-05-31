using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CelesteStudio.RichText;

public class SyntaxDescriptor : IDisposable {
    public readonly List<FoldingDesc> Foldings = new();
    public readonly List<RuleDesc> Rules = new();
    public readonly List<Style> Styles = new();
    public char LeftBracket = '(';
    public char LeftBracket2 = '\x0';
    public char RightBracket = ')';
    public char RightBracket2 = '\x0';

    public void Dispose() {
        foreach (var style in Styles) {
            style.Dispose();
        }
    }
}

public class RuleDesc {
    public RegexOptions Options = RegexOptions.None;
    public string Pattern;
    Regex regex;
    public Style Style;

    public Regex Regex {
        get {
            if (regex == null) {
                regex = new Regex(Pattern, RegexOptions.Compiled | Options);
            }

            return regex;
        }
    }
}

public class FoldingDesc {
    public string FinishMarkerRegex;
    public RegexOptions Options = RegexOptions.None;
    public string StartMarkerRegex;
}