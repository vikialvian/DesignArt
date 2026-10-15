namespace Corel;

public sealed class EditorPalette : UserControl
{
    public event Action<Color?>? ColorSelected;

    public EditorPalette()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(49, 52, 56);
        Padding = new Padding(10, 6, 4, 4);

        var swatches = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = BackColor
        };

        var noFill = new Button
        {
            Text = "×",
            Width = 24,
            Height = 24,
            BackColor = Color.White,
            ForeColor = Color.Firebrick,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(2, 0, 5, 0),
            AccessibleName = "Tanpa isi"
        };
        noFill.FlatAppearance.BorderColor = Color.FromArgb(125, 130, 136);
        noFill.Click += (_, _) => ColorSelected?.Invoke(null);
        swatches.Controls.Add(noFill);

        Color[] colors =
        [
            Color.White,
            Color.Black,
            Color.FromArgb(220, 55, 55),
            Color.FromArgb(245, 145, 45),
            Color.FromArgb(245, 205, 55),
            Color.FromArgb(65, 170, 95),
            Color.FromArgb(42, 132, 230),
            Color.FromArgb(120, 85, 200),
            Color.FromArgb(220, 90, 155),
            Color.FromArgb(110, 115, 120)
        ];

        foreach (var color in colors)
        {
            var swatch = new Button
            {
                Width = 24,
                Height = 24,
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 3, 0),
                Tag = color,
                AccessibleName = $"Warna {color.Name}"
            };
            swatch.FlatAppearance.BorderColor = Color.FromArgb(125, 130, 136);
            swatch.FlatAppearance.BorderSize = 1;
            swatch.Click += (_, _) => ColorSelected?.Invoke((Color)swatch.Tag!);
            swatches.Controls.Add(swatch);
        }

        Controls.Add(swatches);
    }
}
