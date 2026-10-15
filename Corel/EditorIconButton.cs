namespace Corel;

public enum EditorIconKind
{
    Select,
    Pan,
    Rectangle,
    Ellipse,
    Line,
    Freehand,
    Polygon,
    Text,
    NewDocument,
    Save,
    FitPage
}

public sealed class EditorIconButton : Button
{
    private readonly bool _compact;
    private readonly ToolTip _toolTip = new();

    public EditorIconButton(EditorIconKind icon, string caption, bool compact = false)
    {
        Icon = icon;
        Caption = caption;
        _compact = compact;
        Text = string.Empty;
        AccessibleName = caption;
        _toolTip.SetToolTip(this, caption);
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        BackColor = Color.FromArgb(59, 62, 67);
        ForeColor = Color.WhiteSmoke;
        Cursor = Cursors.Hand;
        Height = compact ? 38 : 30;
        Width = compact ? 38 : 34;
        Margin = compact ? new Padding(0, 0, 0, 5) : new Padding(2, 0, 4, 0);
        Padding = Padding.Empty;
        FlatAppearance.BorderColor = Color.FromArgb(72, 76, 81);
        FlatAppearance.BorderSize = 1;
    }

    public EditorIconKind Icon { get; }
    public string Caption { get; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        const int iconSize = 17;
        var iconX = (Width - iconSize) / 2;
        var iconY = (Height - iconSize) / 2;
        DrawIcon(graphics, new Rectangle(iconX, iconY, iconSize, iconSize));
    }

    private void DrawIcon(Graphics graphics, Rectangle bounds)
    {
        using var pen = new Pen(ForeColor, 1.7F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        var x = bounds.X;
        var y = bounds.Y;
        var w = bounds.Width;
        var h = bounds.Height;

        switch (Icon)
        {
            case EditorIconKind.Select:
                Point[] arrow = [new(x + 2, y + 1), new(x + 3, y + 15), new(x + 7, y + 11), new(x + 10, y + 16), new(x + 12, y + 15), new(x + 9, y + 10), new(x + 15, y + 9)];
                using (var fill = new SolidBrush(ForeColor))
                    graphics.FillPolygon(fill, arrow);
                break;
            case EditorIconKind.Pan:
                graphics.DrawLine(pen, x + 8, y + 2, x + 8, y + 15);
                graphics.DrawLine(pen, x + 2, y + 8, x + 15, y + 8);
                graphics.DrawLine(pen, x + 8, y + 2, x + 5, y + 5);
                graphics.DrawLine(pen, x + 8, y + 2, x + 11, y + 5);
                graphics.DrawLine(pen, x + 15, y + 8, x + 12, y + 5);
                graphics.DrawLine(pen, x + 15, y + 8, x + 12, y + 11);
                graphics.DrawLine(pen, x + 8, y + 15, x + 5, y + 12);
                graphics.DrawLine(pen, x + 8, y + 15, x + 11, y + 12);
                graphics.DrawLine(pen, x + 2, y + 8, x + 5, y + 5);
                graphics.DrawLine(pen, x + 2, y + 8, x + 5, y + 11);
                break;
            case EditorIconKind.Rectangle:
                graphics.DrawRectangle(pen, x + 2, y + 3, w - 4, h - 6);
                break;
            case EditorIconKind.Ellipse:
                graphics.DrawEllipse(pen, x + 2, y + 2, w - 4, h - 4);
                break;
            case EditorIconKind.Line:
                graphics.DrawLine(pen, x + 2, y + h - 3, x + w - 2, y + 3);
                break;
            case EditorIconKind.Freehand:
                Point[] stroke = [new(x + 1, y + 13), new(x + 5, y + 8), new(x + 8, y + 10), new(x + 13, y + 3), new(x + 16, y + 5)];
                graphics.DrawLines(pen, stroke);
                break;
            case EditorIconKind.Polygon:
                Point[] polygon = [new(x + 8, y + 1), new(x + 15, y + 5), new(x + 13, y + 13), new(x + 4, y + 15), new(x + 1, y + 7)];
                graphics.DrawPolygon(pen, polygon);
                break;
            case EditorIconKind.Text:
                using (var font = new Font("Segoe UI", 15F, FontStyle.Bold))
                    TextRenderer.DrawText(graphics, "T", font, bounds, ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                break;
            case EditorIconKind.NewDocument:
                graphics.DrawRectangle(pen, x + 3, y + 2, w - 6, h - 4);
                graphics.DrawLine(pen, x + w / 2, y + 5, x + w / 2, y + h - 5);
                graphics.DrawLine(pen, x + 5, y + h / 2, x + w - 5, y + h / 2);
                break;
            case EditorIconKind.Save:
                graphics.DrawRectangle(pen, x + 2, y + 2, w - 4, h - 4);
                graphics.DrawRectangle(pen, x + 5, y + 2, w - 10, 5);
                graphics.DrawRectangle(pen, x + 5, y + 9, w - 10, 5);
                break;
            case EditorIconKind.FitPage:
                graphics.DrawLine(pen, x + 2, y + 6, x + 2, y + 2);
                graphics.DrawLine(pen, x + 2, y + 2, x + 6, y + 2);
                graphics.DrawLine(pen, x + w - 6, y + 2, x + w - 2, y + 2);
                graphics.DrawLine(pen, x + w - 2, y + 2, x + w - 2, y + 6);
                graphics.DrawLine(pen, x + 2, y + h - 6, x + 2, y + h - 2);
                graphics.DrawLine(pen, x + 2, y + h - 2, x + 6, y + h - 2);
                graphics.DrawLine(pen, x + w - 6, y + h - 2, x + w - 2, y + h - 2);
                graphics.DrawLine(pen, x + w - 2, y + h - 2, x + w - 2, y + h - 6);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
