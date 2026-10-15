namespace Corel;

public sealed class EditorPropertiesPanel : UserControl
{
    private readonly Label _selectionLabel;

    public EditorPropertiesPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(39, 42, 46);
        Padding = new Padding(14, 12, 14, 8);

        var title = new Label
        {
            Text = "Properti",
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var documentInfo = new Label
        {
            Text = "Dokumen\nUkuran     A4\nOrientasi  Potret\nSatuan      Piksel",
            Dock = DockStyle.Top,
            Height = 112,
            ForeColor = Color.FromArgb(205, 209, 214),
            Padding = new Padding(0, 8, 0, 0)
        };

        _selectionLabel = new Label
        {
            Text = "Belum ada objek dipilih",
            Dock = DockStyle.Top,
            Height = 74,
            ForeColor = Color.FromArgb(151, 156, 162),
            Padding = new Padding(0, 14, 0, 0)
        };

        Controls.Add(_selectionLabel);
        Controls.Add(documentInfo);
        Controls.Add(title);
    }

    public void ShowSelection(VectorShape? shape)
    {
        _selectionLabel.Text = shape is null
            ? "Belum ada objek dipilih"
            : $"Objek\nJenis: {shape.Kind}\nIsi: {shape.Fill?.Name ?? "Tanpa isi"}\nGaris: {shape.Stroke.Name}";
    }
}
