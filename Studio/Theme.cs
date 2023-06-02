using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CelesteStudio.RichText;

namespace CelesteStudio;

public enum ThemesType {
    Light,
    Dark,
    Custom
}

public static class Themes {
    public static Theme Light = new LightTheme();
    public static Theme Dark = new DarkTheme();
    public static Theme Custom = new CustomTheme();

    public static void Load(Theme light, Theme dark, Theme custom) {
        Light = light;
        Dark = dark;
        Custom = custom;

        ResetThemes();
    }

    public static void ResetThemes() {
        Theme theme = Settings.Instance.ThemesType switch {
            ThemesType.Dark => Dark,
            ThemesType.Custom => Custom,
            _ => Light
        };

        RichText.StudioTextEdit richText = Studio.Instance.richText;

        SyntaxHighlighter.ActionStyle = ColorUtil.CreateTextStyle(theme.Action);
        SyntaxHighlighter.AngleStyle = ColorUtil.CreateTextStyle(theme.Angle);
        SyntaxHighlighter.BreakpointAsteriskStyle = ColorUtil.CreateTextStyle(theme.Breakpoint);
        SyntaxHighlighter.CommaStyle = ColorUtil.CreateTextStyle(theme.Comma);
        SyntaxHighlighter.CommandStyle = ColorUtil.CreateTextStyle(theme.Command);
        SyntaxHighlighter.CommentStyle = ColorUtil.CreateTextStyle(theme.Comment);
        SyntaxHighlighter.FrameStyle = ColorUtil.CreateTextStyle(theme.Frame);
        SyntaxHighlighter.SaveStateStyle = ColorUtil.CreateTextStyle(theme.SaveState);

        richText.SaveStateTextColor = ColorUtil.HexToColor(theme.SaveState);
        richText.SaveStateBgColor = ColorUtil.HexToColor(theme.SaveState, 1);
        richText.PlayingLineTextColor = ColorUtil.HexToColor(theme.PlayingLine);
        richText.PlayingLineBgColor = ColorUtil.HexToColor(theme.PlayingLine, 1);
        richText.BackColor = ColorUtil.HexToColor(theme.Background);
        richText.PaddingBackColor = ColorUtil.HexToColor(theme.Background);
        richText.IndentBackColor = ColorUtil.HexToColor(theme.Background);
        richText.CaretColor = ColorUtil.HexToColor(theme.Caret);
        richText.CurrentTextColor = ColorUtil.HexToColor(theme.PlayingFrame);
        richText.LineNumberColor = ColorUtil.HexToColor(theme.LineNumber);
        richText.SelectionColor = ColorUtil.HexToColor(theme.Selection);
        richText.CurrentLineColor = ColorUtil.HexToColor(theme.CurrentLine);
        richText.ChangedLineTextColor = ColorUtil.HexToColor(theme.ChangedLine);
        richText.ChangedLineBgColor = ColorUtil.HexToColor(theme.ChangedLine, 1);
        richText.ServiceLinesColor = ColorUtil.HexToColor(theme.ServiceLine);

        Studio.Instance.SetControlsColor(theme);

        richText.ClearStylesBuffer();
        richText.SyntaxHighlighter.TASSyntaxHighlight(richText.Range);
    }
}

public abstract class Theme {
    public abstract List<string> Action { get; set; }
    public abstract List<string> Angle { get; set; }
    public abstract List<string> Background { get; set; }
    public abstract List<string> Breakpoint { get; set; }
    public abstract List<string> Caret { get; set; }
    public abstract List<string> ChangedLine { get; set; }
    public abstract List<string> Comma { get; set; }
    public abstract List<string> Command { get; set; }
    public abstract List<string> Comment { get; set; }
    public abstract List<string> CurrentLine { get; set; }
    public abstract List<string> Frame { get; set; }
    public abstract List<string> LineNumber { get; set; }
    public abstract List<string> PlayingFrame { get; set; }
    public abstract List<string> PlayingLine { get; set; }
    public abstract List<string> SaveState { get; set; }
    public abstract List<string> Selection { get; set; }
    public abstract List<string> ServiceLine { get; set; }
    public abstract List<string> Status { get; set; }
}

public class LightTheme : Theme {
    public override List<string> Action { get; set; } = new() { "2222FF" };
    public override List<string> Angle { get; set; } = new() { "EE22EE" };
    public override List<string> Background { get; set; } = new() { "FFFFFF" };
    public override List<string> Breakpoint { get; set; } = new() { "FFFFFF", "FF5555" };
    public override List<string> Caret { get; set; } = new() { "000000" };
    public override List<string> ChangedLine { get; set; } = new() { "000000", "FF8C00" };
    public override List<string> Comma { get; set; } = new() { "808080" };
    public override List<string> Command { get; set; } = new() { "D2691E" };
    public override List<string> Comment { get; set; } = new() { "00A000" };
    public override List<string> CurrentLine { get; set; } = new() { "20000000" };
    public override List<string> Frame { get; set; } = new() { "FF2222" };
    public override List<string> LineNumber { get; set; } = new() { "000000" };
    public override List<string> PlayingFrame { get; set; } = new() { "22A022" };
    public override List<string> PlayingLine { get; set; } = new() { "000000", "55FF55" };
    public override List<string> SaveState { get; set; } = new() { "FFFFFF", "4682B4" };
    public override List<string> Selection { get; set; } = new() { "20000000" };
    public override List<string> ServiceLine { get; set; } = new() { "C0C0C0" };
    public override List<string> Status { get; set; } = new() { "000000", "F2F2F2" };
}

public class DarkTheme : Theme {
    public override List<string> Action { get; set; } = new() { "8BE9FD" };
    public override List<string> Angle { get; set; } = new() { "FF79C6" };
    public override List<string> Background { get; set; } = new() { "282A36" };
    public override List<string> Breakpoint { get; set; } = new() { "F8F8F2", "FF5555" };
    public override List<string> Caret { get; set; } = new() { "AEAFAD" };
    public override List<string> ChangedLine { get; set; } = new() { "6272A4", "FFB86C" };
    public override List<string> Comma { get; set; } = new() { "6272A4" };
    public override List<string> Command { get; set; } = new() { "FFB86C" };
    public override List<string> Comment { get; set; } = new() { "95B272" };
    public override List<string> CurrentLine { get; set; } = new() { "20B4B6C7" };
    public override List<string> Frame { get; set; } = new() { "BD93F9" };
    public override List<string> LineNumber { get; set; } = new() { "6272A4" };
    public override List<string> PlayingFrame { get; set; } = new() { "F1FA8C" };
    public override List<string> PlayingLine { get; set; } = new() { "6272A4", "F1FA8C" };
    public override List<string> SaveState { get; set; } = new() { "F8F8F2", "4682B4" };
    public override List<string> Selection { get; set; } = new() { "20B4B6C7" };
    public override List<string> ServiceLine { get; set; } = new() { "44475A" };
    public override List<string> Status { get; set; } = new() { "F8F8F2", "383A46" };
}

public class CustomTheme : Theme {
    public override List<string> Action { get; set; } = new() { "268BD2" };
    public override List<string> Angle { get; set; } = new() { "D33682" };
    public override List<string> Background { get; set; } = new() { "FDF6E3" };
    public override List<string> Breakpoint { get; set; } = new() { "FDF6E3", "DC322F" };
    public override List<string> Caret { get; set; } = new() { "6B7A82" };
    public override List<string> ChangedLine { get; set; } = new() { "F8F8F2", "CB4B16" };
    public override List<string> Comma { get; set; } = new() { "808080" };
    public override List<string> Command { get; set; } = new() { "B58900" };
    public override List<string> Comment { get; set; } = new() { "859900" };
    public override List<string> CurrentLine { get; set; } = new() { "201A1300" };
    public override List<string> Frame { get; set; } = new() { "DC322F" };
    public override List<string> LineNumber { get; set; } = new() { "93A1A1" };
    public override List<string> PlayingFrame { get; set; } = new() { "6C71C4" };
    public override List<string> PlayingLine { get; set; } = new() { "FDF6E3", "6C71C4" };
    public override List<string> SaveState { get; set; } = new() { "FDF6E3", "268BD2" };
    public override List<string> Selection { get; set; } = new() { "201A1300" };
    public override List<string> ServiceLine { get; set; } = new() { "44475A" };
    public override List<string> Status { get; set; } = new() { "073642", "EEE8D5" };
}

public class ThemeColorTable : ProfessionalColorTable {
    private readonly Theme theme;

    public ThemeColorTable(Theme theme) {
        this.theme = theme;
    }

    public override Color ToolStripDropDownBackground => ColorUtil.HexToColor(theme.Status, 1);
    public override Color ImageMarginGradientBegin => ColorUtil.HexToColor(theme.Status, 1);
    public override Color ImageMarginGradientMiddle => ColorUtil.HexToColor(theme.Status, 1);
    public override Color ImageMarginGradientEnd => ColorUtil.HexToColor(theme.Status, 1);
    public override Color MenuBorder => ColorUtil.HexToColor(theme.CurrentLine);
    public override Color MenuItemBorder => ColorUtil.HexToColor(theme.CurrentLine);
    public override Color MenuItemSelected => ColorUtil.HexToColor(theme.Selection);
    public override Color MenuStripGradientBegin => ColorUtil.HexToColor(theme.Status, 1);
    public override Color MenuStripGradientEnd => ColorUtil.HexToColor(theme.Status, 1);
    public override Color MenuItemSelectedGradientBegin => ColorUtil.HexToColor(theme.Selection);
    public override Color MenuItemSelectedGradientEnd => ColorUtil.HexToColor(theme.Selection);
    public override Color MenuItemPressedGradientBegin => ColorUtil.HexToColor(theme.Status, 1);
    public override Color MenuItemPressedGradientEnd => ColorUtil.HexToColor(theme.Status, 1);
}

public class ThemeRenderer : ToolStripProfessionalRenderer {
    private readonly Theme themes;

    public ThemeRenderer(Theme themes) : base(new ThemeColorTable(themes)) {
        this.themes = themes;
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
        if (e.Item is ToolStripMenuItem) {
            e.ArrowColor = ColorUtil.HexToColor(themes.Status);
        }

        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
        e.TextColor = ColorUtil.HexToColor(themes.Status);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e) {
        e.Item.BackColor = ColorUtil.HexToColor(themes.Status, 1);
        base.OnRenderItemBackground(e);
    }
}

public static class ColorUtil {
    private static readonly Regex HexChar = new(@"^[0-9a-f]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Color ErrorColor = Color.FromArgb(128, 255, 0, 0);
    private static readonly TextStyle ErrorTextStyle = new(Brushes.White, new SolidBrush(ErrorColor), FontStyle.Regular);

    public static TextStyle CreateTextStyle(List<string> colors) {
        if (colors == null || colors.Count == 0) {
            return ErrorTextStyle;
        }

        if (TryHexToColor(colors[0], out Color color)) {
            if (colors.Count == 1) {
                return new TextStyle(new SolidBrush(color), null, FontStyle.Regular);
            } else if (TryHexToColor(colors[1], out Color backgroundColor)) {
                return new TextStyle(new SolidBrush(color), new SolidBrush(backgroundColor), FontStyle.Regular);
            } else {
                return ErrorTextStyle;
            }
        } else {
            return ErrorTextStyle;
        }
    }

    public static Color HexToColor(List<string> colors, int index = 0) {
        if (colors == null || colors.Count <= index) {
            return ErrorColor;
        }

        return TryHexToColor(colors[index], out Color color) ? color : ErrorColor;
    }

    public static bool TryHexToColor(string hex, out Color color) {
        color = ErrorColor;
        if (string.IsNullOrWhiteSpace(hex)) {
            return false;
        }

        hex = hex.Replace("#", "");
        if (!HexChar.IsMatch(hex)) {
            return false;
        }

        // 123456789 => 12345678
        if (hex.Length > 8) {
            hex = hex.Substring(0, 8);
        }

        // 123 => 112233
        // 1234 => 11223344
        if (hex.Length == 3 || hex.Length == 4) {
            hex = hex.ToCharArray().Select(c => $"{c}{c}").Aggregate((s, s1) => s + s1);
        }

        // 123456 => FF123456
        hex = hex.PadLeft(8, 'F');

        try {
            long number = Convert.ToInt64(hex, 16);
            byte a = (byte) (number >> 24);
            byte r = (byte) (number >> 16);
            byte g = (byte) (number >> 8);
            byte b = (byte) number;
            color = Color.FromArgb(a, r, g, b);
            return true;
        } catch (FormatException) {
            return false;
        }
    }
}