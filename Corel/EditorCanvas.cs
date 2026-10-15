using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace Corel;

public sealed class EditorCanvas : Control
{
    private readonly List<VectorShape> _shapes = new();
    private readonly List<PointF> _drawingPoints = new();
    private PointF _dragStart;
    private PointF _dragEnd;
    private Point _panStart;
    private PointF _originAtPan;
    private PointF? _pageOrigin;
    private float _zoom = 1F;
    private int _selectedShape = -1;
    private bool _isDrawing;
    private bool _isPanning;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EditorTool ActiveTool { get; set; } = EditorTool.Select;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? FillColor { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StrokeColor { get; set; } = Color.Black;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ZoomPercent => (int)Math.Round(_zoom * 100F);

    public event Action<string>? StatusChanged;
    public event Action<VectorShape?>? SelectionChanged;
    public event Action<int>? ZoomChanged;

    public EditorCanvas()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(57, 60, 65);
        TabStop = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    public void ClearDocument()
    {
        _shapes.Clear();
        _selectedShape = -1;
        SelectionChanged?.Invoke(null);
        StatusChanged?.Invoke("Dokumen baru");
        Invalidate();
    }

    public void DeleteSelection()
    {
        if (_selectedShape < 0 || _selectedShape >= _shapes.Count)
            return;

        _shapes.RemoveAt(_selectedShape);
        _selectedShape = -1;
        SelectionChanged?.Invoke(null);
        StatusChanged?.Invoke("Objek dihapus");
        Invalidate();
    }

    public void FitPage()
    {
        _zoom = 1F;
        _pageOrigin = null;
        ZoomChanged?.Invoke(ZoomPercent);
        StatusChanged?.Invoke("Halaman disesuaikan ke layar");
        Invalidate();
    }

    public void ZoomBy(float factor)
    {
        ZoomAt(factor, new Point(ClientSize.Width / 2, ClientSize.Height / 2));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ZoomAt(e.Delta > 0 ? 1.1F : 1F / 1.1F, e.Location);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
        Cursor = ActiveTool switch
        {
            EditorTool.Pan => Cursors.Hand,
            EditorTool.Select => Cursors.Default,
            _ => Cursors.Cross
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        var page = GetPageBounds();
        using (var shadow = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
            graphics.FillRectangle(shadow, page.X + 5, page.Y + 6, page.Width, page.Height);
        graphics.FillRectangle(Brushes.White, page);
        using (var border = new Pen(Color.FromArgb(205, 207, 210)))
            graphics.DrawRectangle(border, page.X, page.Y, page.Width, page.Height);

        for (var i = 0; i < _shapes.Count; i++)
            DrawShape(graphics, _shapes[i], page, i == _selectedShape);

        if (_isDrawing)
            DrawShape(graphics, CreateCurrentShape(), page, false);

        using var labelBrush = new SolidBrush(Color.FromArgb(190, 194, 199));
        graphics.DrawString("Halaman 1", Font, labelBrush, page.Left,
            Math.Min(ClientSize.Height - 20, page.Bottom + 8));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Middle)
        {
            var panPage = GetPageBounds();
            _pageOrigin = panPage.Location;
            _originAtPan = panPage.Location;
            _panStart = e.Location;
            _isPanning = true;
            Cursor = Cursors.SizeAll;
            Capture = true;
            return;
        }

        if (e.Button != MouseButtons.Left)
            return;

        var page = GetPageBounds();
        if (ActiveTool == EditorTool.Pan)
        {
            _pageOrigin = page.Location;
            _originAtPan = page.Location;
            _panStart = e.Location;
            _isPanning = true;
            Cursor = Cursors.SizeAll;
            Capture = true;
            return;
        }

        var point = ToNormalizedPoint(e.Location, page);
        if (ActiveTool == EditorTool.Select)
        {
            _selectedShape = FindShape(point);
            SelectionChanged?.Invoke(_selectedShape >= 0 ? _shapes[_selectedShape] : null);
            StatusChanged?.Invoke(_selectedShape >= 0 ? "Objek dipilih" : "Tidak ada objek dipilih");
            Invalidate();
            return;
        }

        if (ActiveTool == EditorTool.Text)
        {
            AddText(point);
            return;
        }

        _dragStart = point;
        _dragEnd = point;
        _drawingPoints.Clear();
        _drawingPoints.Add(point);
        _isDrawing = true;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isPanning)
        {
            _pageOrigin = new PointF(
                _originAtPan.X + e.X - _panStart.X,
                _originAtPan.Y + e.Y - _panStart.Y);
            Invalidate();
            return;
        }

        if (!_isDrawing)
            return;

        _dragEnd = ToNormalizedPoint(e.Location, GetPageBounds());
        if (ActiveTool == EditorTool.Freehand)
        {
            var last = _drawingPoints[^1];
            var dx = _dragEnd.X - last.X;
            var dy = _dragEnd.Y - last.Y;
            if (dx * dx + dy * dy > 0.00002F)
                _drawingPoints.Add(_dragEnd);
        }

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isPanning && (e.Button == MouseButtons.Middle ||
            (ActiveTool == EditorTool.Pan && e.Button == MouseButtons.Left)))
        {
            _isPanning = false;
            Capture = false;
            Cursor = ActiveTool switch
            {
                EditorTool.Pan => Cursors.Hand,
                EditorTool.Select => Cursors.Default,
                _ => Cursors.Cross
            };
            return;
        }

        if (!_isDrawing)
            return;

        _dragEnd = ToNormalizedPoint(e.Location, GetPageBounds());
        if (ActiveTool == EditorTool.Freehand)
            _drawingPoints.Add(_dragEnd);

        _isDrawing = false;
        Capture = false;
        var shape = CreateCurrentShape();
        if (shape.Bounds.Width > 0.004F || shape.Bounds.Height > 0.004F)
        {
            _shapes.Add(shape);
            _selectedShape = _shapes.Count - 1;
            SelectionChanged?.Invoke(shape);
            StatusChanged?.Invoke("Objek dibuat");
        }
        Invalidate();
    }

    private void ZoomAt(float factor, Point anchor)
    {
        var page = GetPageBounds();
        var anchorX = (anchor.X - page.X) / page.Width;
        var anchorY = (anchor.Y - page.Y) / page.Height;
        var newZoom = Math.Clamp(_zoom * factor, 0.25F, 8F);
        if (Math.Abs(newZoom - _zoom) < 0.0001F)
            return;

        _zoom = newZoom;
        var size = GetPageSize();
        _pageOrigin = new PointF(
            anchor.X - anchorX * size.Width,
            anchor.Y - anchorY * size.Height);

        ZoomChanged?.Invoke(ZoomPercent);
        StatusChanged?.Invoke($"Zoom {ZoomPercent}%");
        Invalidate();
    }

    private void AddText(PointF point)
    {
        using var dialog = new TextEntryDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.EnteredText))
            return;

        var shape = new VectorShape
        {
            Kind = ShapeKind.Text,
            Bounds = new RectangleF(point.X, point.Y, 0.35F, 0.08F),
            Stroke = StrokeColor,
            Text = dialog.EnteredText
        };
        _shapes.Add(shape);
        _selectedShape = _shapes.Count - 1;
        SelectionChanged?.Invoke(shape);
        StatusChanged?.Invoke("Teks ditambahkan");
        Invalidate();
    }

    private VectorShape CreateCurrentShape()
    {
        var bounds = new RectangleF(
            Math.Min(_dragStart.X, _dragEnd.X),
            Math.Min(_dragStart.Y, _dragEnd.Y),
            Math.Abs(_dragEnd.X - _dragStart.X),
            Math.Abs(_dragEnd.Y - _dragStart.Y));

        var kind = ActiveTool switch
        {
            EditorTool.Ellipse => ShapeKind.Ellipse,
            EditorTool.Line => ShapeKind.Line,
            EditorTool.Freehand => ShapeKind.Freehand,
            EditorTool.Polygon => ShapeKind.Polygon,
            _ => ShapeKind.Rectangle
        };

        var points = kind switch
        {
            ShapeKind.Line => new List<PointF> { _dragStart, _dragEnd },
            ShapeKind.Freehand => new List<PointF>(_drawingPoints),
            _ => new List<PointF>()
        };

        return new VectorShape
        {
            Kind = kind,
            Bounds = bounds,
            Points = points,
            Fill = FillColor,
            Stroke = StrokeColor
        };
    }

    private int FindShape(PointF point)
    {
        for (var i = _shapes.Count - 1; i >= 0; i--)
        {
            var bounds = _shapes[i].Bounds;
            bounds.Inflate(0.015F, 0.015F);
            if (bounds.Contains(point))
                return i;
        }

        return -1;
    }

    private static void DrawShape(Graphics graphics, VectorShape shape, RectangleF page, bool selected)
    {
        var bounds = ToPageBounds(shape.Bounds, page);
        using var pen = new Pen(shape.Stroke, Math.Max(1F, shape.StrokeWidth * page.Width / 794F));

        switch (shape.Kind)
        {
            case ShapeKind.Rectangle:
                if (shape.Fill is { } rectangleFill)
                {
                    using var brush = new SolidBrush(rectangleFill);
                    graphics.FillRectangle(brush, bounds);
                }
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                break;
            case ShapeKind.Ellipse:
                if (shape.Fill is { } ellipseFill)
                {
                    using var brush = new SolidBrush(ellipseFill);
                    graphics.FillEllipse(brush, bounds);
                }
                graphics.DrawEllipse(pen, bounds);
                break;
            case ShapeKind.Line:
            case ShapeKind.Freehand:
                var points = shape.Points.Select(point => ToPagePoint(point, page)).ToArray();
                if (points.Length > 1)
                    graphics.DrawLines(pen, points);
                else if (points.Length == 1)
                    graphics.DrawEllipse(pen, points[0].X - 2, points[0].Y - 2, 4, 4);
                break;
            case ShapeKind.Polygon:
                var vertices = new PointF[6];
                for (var i = 0; i < vertices.Length; i++)
                {
                    var angle = -Math.PI / 2 + i * Math.PI / 3;
                    vertices[i] = new PointF(
                        bounds.X + bounds.Width / 2F + (float)Math.Cos(angle) * bounds.Width / 2F,
                        bounds.Y + bounds.Height / 2F + (float)Math.Sin(angle) * bounds.Height / 2F);
                }
                if (shape.Fill is { } polygonFill)
                {
                    using var brush = new SolidBrush(polygonFill);
                    graphics.FillPolygon(brush, vertices);
                }
                graphics.DrawPolygon(pen, vertices);
                break;
            case ShapeKind.Text:
                using (var textBrush = new SolidBrush(shape.Stroke))
                using (var font = new Font("Segoe UI", Math.Clamp(page.Width * 0.045F, 10F, 36F)))
                    graphics.DrawString(shape.Text, font, textBrush, bounds.Location);
                break;
        }

        if (selected)
        {
            DrawSelectionHandles(graphics, bounds);
        }
    }

    private static void DrawSelectionHandles(Graphics graphics, RectangleF bounds)
    {
        using var selectionPen = new Pen(Color.FromArgb(30, 120, 220), 1.5F);
        using var handleBrush = new SolidBrush(Color.White);
        graphics.DrawRectangle(selectionPen, bounds.X - 3, bounds.Y - 3,
            bounds.Width + 6, bounds.Height + 6);

        var handlePoints = new[]
        {
            new PointF(bounds.Left, bounds.Top),
            new PointF(bounds.Left + bounds.Width / 2F, bounds.Top),
            new PointF(bounds.Right, bounds.Top),
            new PointF(bounds.Right, bounds.Top + bounds.Height / 2F),
            new PointF(bounds.Right, bounds.Bottom),
            new PointF(bounds.Left + bounds.Width / 2F, bounds.Bottom),
            new PointF(bounds.Left, bounds.Bottom),
            new PointF(bounds.Left, bounds.Top + bounds.Height / 2F)
        };

        foreach (var point in handlePoints)
        {
            var handle = new RectangleF(point.X - 3.5F, point.Y - 3.5F, 7F, 7F);
            graphics.FillRectangle(handleBrush, handle);
            graphics.DrawRectangle(selectionPen, handle.X, handle.Y, handle.Width, handle.Height);
        }
    }

    private static PointF ToPagePoint(PointF point, RectangleF page)
    {
        return new PointF(page.X + point.X * page.Width, page.Y + point.Y * page.Height);
    }

    private static RectangleF ToPageBounds(RectangleF bounds, RectangleF page)
    {
        return new RectangleF(
            page.X + bounds.X * page.Width,
            page.Y + bounds.Y * page.Height,
            bounds.Width * page.Width,
            bounds.Height * page.Height);
    }

    private static PointF ToNormalizedPoint(Point point, RectangleF page)
    {
        return new PointF(
            (point.X - page.X) / page.Width,
            (point.Y - page.Y) / page.Height);
    }

    private RectangleF GetPageBounds()
    {
        var size = GetPageSize();
        return new RectangleF(
            _pageOrigin?.X ?? (ClientSize.Width - size.Width) / 2F,
            _pageOrigin?.Y ?? (ClientSize.Height - size.Height) / 2F,
            size.Width,
            size.Height);
    }

    private SizeF GetPageSize()
    {
        const float pageWidth = 794F;
        const float pageHeight = 1123F;
        var scale = Math.Min(
            Math.Max(1F, ClientSize.Width - 80F) / pageWidth,
            Math.Max(1F, ClientSize.Height - 70F) / pageHeight);
        scale = Math.Clamp(scale, 0.05F, 1F) * _zoom;
        return new SizeF(pageWidth * scale, pageHeight * scale);
    }
}
