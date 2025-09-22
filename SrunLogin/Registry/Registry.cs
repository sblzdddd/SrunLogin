using System.Text.Json;
using Microsoft.Win32;
using SrunLogin.Records;

namespace SrunLogin.Registry;
using Utilities;

public static class Registry
{
    private const string RootKeyPath = "Software\\SrunLogin";
    private const string AccountValueName = "Account";
    private const string RefreshIntervalValueName = "RefreshIntervalMinutes";

    public static AccountInfoRecord? Account;

    public static void ParseAccountInfoFromFile()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RootKeyPath, false);
            var encrypted = key?.GetValue(AccountValueName) as string;
            if (string.IsNullOrWhiteSpace(encrypted))
            {
                Account = null;
                return;
            }

            var json = LinkEncryptor.Decrypt(encrypted);
            Account = JsonSerializer.Deserialize<AccountInfoRecord>(json);
        }
        catch
        {
            Account = null;
        }
    }

    public static void DumpAccountInfoToFile()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RootKeyPath, true);

            if (Account is null)
            {
                key.DeleteValue(AccountValueName, false);
                return;
            }

            var json = JsonSerializer.Serialize(Account);
            var encrypted = LinkEncryptor.Encrypt(json);
            key.SetValue(AccountValueName, encrypted, RegistryValueKind.String);
        }
        catch
        {
            // ignore
        }
    }

    public static int GetRefreshIntervalMinutes()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RootKeyPath, false);
            var value = key?.GetValue(RefreshIntervalValueName);
            switch (value)
            {
                case int intValue when intValue > 0:
                    return intValue;
                case string str when int.TryParse(str, out var parsed) && parsed > 0:
                    return parsed;
            }
        }
        catch
        {
            // ignore
        }
        return 5; // default
    }

    public static void SetRefreshIntervalMinutes(int minutes)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RootKeyPath, true);
            key.SetValue(RefreshIntervalValueName, minutes, RegistryValueKind.DWord);
        }
        catch
        {
            // ignore
        }
    }
}