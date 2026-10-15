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
    private PointF _transformStart;
    private VectorShape? _transformSource;
    private float _zoom = 1F;
    private int _selectedShape = -1;
    private int _resizeHandle = -1;
    private bool _isDrawing;
    private bool _isPanning;
    private bool _isTransforming;
    private TransformMode _transformMode;

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
            var handle = GetHandleAt(e.Location);
            if (handle >= 0 && _selectedShape >= 0)
            {
                BeginTransform(TransformMode.Resize, point, handle);
                return;
            }

            _selectedShape = FindShape(point);
            var selected = _selectedShape >= 0 ? _shapes[_selectedShape] : null;
            SelectionChanged?.Invoke(selected);
            if (selected is not null)
            {
                BeginTransform(TransformMode.Move, point, -1);
                StatusChanged?.Invoke("Objek dipilih — seret untuk memindahkan");
            }
            else
            {
                StatusChanged?.Invoke("Tidak ada objek dipilih");
            }
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

        if (_isTransforming)
        {
            UpdateTransform(ToNormalizedPoint(e.Location, GetPageBounds()));
            Invalidate();
            return;
        }

        if (!_isDrawing)
        {
            UpdateSelectionCursor(e.Location);
            return;
        }

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

        if (_isTransforming && e.Button == MouseButtons.Left)
        {
            _isTransforming = false;
            Capture = false;
            var selected = _selectedShape >= 0 ? _shapes[_selectedShape] : null;
            SelectionChanged?.Invoke(selected);
            StatusChanged?.Invoke(_transformMode == TransformMode.Move ? "Objek dipindahkan" : "Ukuran objek diubah");
            _transformSource = null;
            UpdateSelectionCursor(e.Location);
            Invalidate();
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

    private void BeginTransform(TransformMode mode, PointF start, int handle)
    {
        _transformMode = mode;
        _resizeHandle = handle;
        _transformStart = start;
        _transformSource = _shapes[_selectedShape];
        _isTransforming = true;
        Capture = true;
        Cursor = mode == TransformMode.Move ? Cursors.SizeAll : GetResizeCursor(handle);
    }

    private void UpdateTransform(PointF current)
    {
        if (_transformSource is null || _selectedShape < 0)
            return;

        var source = _transformSource;
        if (_transformMode == TransformMode.Move)
        {
            var delta = new PointF(current.X - _transformStart.X, current.Y - _transformStart.Y);
            var movedBounds = new RectangleF(
                source.Bounds.X + delta.X,
                source.Bounds.Y + delta.Y,
                source.Bounds.Width,
                source.Bounds.Height);
            var movedPoints = source.Points
                .Select(point => new PointF(point.X + delta.X, point.Y + delta.Y))
                .ToList();
            _shapes[_selectedShape] = CopyShape(source, movedBounds, movedPoints, source.TextScale);
            return;
        }

        var resizedBounds = ResizeBounds(source.Bounds, current, _resizeHandle);
        var sourceWidth = Math.Max(Math.Abs(source.Bounds.Width), 0.0001F);
        var sourceHeight = Math.Max(Math.Abs(source.Bounds.Height), 0.0001F);
        var scaleX = resizedBounds.Width / sourceWidth;
        var scaleY = resizedBounds.Height / sourceHeight;
        var resizedPoints = source.Points.Select(point => new PointF(
            resizedBounds.Left + (point.X - source.Bounds.Left) / sourceWidth * resizedBounds.Width,
            resizedBounds.Top + (point.Y - source.Bounds.Top) / sourceHeight * resizedBounds.Height)).ToList();

        _shapes[_selectedShape] = CopyShape(
            source,
            resizedBounds,
            resizedPoints,
            source.TextScale * Math.Max(scaleX, scaleY));
    }

    private static RectangleF ResizeBounds(RectangleF source, PointF current, int handle)
    {
        var left = source.Left;
        var top = source.Top;
        var right = source.Right;
        var bottom = source.Bottom;

        if (handle is 0 or 6 or 7) left = current.X;
        if (handle is 2 or 3 or 4) right = current.X;
        if (handle is 0 or 1 or 2) top = current.Y;
        if (handle is 4 or 5 or 6) bottom = current.Y;

        return new RectangleF(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top));
    }

    private static VectorShape CopyShape(VectorShape source, RectangleF bounds, List<PointF> points, float textScale)
    {
        return new VectorShape
        {
            Kind = source.Kind,
            Bounds = bounds,
            Points = points,
            Fill = source.Fill,
            Stroke = source.Stroke,
            StrokeWidth = source.StrokeWidth,
            TextScale = Math.Max(0.05F, textScale),
            Text = source.Text
        };
    }

    private void UpdateSelectionCursor(Point point)
    {
        if (ActiveTool != EditorTool.Select)
            return;

        var handle = GetHandleAt(point);
        if (handle >= 0)
        {
            Cursor = GetResizeCursor(handle);
            return;
        }

        if (_selectedShape >= 0 && _selectedShape < _shapes.Count &&
            ToPageBounds(_shapes[_selectedShape].Bounds, GetPageBounds()).Contains(point))
        {
            Cursor = Cursors.SizeAll;
            return;
        }

        Cursor = Cursors.Default;
    }

    private int GetHandleAt(Point point)
    {
        if (_selectedShape < 0 || _selectedShape >= _shapes.Count)
            return -1;

        var bounds = ToPageBounds(_shapes[_selectedShape].Bounds, GetPageBounds());
        var handles = GetHandlePoints(GetHandleBounds(bounds));
        for (var i = 0; i < handles.Length; i++)
        {
            var dx = point.X - handles[i].X;
            var dy = point.Y - handles[i].Y;
            if (dx * dx + dy * dy <= 64F)
                return i;
        }

        return -1;
    }

    private static Cursor GetResizeCursor(int handle)
    {
        return handle switch
        {
            0 or 4 => Cursors.SizeNWSE,
            2 or 6 => Cursors.SizeNESW,
            1 or 5 => Cursors.SizeNS,
            _ => Cursors.SizeWE
        };
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
                using (var font = new Font("Segoe UI", Math.Clamp(page.Width * 0.045F * shape.TextScale, 6F, 128F)))
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
        bounds = GetHandleBounds(bounds);
        using var selectionPen = new Pen(Color.FromArgb(30, 120, 220), 1.5F);
        using var handleBrush = new SolidBrush(Color.White);
        graphics.DrawRectangle(selectionPen, bounds.X - 3, bounds.Y - 3,
            bounds.Width + 6, bounds.Height + 6);

        foreach (var point in GetHandlePoints(bounds))
        {
            var handle = new RectangleF(point.X - 3.5F, point.Y - 3.5F, 7F, 7F);
            graphics.FillRectangle(handleBrush, handle);
            graphics.DrawRectangle(selectionPen, handle.X, handle.Y, handle.Width, handle.Height);
        }
    }

    private static RectangleF GetHandleBounds(RectangleF bounds)
    {
        const float minimumSize = 10F;
        if (bounds.Width < minimumSize)
        {
            bounds.X -= (minimumSize - bounds.Width) / 2F;
            bounds.Width = minimumSize;
        }
        if (bounds.Height < minimumSize)
        {
            bounds.Y -= (minimumSize - bounds.Height) / 2F;
            bounds.Height = minimumSize;
        }
        return bounds;
    }

    private static PointF[] GetHandlePoints(RectangleF bounds)
    {
        return
        [
            new PointF(bounds.Left, bounds.Top),
            new PointF(bounds.Left + bounds.Width / 2F, bounds.Top),
            new PointF(bounds.Right, bounds.Top),
            new PointF(bounds.Right, bounds.Top + bounds.Height / 2F),
            new PointF(bounds.Right, bounds.Bottom),
            new PointF(bounds.Left + bounds.Width / 2F, bounds.Bottom),
            new PointF(bounds.Left, bounds.Bottom),
            new PointF(bounds.Left, bounds.Top + bounds.Height / 2F)
        ];
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

    private enum TransformMode
    {
        Move,
        Resize
    }
}
