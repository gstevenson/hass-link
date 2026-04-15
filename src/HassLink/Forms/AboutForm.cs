using System.Security.Principal;

namespace HassLink.Forms;

public class AboutForm : Form
{
    private readonly Func<string> _getDiagnostics;
    private readonly Bitmap? _iconBitmap;

    public AboutForm(Func<string> getDiagnostics)
    {
        _getDiagnostics = getDiagnostics;

        Text = "About hass-link";
        Size = new Size(360, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(20, 20, 20, 12),
        };

        // App icon — extracted from the exe so it always matches the built-in icon
        // Load from embedded resource — gives us the 256px version to downsample cleanly
        using var stream = typeof(AboutForm).Assembly.GetManifestResourceStream("icon.ico");
        if (stream is not null)
        {
            using var fullIcon = new Icon(stream);
            using var largeIcon = new Icon(fullIcon, 256, 256);
            using var largeBitmap = largeIcon.ToBitmap();
            _iconBitmap = new Bitmap(64, 64);
            using var g = Graphics.FromImage(_iconBitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawImage(largeBitmap, 0, 0, 64, 64);
            panel.Controls.Add(new PictureBox
            {
                Image = _iconBitmap,
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Normal,
                Margin = new Padding(0, 0, 0, 8),
            });
        }

        // Strip the git SHA from the informational version — show just "0.2.x"
        var version = Application.ProductVersion.Split('+')[0];

        panel.Controls.Add(new Label { Text = "hass-link", Font = new Font(Font.FontFamily, 16, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(new Label { Text = $"Version {version}", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
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

        var website = new LinkLabel { Text = "gstevenson.github.io/hass-link", AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
        website.LinkClicked += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://gstevenson.github.io/hass-link/") { UseShellExecute = true }); }
            catch { }
        };
        panel.Controls.Add(website);

        var btnDiagnostics = new Button { Text = "Diagnostics...", AutoSize = true, Margin = new Padding(0, 16, 8, 0) };
        var btnClose = new Button { Text = "Close", AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
        btnClose.Click += (_, _) => Close();
        btnDiagnostics.Click += OnShowDiagnostics;

        var btnRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
        };
        btnRow.Controls.Add(btnDiagnostics);
        btnRow.Controls.Add(btnClose);

        panel.Controls.Add(btnRow);
        AcceptButton = btnClose;
        Controls.Add(panel);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _iconBitmap?.Dispose();
        base.Dispose(disposing);
    }

    private void OnShowDiagnostics(object? sender, EventArgs e)
    {
        var report = _getDiagnostics();

        var dlg = new Form
        {
            Text = "Diagnostics",
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
