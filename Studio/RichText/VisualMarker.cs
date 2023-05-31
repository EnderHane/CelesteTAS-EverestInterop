using System.Drawing;
using System.Windows.Forms;

namespace CelesteStudio.RichText;

public class VisualMarker {
    public readonly Rectangle Rectangle;

    public VisualMarker(Rectangle rectangle) {
        this.Rectangle = rectangle;
    }

    public virtual Cursor Cursor => Cursors.Hand;

    public virtual void Draw(Graphics gr, Pen pen) { }
}

class CollapseFoldingMarker : VisualMarker {
    public readonly int Line;

    public CollapseFoldingMarker(int line, Rectangle rectangle)
        : base(rectangle) {
        Line = line;
    }

    public override void Draw(Graphics gr, Pen pen) {
        //draw minus
        gr.FillRectangle(Brushes.White, Rectangle);
        gr.DrawRectangle(pen, Rectangle);
        gr.DrawLine(pen, Rectangle.Left + 2, Rectangle.Top + Rectangle.Height / 2, Rectangle.Right - 2, Rectangle.Top + Rectangle.Height / 2);
    }
}

class ExpandFoldingMarker : VisualMarker {
    public readonly int Line;

    public ExpandFoldingMarker(int line, Rectangle rectangle)
        : base(rectangle) {
        Line = line;
    }

    public override void Draw(Graphics gr, Pen pen) {
        //draw plus
        gr.FillRectangle(Brushes.White, Rectangle);
        gr.DrawRectangle(pen, Rectangle);
        gr.DrawLine(Pens.Red, Rectangle.Left + 2, Rectangle.Top + Rectangle.Height / 2, Rectangle.Right - 2,
            Rectangle.Top + Rectangle.Height / 2);
        gr.DrawLine(Pens.Red, Rectangle.Left + Rectangle.Width / 2, Rectangle.Top + 2, Rectangle.Left + Rectangle.Width / 2,
            Rectangle.Bottom - 2);
    }
}

public class FoldedAreaMarker : VisualMarker {
    public readonly int Line;

    public FoldedAreaMarker(int line, Rectangle rectangle)
        : base(rectangle) {
        Line = line;
    }

    public override void Draw(Graphics gr, Pen pen) {
        gr.DrawRectangle(pen, Rectangle);
    }
}

public class StyleVisualMarker : VisualMarker {
    public StyleVisualMarker(Rectangle rectangle, Style style)
        : base(rectangle) {
        Style = style;
    }

    public Style Style { get; private set; }
}

public class VisualMarkerEventArgs : MouseEventArgs {
    public VisualMarkerEventArgs(Style style, StyleVisualMarker marker, MouseEventArgs args)
        : base(args.Button, args.Clicks, args.X, args.Y, args.Delta) {
        Style = style;
        Marker = marker;
    }

    public Style Style { get; private set; }
    public StyleVisualMarker Marker { get; private set; }
}