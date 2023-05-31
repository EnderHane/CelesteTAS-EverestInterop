using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CelesteStudio.RichText;

/// <summary>
/// Style of chars
/// </summary>
/// <remarks>This is base class for all text and design renderers</remarks>
public abstract class Style : IDisposable {
    /// <summary>
    /// Constructor
    /// </summary>
    public Style() {
        IsExportable = true;
    }

    /// <summary>
    /// This style is exported to outer formats (HTML for example)
    /// </summary>
    public virtual bool IsExportable { get; set; }

    public virtual void Dispose() {
        ;
    }

    /// <summary>
    /// Occurs when user click on StyleVisualMarker joined to this style 
    /// </summary>
    public event EventHandler<VisualMarkerEventArgs> VisualMarkerClick;

    /// <summary>
    /// Renders given range of text
    /// </summary>
    /// <param name="gr">Graphics object</param>
    /// <param name="position">Position of the range in absolute control coordinates</param>
    /// <param name="range">Rendering range of text</param>
    public abstract void Draw(Graphics gr, Point position, Range range);

    /// <summary>
    /// Occurs when user click on StyleVisualMarker joined to this style 
    /// </summary>
    public virtual void OnVisualMarkerClick(RichText tb, VisualMarkerEventArgs args) {
        VisualMarkerClick?.Invoke(tb, args);
    }

    /// <summary>
    /// Shows VisualMarker
    /// Call this method in Draw method, when you need to show VisualMarker for your style
    /// </summary>
    protected virtual void AddVisualMarker(RichText tb, StyleVisualMarker marker) {
        tb.AddVisualMarker(marker);
    }

    public static Size GetSizeOfRange(Range range) {
        return new((range.End.Char - range.Start.Char) * range.Tb.CharWidth, range.Tb.CharHeight);
    }

    public static GraphicsPath GetRoundedRectangle(Rectangle rect, int d) {
        GraphicsPath gp = new();

        gp.AddArc(rect.X, rect.Y, d, d, 180, 90);
        gp.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
        gp.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
        gp.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
        gp.AddLine(rect.X, rect.Y + rect.Height - d, rect.X, rect.Y + d / 2);

        return gp;
    }

    /// <summary>
    /// Returns CSS for export to HTML
    /// </summary>
    /// <returns></returns>
    public virtual string GetCSS() {
        return "";
    }
}

/// <summary>
/// Style for chars rendering
/// This renderer can draws chars, with defined fore and back colors
/// </summary>
public class TextStyle : Style {
    //public readonly Font Font;
    public StringFormat StringFormat;

    public TextStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle) {
        ForeBrush = foreBrush;
        BackgroundBrush = backgroundBrush;
        FontStyle = fontStyle;
        StringFormat = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
    }

    public Brush ForeBrush { get; set; }
    public Brush BackgroundBrush { get; set; }

    public FontStyle FontStyle { get; set; }

    public override void Draw(Graphics gr, Point position, Range range) {
        //draw background
        if (BackgroundBrush != null) {
            gr.FillRectangle(BackgroundBrush, position.X, position.Y, (range.End.Char - range.Start.Char) * range.Tb.CharWidth,
                range.Tb.CharHeight);
        }

        //draw chars
        Font f = new(range.Tb.Font, FontStyle);
        //Font fHalfSize = new Font(range.tb.Font.FontFamily, f.SizeInPoints/2, FontStyle);
        Line line = range.Tb[range.Start.Line];
        float dx = range.Tb.CharWidth;
        float y = position.Y + range.Tb.LineInterval / 2;
        float x = position.X - range.Tb.CharWidth / 3;

        ForeBrush ??= new SolidBrush(range.Tb.ForeColor);

        //IME mode
        if (range.Tb.ImeAllowed) {
            for (int i = range.Start.Char; i < range.End.Char; i++) {
                SizeF size = RichText.GetCharSize(f, line[i].Char_);

                GraphicsState gs = gr.Save();
                float k = size.Width > range.Tb.CharWidth + 1 ? range.Tb.CharWidth / size.Width : 1;
                gr.TranslateTransform(x, y + (1 - k) * range.Tb.CharHeight / 2);
                gr.ScaleTransform(k, (float) Math.Sqrt(k));
                gr.DrawString(line[i].Char_.ToString(), f, ForeBrush, 0, 0, StringFormat);
                gr.Restore(gs);
                /*
                if(size.Width>range.tb.CharWidth*1.5f)
                    gr.DrawString(line[i].c.ToString(), fHalfSize, foreBrush, x, y+range.tb.CharHeight/4, stringFormat);
                else
                    gr.DrawString(line[i].c.ToString(), f, foreBrush, x, y, stringFormat);
                 * */
                x += dx;
            }
        } else
        //classic mode 
        {
            for (int i = range.Start.Char; i < range.End.Char; i++) {
                //draw char
                gr.DrawString(line[i].Char_.ToString(), f, ForeBrush, x, y, StringFormat);
                x += dx;
            }
        }

        //
        f.Dispose();
    }

    public override void Dispose() {
        base.Dispose();

        ForeBrush?.Dispose();

        BackgroundBrush?.Dispose();
    }

    public override string GetCSS() {
        string result = "";

        if (BackgroundBrush is SolidBrush) {
            string s = ExportToHTML.GetColorAsString((BackgroundBrush as SolidBrush).Color);
            if (s != "") {
                result += "background-color:" + s + ";";
            }
        }

        if (ForeBrush is SolidBrush) {
            string s = ExportToHTML.GetColorAsString((ForeBrush as SolidBrush).Color);
            if (s != "") {
                result += "color:" + s + ";";
            }
        }

        if ((FontStyle & FontStyle.Bold) != 0) {
            result += "font-weight:bold;";
        }

        if ((FontStyle & FontStyle.Italic) != 0) {
            result += "font-style:oblique;";
        }

        if ((FontStyle & FontStyle.Strikeout) != 0) {
            result += "text-decoration:line-through;";
        }

        if ((FontStyle & FontStyle.Underline) != 0) {
            result += "text-decoration:underline;";
        }

        return result;
    }
}

/// <summary>
/// Renderer for folded block
/// </summary>
public class FoldedBlockStyle : TextStyle {
    public FoldedBlockStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle) :
        base(foreBrush, backgroundBrush, fontStyle) { }

    public override void Draw(Graphics gr, Point position, Range range) {
        if (range.End.Char > range.Start.Char) {
            base.Draw(gr, position, range);

            int firstNonSpaceSymbolX = position.X;

            //find first non space symbol
            for (int i = range.Start.Char; i < range.End.Char; i++) {
                if (range.Tb[range.Start.Line][i].Char_ != ' ') {
                    break;
                } else {
                    firstNonSpaceSymbolX += range.Tb.CharWidth;
                }
            }

            //create marker
            range.Tb.AddVisualMarker(new FoldedAreaMarker(range.Start.Line,
                new Rectangle(firstNonSpaceSymbolX, position.Y,
                    position.X + (range.End.Char - range.Start.Char) * range.Tb.CharWidth - firstNonSpaceSymbolX, range.Tb.CharHeight)));
        } else {
            //draw '...'
            using (Font f = new(range.Tb.Font, FontStyle)) {
                gr.DrawString("...", f, ForeBrush, range.Tb.LeftIndent, position.Y - 2);
            }

            //create marker
            range.Tb.AddVisualMarker(new FoldedAreaMarker(range.Start.Line,
                new Rectangle(range.Tb.LeftIndent + 2, position.Y, 2 * range.Tb.CharHeight, range.Tb.CharHeight)));
        }
    }
}

/// <summary>
/// Renderer for selection area
/// </summary>
public class SelectionStyle : Style {
    public SelectionStyle(Brush backgroundBrush) {
        BackgroundBrush = backgroundBrush;
    }

    public Brush BackgroundBrush { get; set; }

    public override bool IsExportable {
        get => false;
        set { }
    }

    public override void Draw(Graphics gr, Point position, Range range) {
        //draw background
        if (BackgroundBrush != null) {
            Rectangle rect = new(position.X, position.Y, (range.End.Char - range.Start.Char) * range.Tb.CharWidth,
                range.Tb.CharHeight);
            if (rect.Width == 0) {
                return;
            }

            gr.FillRectangle(BackgroundBrush, rect);
        }
    }

    public override void Dispose() {
        base.Dispose();

        BackgroundBrush?.Dispose();
    }
}

/// <summary>
/// Marker style
/// Draws background color for text
/// </summary>
public class MarkerStyle : Style {
    public MarkerStyle(Brush backgroundBrush) {
        BackgroundBrush = backgroundBrush;
        IsExportable = true;
    }

    public Brush BackgroundBrush { get; set; }

    public override void Draw(Graphics gr, Point position, Range range) {
        //draw background
        if (BackgroundBrush != null) {
            Rectangle rect = new(position.X, position.Y, (range.End.Char - range.Start.Char) * range.Tb.CharWidth,
                range.Tb.CharHeight);
            if (rect.Width == 0) {
                return;
            }

            //var path = GetRoundedRectangle(rect, 5);
            //gr.FillPath(BackgroundBrush, path);
            gr.FillRectangle(BackgroundBrush, rect);
        }
    }

    public override void Dispose() {
        base.Dispose();

        BackgroundBrush?.Dispose();
    }

    public override string GetCSS() {
        string result = "";

        if (BackgroundBrush is SolidBrush) {
            string s = ExportToHTML.GetColorAsString((BackgroundBrush as SolidBrush).Color);
            if (s != "") {
                result += "background-color:" + s + ";";
            }
        }

        return result;
    }
}

/// <summary>
/// Draws small rectangle for popup menu
/// </summary>
public class ShortcutStyle : Style {
    public Pen BorderPen;

    public ShortcutStyle(Pen borderPen) {
        BorderPen = borderPen;
    }

    public override void Draw(Graphics gr, Point position, Range range) {
        //get last char coordinates
        Point p = range.Tb.PlaceToPoint(range.End);
        //draw small square under char
        Rectangle rect = new(p.X - 5, p.Y + range.Tb.CharHeight - 2, 4, 3);
        gr.FillPath(Brushes.White, GetRoundedRectangle(rect, 1));
        gr.DrawPath(BorderPen, GetRoundedRectangle(rect, 1));
        //add visual marker for handle mouse events
        AddVisualMarker(range.Tb,
            new StyleVisualMarker(new Rectangle(p.X - range.Tb.CharWidth, p.Y, range.Tb.CharWidth, range.Tb.CharHeight), this));
    }

    public override void Dispose() {
        base.Dispose();

        BorderPen?.Dispose();
    }
}

/// <summary>
/// This style draws a wavy line below a given text range.
/// </summary>
/// <remarks>Thanks for Yallie</remarks>
public class WavyLineStyle : Style {
    public WavyLineStyle(int alpha, Color color) {
        Pen = new Pen(Color.FromArgb(alpha, color));
    }

    private Pen Pen { get; set; }

    public override void Draw(Graphics gr, Point pos, Range range) {
        Size size = GetSizeOfRange(range);
        Point start = new(pos.X, pos.Y + size.Height - 1);
        Point end = new(pos.X + size.Width, pos.Y + size.Height - 1);
        DrawWavyLine(gr, start, end);
    }

    private void DrawWavyLine(Graphics graphics, Point start, Point end) {
        if (end.X - start.X < 2) {
            graphics.DrawLine(Pen, start, end);
            return;
        }

        int offset = -1;
        List<Point> points = new();

        for (int i = start.X; i <= end.X; i += 2) {
            points.Add(new Point(i, start.Y + offset));
            offset = -offset;
        }

        graphics.DrawLines(Pen, points.ToArray());
    }

    public override void Dispose() {
        base.Dispose();
        Pen?.Dispose();
    }
}