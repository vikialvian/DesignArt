namespace Corel;

public enum ShapeKind
{
    Rectangle,
    Ellipse,
    Line,
    Freehand,
    Polygon,
    Text
}

public sealed class VectorShape
{
    public ShapeKind Kind { get; init; }
    public RectangleF Bounds { get; init; }
    public List<PointF> Points { get; init; } = new();
    public Color? Fill { get; init; }
    public Color Stroke { get; init; } = Color.Black;
    public float StrokeWidth { get; init; } = 1.25F;
    public float TextScale { get; init; } = 1F;
    public string Text { get; init; } = string.Empty;
}
