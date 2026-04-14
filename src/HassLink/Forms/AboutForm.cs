using System.Security.Principal;

namespace HassLink.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About hass-link";
        Size = new Size(360, 240);
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

        var btnClose = new Button { Text = "Close", DialogResult = DialogResult.OK, Width = 80, Margin = new Padding(0, 16, 0, 0) };
        panel.Controls.Add(btnClose);
        AcceptButton = btnClose;

        Controls.Add(panel);
    }
}
