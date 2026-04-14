using System.Security.Principal;

namespace HassLink.Forms;

public class AboutForm : Form
{
    private readonly Func<string> _getDiagnostics;

    public AboutForm(Func<string> getDiagnostics)
    {
        _getDiagnostics = getDiagnostics;

        Text = "About hass-link";
        Size = new Size(360, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(20),
        };

        panel.Controls.Add(new Label { Text = "hass-link", Font = new Font(Font.FontFamily, 16, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(new Label { Text = $"Version {Application.ProductVersion}", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
        panel.Controls.Add(new Label { Text = "Windows → Home Assistant via MQTT", AutoSize = true, Padding = new Padding(0, 8, 0, 0), ForeColor = Color.Gray });
        panel.Controls.Add(new Label { Text = $"Running on {Environment.OSVersion}", AutoSize = true, Padding = new Padding(0, 4, 0, 0), ForeColor = Color.Gray });

        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        panel.Controls.Add(new Label
        {
            Text = isAdmin ? "Running as Administrator" : "Not running as Administrator",
            ForeColor = isAdmin ? Color.DarkGreen : Color.DarkOrange,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0),
        });

        var link = new LinkLabel { Text = "github.com/gstevenson/hass-link", AutoSize = true, Padding = new Padding(0, 12, 0, 0) };
        link.LinkClicked += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/gstevenson/hass-link") { UseShellExecute = true }); }
            catch { }
        };
        panel.Controls.Add(link);

        var btnPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 16, 0, 0),
        };

        var btnDiagnostics = new Button { Text = "Hardware Diagnostics...", AutoSize = true };
        btnDiagnostics.Click += OnShowDiagnostics;
        btnPanel.Controls.Add(btnDiagnostics);

        var btnClose = new Button { Text = "Close", DialogResult = DialogResult.OK, Width = 80, Margin = new Padding(8, 0, 0, 0) };
        btnPanel.Controls.Add(btnClose);
        AcceptButton = btnClose;

        panel.Controls.Add(btnPanel);
        Controls.Add(panel);
    }

    private void OnShowDiagnostics(object? sender, EventArgs e)
    {
        var report = _getDiagnostics();

        var dlg = new Form
        {
            Text = "Hardware Diagnostics",
            Size = new Size(620, 480),
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
        };

        var txt = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9f),
            Text = report,
        };

        var btnCopy = new Button
        {
            Text = "Copy to Clipboard",
            Dock = DockStyle.Bottom,
            Height = 32,
        };
        btnCopy.Click += (_, _) => Clipboard.SetText(report);

        dlg.Controls.Add(txt);
        dlg.Controls.Add(btnCopy);
        dlg.ShowDialog(this);
    }
}
