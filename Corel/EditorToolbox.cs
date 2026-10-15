namespace Corel;

public sealed class EditorToolbox : UserControl
{
    public event Action<EditorTool>? ToolSelected;

    public EditorToolbox()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(39, 42, 46);
        Padding = new Padding(6, 8, 6, 6);

        var tools = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = BackColor
        };

        AddTool(tools, EditorIconKind.Select, "Pilih", EditorTool.Select);
        AddTool(tools, EditorIconKind.Pan, "Pan — klik kiri tahan lalu seret", EditorTool.Pan);
        AddTool(tools, EditorIconKind.Rectangle, "Persegi", EditorTool.Rectangle);
        AddTool(tools, EditorIconKind.Ellipse, "Elips", EditorTool.Ellipse);
        AddTool(tools, EditorIconKind.Line, "Garis", EditorTool.Line);
        AddTool(tools, EditorIconKind.Freehand, "Pensil", EditorTool.Freehand);
        AddTool(tools, EditorIconKind.Polygon, "Poligon", EditorTool.Polygon);
        AddTool(tools, EditorIconKind.Text, "Teks", EditorTool.Text);

        Controls.Add(tools);
    }

    private void AddTool(FlowLayoutPanel panel, EditorIconKind icon, string label, EditorTool tool)
    {
        var button = new EditorIconButton(icon, label, compact: true);
        button.Click += (_, _) => ToolSelected?.Invoke(tool);
        panel.Controls.Add(button);
    }
}
