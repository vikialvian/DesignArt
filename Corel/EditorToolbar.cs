namespace Corel;

public sealed class EditorToolbar : UserControl
{
    public event Action? NewDocumentRequested;
    public event Action? SaveRequested;
    public event Action? FitPageRequested;
    public event Action<EditorTool>? ToolSelected;

    public EditorToolbar()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var menu = new MenuStrip
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(31, 33, 36),
            ForeColor = Color.WhiteSmoke,
            Padding = new Padding(8, 2, 0, 2)
        };

        var file = new ToolStripMenuItem("File");
        var newItem = new ToolStripMenuItem("Dokumen baru");
        newItem.ShortcutKeys = Keys.Control | Keys.N;
        newItem.Click += (_, _) => NewDocumentRequested?.Invoke();
        var saveItem = new ToolStripMenuItem("Simpan");
        saveItem.ShortcutKeys = Keys.Control | Keys.S;
        saveItem.Click += (_, _) => SaveRequested?.Invoke();
        var exitItem = new ToolStripMenuItem("Keluar");
        exitItem.Click += (_, _) => FindForm()?.Close();
        file.DropDownItems.Add(newItem);
        file.DropDownItems.Add(saveItem);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(exitItem);
        menu.Items.Add(file);
        menu.Items.Add(new ToolStripMenuItem("Edit"));
        var view = new ToolStripMenuItem("Tampilan");
        var fitPage = new ToolStripMenuItem("Sesuaikan halaman");
        fitPage.ShortcutKeys = Keys.Control | Keys.D0;
        fitPage.Click += (_, _) => FitPageRequested?.Invoke();
        view.DropDownItems.Add(fitPage);
        menu.Items.Add(view);
        menu.Items.Add(new ToolStripMenuItem("Objek"));
        menu.Items.Add(new ToolStripMenuItem("Bantuan"));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.FromArgb(49, 52, 56),
            Padding = new Padding(8, 4, 0, 3),
            Margin = Padding.Empty
        };

        buttons.Controls.Add(CreateButton(EditorIconKind.NewDocument, "Baru", () => NewDocumentRequested?.Invoke()));
        buttons.Controls.Add(CreateButton(EditorIconKind.Save, "Simpan", () => SaveRequested?.Invoke()));
        buttons.Controls.Add(CreateButton(EditorIconKind.Select, "Pilih — Shift+klik tambah/hapus; seret untuk seleksi area", () => ToolSelected?.Invoke(EditorTool.Select)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Pan, "Pan — klik kiri tahan lalu seret", () => ToolSelected?.Invoke(EditorTool.Pan)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Rectangle, "Persegi", () => ToolSelected?.Invoke(EditorTool.Rectangle)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Ellipse, "Elips", () => ToolSelected?.Invoke(EditorTool.Ellipse)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Line, "Garis", () => ToolSelected?.Invoke(EditorTool.Line)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Freehand, "Pensil", () => ToolSelected?.Invoke(EditorTool.Freehand)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Polygon, "Poligon", () => ToolSelected?.Invoke(EditorTool.Polygon)));
        buttons.Controls.Add(CreateButton(EditorIconKind.Text, "Teks", () => ToolSelected?.Invoke(EditorTool.Text)));
        buttons.Controls.Add(CreateButton(EditorIconKind.FitPage, "Muat", () => FitPageRequested?.Invoke()));

        layout.Controls.Add(menu, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        Controls.Add(layout);
    }

    private static EditorIconButton CreateButton(EditorIconKind icon, string caption, Action action)
    {
        var button = new EditorIconButton(icon, caption);
        button.Click += (_, _) => action();
        return button;
    }
}
