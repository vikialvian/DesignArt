namespace Corel;

internal sealed class TextEntryDialog : Form
{
    private readonly TextBox _textBox;

    public string EnteredText => _textBox.Text.Trim();

    public TextEntryDialog()
    {
        Text = "Tambahkan teks";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(340, 112);
        Font = new Font("Segoe UI", 9F);

        _textBox = new TextBox
        {
            Dock = DockStyle.Top,
            Margin = new Padding(10),
            PlaceholderText = "Ketik teks..."
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(6)
        };

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 78 };
        var cancel = new Button { Text = "Batal", DialogResult = DialogResult.Cancel, Width = 78 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(buttons);
        Controls.Add(_textBox);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
