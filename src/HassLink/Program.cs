using System.Diagnostics.CodeAnalysis;

namespace HassLink;

[ExcludeFromCodeCoverage]
static class Program
{
    [STAThread]
    static void Main()
    {
        // STAThread is required for WinForms — it sets up the COM single-threaded apartment
        // which is needed for clipboard, drag-and-drop, and other Windows UI interactions.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Prevent multiple instances
        using var mutex = new System.Threading.Mutex(true, @"Global\hass-link-A1B2C3D4-E5F6-7890-ABCD-EF1234567890", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "hass-link is already running. Check the system tray.",
                "hass-link",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplicationContext());
    }
}
