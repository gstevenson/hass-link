using System.Diagnostics.CodeAnalysis;
using HassLink.Config;

namespace HassLink.Forms;

[ExcludeFromCodeCoverage]
public class CommandEditDialog : Form
{
    private readonly TextBox _tbName;
    private readonly TextBox _tbExecutable;
    private readonly TextBox _tbArguments;

    public string CommandName => _tbName.Text.Trim();
    public string Executable  => _tbExecutable.Text.Trim();
    public string Arguments   => _tbArguments.Text.Trim();

    public CommandEditDialog(CommandConfig? existing = null)
    {
        Text = existing is null ? "Add Command" : "Edit Command";
        Size = new Size(440, 215);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        _tbName       = new TextBox { Dock = DockStyle.Fill };
        _tbExecutable = new TextBox { Dock = DockStyle.Fill };
        _tbArguments  = new TextBox { Dock = DockStyle.Fill };

        var btnBrowse = new Button { Text = "Browse...", Dock = DockStyle.Fill, Margin = new Padding(2, 0, 0, 0) };
        btnBrowse.Click += OnBrowse;

        AddLabeledRow(layout, "Name:",       _tbName,       null);
        AddLabeledRow(layout, "Executable:", _tbExecutable, btnBrowse);
        AddLabeledRow(layout, "Arguments:",  _tbArguments,  null);

        var btnOk     = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Width = 80 };
        var btnCancel = new Button { Text = "Cancel",  DialogResult = DialogResult.Cancel, Width = 80 };
        btnOk.Click += OnOk;

        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            WrapContents = false,
        };
        btnFlow.Controls.Add(btnCancel);
        btnFlow.Controls.Add(btnOk);

        Controls.Add(layout);
        Controls.Add(btnFlow);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        if (existing is not null)
        {
            _tbName.Text       = existing.Name;
            _tbExecutable.Text = existing.Executable ?? "";
            _tbArguments.Text  = existing.Arguments ?? "";
        }
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tbName.Text))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title  = "Select executable",
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _tbExecutable.Text = dlg.FileName;
    }

    private static void AddLabeledRow(TableLayoutPanel layout, string label, Control control, Control? extra)
    {
        layout.Controls.Add(new Label
        {
            Text      = label,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize  = true,
            Padding   = new Padding(0, 4, 4, 0),
        });

        if (extra is null)
            layout.SetColumnSpan(control, 2);

        layout.Controls.Add(control);

        if (extra is not null)
            layout.Controls.Add(extra);
    }
}
