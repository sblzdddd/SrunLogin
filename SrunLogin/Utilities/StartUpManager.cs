namespace SrunLogin.Utilities;

public abstract class StartUpManager
{
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var value = key?.GetValue("SrunLogin") as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (key is null) return;
            if (enabled)
            {
                var exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue("SrunLogin", $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue("SrunLogin", false);
            }
        }
        catch
        {
            // ignore
        }
    }
}