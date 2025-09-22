using SrunLogin.Records;
using SrunLogin.Handlers;

namespace SrunLogin.UI;

public sealed class AccountPromptForm : Form
{
    private readonly TextBox username;
    private readonly TextBox password;
    private readonly Button save;

    public AccountPromptForm()
    {
        Text = "SrunLogin | Status: Checking...";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(500, 260);

        try
        {
            var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon is not null)
            {
                Icon = exeIcon;
            }
        }
        catch
        {
            // ignore
        }

        // ASCII art header
        string logo = string.Empty;
        try
        {
            using var logoStream = typeof(Program).Assembly.GetManifestResourceStream("SrunLogin.storage.logo.txt");
            if (logoStream is not null)
            {
                using var reader = new StreamReader(logoStream);
                logo = reader.ReadToEnd();
            }
        }
        catch
        {
            // ignore
        }

        var logoLabel = new Label
        {
            Text = logo,
            Location = new Point(12, 10),
            Size = new Size(480, 100),
            AutoSize = false,
            Font = new Font(FontFamily.GenericMonospace, 5.6f),
        };

        var lblUser = new Label { Text = "Portal Username", Location = new Point(12, 140), AutoSize = true };
        username = new TextBox { Location = new Point(160, 137), Width = 320 };
        var lblPass = new Label { Text = "Portal Password", Location = new Point(12, 180), AutoSize = true };
        password = new TextBox { Location = new Point(160, 177), Width = 320, UseSystemPasswordChar = true };

        save = new Button { Text = "Save", Location = new Point(160, 220), DialogResult = DialogResult.OK, Height = 36, Width = 120 };
        var cancel1 = new Button { Text = "Cancel", Location = new Point(300, 220), DialogResult = DialogResult.Cancel, Height = 36, Width = 120 };

        save.Click += (_, _) => OnSave();

        Controls.Add(logoLabel);
        Controls.Add(lblUser);
        Controls.Add(username);
        Controls.Add(lblPass);
        Controls.Add(password);
        Controls.Add(save);
        Controls.Add(cancel1);

        // Pre-fill if available
        Registry.Registry.ParseAccountInfoFromFile();
        if (Registry.Registry.Account is not null)
        {
            username.Text = Registry.Registry.Account.PortalUsername;
            password.Text = Registry.Registry.Account.PortalPassword;
        }

        // Check connection status asynchronously
        _ = CheckConnectionStatusAsync();

        // Decide if we must exit on cancel/close (no stored password)
        var mustExitOnCancel1 = Registry.Registry.Account is null || string.IsNullOrWhiteSpace(Registry.Registry.Account.PortalPassword);

        // If the user cancels or closes the form without saving and no password is stored, exit the app
        FormClosing += (_, _) =>
        {
            if (mustExitOnCancel1 && DialogResult != DialogResult.OK)
            {
                Application.Exit();
            }
        };
    }

    private void OnSave()
    {
        var portalUsername = username.Text.Trim();
        var portalPassword = password.Text;
        if (string.IsNullOrWhiteSpace(portalUsername) || string.IsNullOrWhiteSpace(portalPassword))
        {
            MessageBox.Show(this, "Username and password are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Registry.Registry.Account = new AccountInfoRecord(portalUsername, portalPassword);
        Registry.Registry.DumpAccountInfoToFile();
        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task CheckConnectionStatusAsync()
    {
        try
        {
            var isOnline = await LoginHandler.IsOnline();
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatusLabel(isOnline));
            }
            else
            {
                UpdateStatusLabel(isOnline);
            }
        }
        catch
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatusLabel(false, "Error checking status"));
            }
            else
            {
                UpdateStatusLabel(false, "Error checking status");
            }
        }
    }

    private void UpdateStatusLabel(bool isOnline, string? customMessage = null)
    {
        if (!string.IsNullOrEmpty(customMessage))
        {
            Text = $"SrunLogin | {customMessage}";
        }
        else if (isOnline)
        {
            Text = "SrunLogin | Online";
        }
        else
        {
            Text = "SrunLogin | Offline";
        }
    }
}
