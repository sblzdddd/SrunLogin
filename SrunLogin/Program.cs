using SrunLogin.UI;

namespace SrunLogin;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Ensure application icon is available from the exe for forms that may query it
        try
        {
            _ = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // ignore
        }

        Application.Run(new TrayAppContext());
    }
}