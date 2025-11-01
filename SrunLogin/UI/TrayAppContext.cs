using SrunLogin.Handlers;
using SrunLogin.Records;
using SrunLogin.Utilities;

namespace SrunLogin.UI;

public sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon trayIcon;
    private readonly System.Windows.Forms.Timer refreshTimer;
    private bool isRefreshing;

    public TrayAppContext()
    {
        trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Visible = true,
            Text = "SrunLogin"
        };

        var menu = new ContextMenuStrip();
        var startAtLoginItem = new ToolStripMenuItem("Start at login") { CheckOnClick = true };
        startAtLoginItem.Checked = StartUpManager.IsStartupEnabled();
        startAtLoginItem.CheckedChanged += (_, _) => StartUpManager.SetStartupEnabled(startAtLoginItem.Checked);
        menu.Items.Add(startAtLoginItem);
        
        // Rate settings submenu
        var rateMenu = new ToolStripMenuItem("Refresh Rate");
        var minutesOptions = new[] { 1, 5, 10, 30, 60 };
        var rateItems = new List<ToolStripMenuItem>();
        foreach (var minutes in minutesOptions)
        {
            string text = $"{minutes} min(s)";
            var item = new ToolStripMenuItem(text) { CheckOnClick = true };
            var m = minutes; // capture for closure
            item.Click += (_, _) => SetRefreshRate(m, rateItems.ToArray());
            rateItems.Add(item);
        }
        
        var initialMinutes = Registry.Registry.GetRefreshIntervalMinutes();
        refreshTimer = new System.Windows.Forms.Timer();
        refreshTimer.Interval = initialMinutes * 60 * 1000;
        
        bool matched = false;
        for (int i = 0; i < minutesOptions.Length; i++)
        {
            if (minutesOptions[i] == initialMinutes)
            {
                rateItems[i].Checked = true;
                matched = true;
                break;
            }
        }
        if (!matched)
        {
            int idx = Array.IndexOf(minutesOptions, 5);
            if (idx >= 0) rateItems[idx].Checked = true;
        }
        
        foreach (var item in rateItems)
        {
            rateMenu.DropDownItems.Add(item);
        }
        
        menu.Items.Add(rateMenu);
        menu.Items.Add("Refresh Now", null, (_, _) => _ = RefreshNowAsync());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) => ShowAccountForm();

        refreshTimer.Tick += async (_, _) => await RefreshNowAsync();

        EnsureAccount();

        _ = RefreshNowAsync();
        refreshTimer.Start();
    }

    private void EnsureAccount()
    {
        Registry.Registry.ParseAccountInfoFromFile();
        if (Registry.Registry.Account is not null) return;

        using var form = new AccountPromptForm();
        var result = form.ShowDialog();
        if (result != DialogResult.OK)
        {
            // User cancelled; keep running in tray but show tooltip
            ShowBalloon("SrunLogin", "No account configured. Right-click to exit.", ToolTipIcon.Warning);
        }
    }

    private void ShowAccountForm()
    {
        using var form = new AccountPromptForm();
        form.ShowDialog();
    }

    private async Task RefreshNowAsync()
    {
        if (isRefreshing) return;
        isRefreshing = true;
        try
        {
            var ip = NetworkDetector.GetIpAddress() ?? string.Empty;
            if (string.IsNullOrEmpty(ip))
            {
                ShowBalloon("SrunLogin", "No IP found.", ToolTipIcon.Error);
                return;
            }

            if (!NetworkDetector.IsConnectedToWiFi())
            {
                // ShowBalloon("SrunLogin", "Not connected to target WiFi.", ToolTipIcon.Warning);
                return;
            }

            // Already online?
            if (await LoginHandler.IsOnline())
            {
                trayIcon.Text = "SrunLogin - Online";
                // ShowBalloon("SrunLogin", "Already connected - no login needed.", ToolTipIcon.Info);
                return;
            }

            // Need account
            var account = GetAccount();
            if (account is null)
            {
                using var form = new AccountPromptForm();
                if (form.ShowDialog() != DialogResult.OK)
                {
                    ShowBalloon("SrunLogin", "Login cancelled.", ToolTipIcon.Info);
                    return;
                }
                account = Registry.Registry.Account;
            }
            
            await LoginHandler.Login(account!, ip);
            trayIcon.Text = "SrunLogin - Online";
            ShowBalloon("SrunLogin", "Login successful!", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            trayIcon.Text = "SrunLogin - Error";
            ShowBalloon("SrunLogin", $"Login failed: {ex.Message}", ToolTipIcon.Error);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private static AccountInfoRecord? GetAccount()
    {
        Registry.Registry.ParseAccountInfoFromFile();
        return Registry.Registry.Account;
    }

    private void ExitApplication()
    {
        refreshTimer.Stop();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        ExitThread();
        Application.Exit();
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        trayIcon.BalloonTipIcon = icon;
        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = message;
        trayIcon.ShowBalloonTip(3000);
    }

    private void SetRefreshRate(int minutes, params ToolStripMenuItem[] menuItems)
    {
        refreshTimer.Interval = minutes * 60 * 1000;
        Registry.Registry.SetRefreshIntervalMinutes(minutes);
        
        // Update menu check states
        foreach (var item in menuItems)
        {
            item.Checked = false;
        }
        
        // Find and check the selected item
        var selectedItem = menuItems.FirstOrDefault(item => 
            item.Text?.Contains(minutes.ToString()) == true);
        
        if (selectedItem is not null)
        {
            selectedItem.Checked = true;
        }
        
        ShowBalloon("SrunLogin", $"Refresh rate set to {minutes} minute{(minutes == 1 ? "" : "s")}", ToolTipIcon.Info);
    }
}
