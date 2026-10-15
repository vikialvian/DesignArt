namespace Corel;

public sealed class MainForm : Form
{
    private readonly EditorCanvas _canvas;
    private readonly EditorPropertiesPanel _properties;
    private readonly Label _statusLabel;

    public MainForm()
    {
        Text = "Vectora — Vector Editor";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 680);
        ClientSize = new Size(1440, 900);
        BackColor = Color.FromArgb(37, 39, 42);
        Font = new Font("Segoe UI", 9F);
        KeyPreview = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 67));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

        var toolbar = new EditorToolbar();
        var toolbox = new EditorToolbox();
        _canvas = new EditorCanvas();
        _properties = new EditorPropertiesPanel();
        var palette = new EditorPalette();

        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(45, 48, 52),
            Margin = Padding.Empty
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        workspace.Controls.Add(toolbox, 0, 0);
        workspace.Controls.Add(_canvas, 1, 0);
        workspace.Controls.Add(_properties, 2, 0);

        var statusBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(31, 33, 36),
            Padding = new Padding(12, 3, 12, 3),
            Margin = Padding.Empty
        };
        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(190, 194, 199),
            Text = "Siap  |  Halaman A4  |  100%",
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusBar.Controls.Add(_statusLabel);

        toolbar.NewDocumentRequested += _canvas.ClearDocument;
        toolbar.SaveRequested += () => SetStatus("Ekspor file akan ditambahkan berikutnya");
        toolbar.FitPageRequested += _canvas.FitPage;
        toolbar.ToolSelected += SetTool;
        toolbox.ToolSelected += SetTool;
        palette.ColorSelected += color =>
        {
            _canvas.FillColor = color;
            SetStatus(color is null ? "Isi objek dimatikan" : $"Warna isi: {color.Value.Name}");
        };
        _canvas.StatusChanged += SetStatus;
        _canvas.ZoomChanged += _ => SetStatus("Zoom diperbarui");
        _canvas.SelectionChanged += _properties.ShowSelection;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(workspace, 0, 1);
        root.Controls.Add(palette, 0, 2);
        root.Controls.Add(statusBar, 0, 3);
        Controls.Add(root);
    }

    private void SetTool(EditorTool tool)
    {
        _canvas.ActiveTool = tool;
        _canvas.Cursor = tool switch
        {
            EditorTool.Select => Cursors.Default,
            EditorTool.Pan => Cursors.Hand,
            _ => Cursors.Cross
        };
        SetStatus($"Alat aktif: {tool}");
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = $"{message}  |  Halaman A4  |  {_canvas.ZoomPercent}%";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.N))
        {
            _canvas.ClearDocument();
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            SetStatus("Ekspor file akan ditambahkan berikutnya");
            return true;
        }

        if (keyData == (Keys.Control | Keys.D0))
        {
            _canvas.FitPage();
            return true;
        }

        if (keyData == Keys.Delete || keyData == Keys.Back)
        {
            _canvas.DeleteSelection();
            return true;
        }

        if (keyData == Keys.Escape)
        {
            SetTool(EditorTool.Select);
            return true;
        }

        if (keyData == Keys.Add || keyData == Keys.Oemplus)
        {
            _canvas.ZoomBy(1.1F);
            return true;
        }

        if (keyData == Keys.Subtract || keyData == Keys.OemMinus)
        {
            _canvas.ZoomBy(1F / 1.1F);
            return true;
        }

        if (keyData == Keys.V) SetTool(EditorTool.Select);
        else if (keyData == Keys.H) SetTool(EditorTool.Pan);
        else if (keyData == Keys.R) SetTool(EditorTool.Rectangle);
        else if (keyData == Keys.E) SetTool(EditorTool.Ellipse);
        else if (keyData == Keys.L) SetTool(EditorTool.Line);
        else if (keyData == Keys.P) SetTool(EditorTool.Freehand);
        else if (keyData == Keys.G) SetTool(EditorTool.Polygon);
        else if (keyData == Keys.T) SetTool(EditorTool.Text);
        else return base.ProcessCmdKey(ref msg, keyData);

        return true;
    }
}
