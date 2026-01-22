using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using ScreenshotUploader.Models;
using ScreenshotUploader.Services;

namespace ScreenshotUploader;

public partial class SettingsForm : Form
{
    private readonly ConfigurationService _configService;
    private AppSettings _settings;

    private const string DefaultApiBaseUrl = "https://www.echoairlines.com/api";

    private PictureBox? _picLogo;
    private Label? _lblHeaderTitle;
    private Label? _lblHeaderSubtitle;

    private TextBox? _txtApiBaseUrl;
    private TextBox? _txtApiKey;
    private TextBox? _txtScreenshotFolder;
    private CheckBox? _chkAutoDetectFlight;
    private TextBox? _txtManualFlightId;
    private CheckBox? _chkEnableFsuipc;
    private Label? _lblFsuipcNote;
    private Button? _btnBrowseFolder;
    private Button? _btnSave;
    private Button? _btnCancel;
    private Button? _btnDebugNotification;

    public SettingsForm(ConfigurationService configService)
    {
        _configService = configService;
        _settings = _configService.LoadSettings();
        
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Echo Airlines Screenshot Uploader - Settings";
        this.Size = new System.Drawing.Size(720, 520);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Padding = new Padding(14);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92)); // header
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));  // API
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));  // Upload
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));  // Flight
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); // buttons
        this.Controls.Add(root);

        // Header
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Window
        };
        root.Controls.Add(header, 0, 0);

        _picLogo = new PictureBox
        {
            Location = new Point(0, 10),
            Size = new Size(64, 64),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        header.Controls.Add(_picLogo);

        _lblHeaderTitle = new Label
        {
            AutoSize = true,
            Location = new Point(78, 12),
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 14f, FontStyle.Bold),
            Text = "Echo Airlines Screenshot Uploader"
        };
        header.Controls.Add(_lblHeaderTitle);

        _lblHeaderSubtitle = new Label
        {
            AutoSize = true,
            Location = new Point(80, 42),
            ForeColor = SystemColors.GrayText,
            Text = $"Configure API access and screenshot monitoring.  Version {GetVersionString()}"
        };
        header.Controls.Add(_lblHeaderSubtitle);

        // API group
        var grpApi = new GroupBox
        {
            Text = "API",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        root.Controls.Add(grpApi, 0, 1);

        var apiLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4
        };
        apiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        apiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        apiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        apiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        apiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        apiLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        apiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grpApi.Controls.Add(apiLayout);

        apiLayout.Controls.Add(new Label { Text = "API Base URL", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtApiBaseUrl = new TextBox { Dock = DockStyle.Fill };
        apiLayout.Controls.Add(_txtApiBaseUrl, 1, 0);
        apiLayout.SetColumnSpan(_txtApiBaseUrl, 2);

        apiLayout.Controls.Add(new Label { Text = "API Key", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _txtApiKey = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        apiLayout.Controls.Add(_txtApiKey, 1, 1);

        var btnProfile = new Button
        {
            Text = "Open Profile",
            Dock = DockStyle.Fill
        };
        btnProfile.Click += (_, _) => OpenUrl("https://www.echoairlines.com/profile");
        apiLayout.Controls.Add(btnProfile, 2, 1);

        var apiHelp = new Label
        {
            Text = "Get your API Key from your Echo Airlines user profile page.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Left
        };
        apiLayout.Controls.Add(apiHelp, 1, 2);
        apiLayout.SetColumnSpan(apiHelp, 2);

        // Upload group
        var grpUpload = new GroupBox
        {
            Text = "Uploads",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        root.Controls.Add(grpUpload, 0, 2);

        var uploadLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2
        };
        uploadLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        uploadLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        uploadLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        uploadLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        uploadLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grpUpload.Controls.Add(uploadLayout);

        uploadLayout.Controls.Add(new Label { Text = "Screenshot Folder", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtScreenshotFolder = new TextBox { Dock = DockStyle.Fill };
        uploadLayout.Controls.Add(_txtScreenshotFolder, 1, 0);

        _btnBrowseFolder = new Button { Text = "Browse…", Dock = DockStyle.Fill };
        _btnBrowseFolder.Click += BtnBrowseFolder_Click;
        uploadLayout.Controls.Add(_btnBrowseFolder, 2, 0);

        // Flight group
        var grpFlight = new GroupBox
        {
            Text = "Flight",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };
        root.Controls.Add(grpFlight, 0, 3);

        var flightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        flightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        flightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        flightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        flightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        flightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        flightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grpFlight.Controls.Add(flightLayout);

        _chkAutoDetectFlight = new CheckBox
        {
            Text = "Auto-detect Flight ID",
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        _chkAutoDetectFlight.CheckedChanged += ChkAutoDetectFlight_CheckedChanged;
        flightLayout.Controls.Add(_chkAutoDetectFlight, 1, 0);

        flightLayout.Controls.Add(new Label { Text = "Manual Flight ID", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _txtManualFlightId = new TextBox { Dock = DockStyle.Fill };
        flightLayout.Controls.Add(_txtManualFlightId, 1, 1);

        _chkEnableFsuipc = new CheckBox
        {
            Text = "Enable FSUIPC Metadata",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Enabled = false
        };
        flightLayout.Controls.Add(_chkEnableFsuipc, 1, 2);

        _lblFsuipcNote = new Label
        {
            Text = "Currently disabled due to compatibility issues.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Dock = DockStyle.Fill
        };
        flightLayout.Controls.Add(_lblFsuipcNote, 1, 3);

        // Buttons row
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        root.Controls.Add(buttons, 0, 4);

        _btnSave = new Button { Text = "Save", Width = 100, Height = 30, DialogResult = DialogResult.OK };
        _btnSave.Click += BtnSave_Click;
        _btnCancel = new Button { Text = "Cancel", Width = 100, Height = 30, DialogResult = DialogResult.Cancel };

        if (Environment.GetEnvironmentVariable("ENVIRONMENT") == "development")
        {
            _btnDebugNotification = new Button { Text = "Test Notification", Width = 120, Height = 30 };
            _btnDebugNotification.Click += BtnDebugNotification_Click;
            buttons.Controls.Add(_btnDebugNotification);
        }
        
        buttons.Controls.Add(_btnSave);
        buttons.Controls.Add(_btnCancel);

        this.AcceptButton = _btnSave;
        this.CancelButton = _btnCancel;

        // Apply visual tweaks / icon
        TryLoadHeaderLogo();
    }

    private void LoadSettings()
    {
        _txtApiBaseUrl!.Text = string.IsNullOrWhiteSpace(_settings.ApiBaseUrl) ? DefaultApiBaseUrl : _settings.ApiBaseUrl;
        _txtApiKey!.Text = _settings.ApiKey;
        _txtScreenshotFolder!.Text = _settings.ScreenshotFolderPath;
        _chkAutoDetectFlight!.Checked = _settings.AutoDetectFlightId;
        _txtManualFlightId!.Text = _settings.ManualFlightId;
        _txtManualFlightId!.Enabled = !_settings.AutoDetectFlightId;
        // FSUIPC metadata is currently disabled
        _chkEnableFsuipc!.Checked = false;
    }

    private void ChkAutoDetectFlight_CheckedChanged(object? sender, EventArgs e)
    {
        _txtManualFlightId!.Enabled = !_chkAutoDetectFlight!.Checked;
    }

    private void BtnBrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where screenshots are saved",
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(_txtScreenshotFolder!.Text) && Directory.Exists(_txtScreenshotFolder.Text))
        {
            dialog.SelectedPath = _txtScreenshotFolder.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _txtScreenshotFolder!.Text = dialog.SelectedPath;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtApiBaseUrl!.Text))
        {
            _txtApiBaseUrl.Text = DefaultApiBaseUrl;
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(_txtApiBaseUrl.Text))
        {
            MessageBox.Show("Please enter an API Base URL.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtApiBaseUrl.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtApiKey!.Text))
        {
            MessageBox.Show("Please enter an API Key.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtApiKey.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtScreenshotFolder!.Text))
        {
            MessageBox.Show("Please select a screenshot folder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtScreenshotFolder.Focus();
            return;
        }

        if (!Directory.Exists(_txtScreenshotFolder.Text))
        {
            MessageBox.Show("The selected folder does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtScreenshotFolder.Focus();
            return;
        }

        if (!_chkAutoDetectFlight!.Checked && string.IsNullOrWhiteSpace(_txtManualFlightId!.Text))
        {
            MessageBox.Show("Please enter a Flight ID or enable auto-detection.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtManualFlightId.Focus();
            return;
        }

        // Save settings
        _settings.ApiBaseUrl = _txtApiBaseUrl!.Text.Trim();
        _settings.ApiKey = _txtApiKey!.Text.Trim();
        _settings.ScreenshotFolderPath = _txtScreenshotFolder!.Text.Trim();
        _settings.AutoDetectFlightId = _chkAutoDetectFlight!.Checked;
        _settings.ManualFlightId = _txtManualFlightId!.Text.Trim();
        // Force-disabled for now
        _settings.EnableFsuipcMetadata = false;

        try
        {
            _configService.SaveSettings(_settings);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public AppSettings GetSettings()
    {
        return _settings;
    }

    private void TryLoadHeaderLogo()
    {
        try
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon != null)
            {
                this.Icon = icon;
                if (_picLogo != null)
                {
                    _picLogo.Image = icon.ToBitmap();
                }
            }
        }
        catch
        {
            // ignore - purely cosmetic
        }
    }

    private static string GetVersionString()
    {
        // Prefer InformationalVersion if present, else fall back to AssemblyName.Version
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            // Strip build metadata like "+abcdef"
            var plusIndex = info.IndexOf('+');
            return plusIndex >= 0 ? info[..plusIndex] : info;
        }

        var ver = asm.GetName().Version;
        return ver?.ToString() ?? "unknown";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore - user can copy/paste manually
        }
    }

    private void BtnDebugNotification_Click(object? sender, EventArgs e)
    {
        try
        {
            new ToastContentBuilder()
                .AddText("Debug Notification")
                .AddText("This is a test notification from the Screenshot Uploader settings.")
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddMinutes(1);
                });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to show notification: {ex.Message}",
                "Debug Notification Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }
}
