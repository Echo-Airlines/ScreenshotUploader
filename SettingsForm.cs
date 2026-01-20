using System;
using System.IO;
using System.Windows.Forms;
using ScreenshotUploader.Models;
using ScreenshotUploader.Services;

namespace ScreenshotUploader;

public partial class SettingsForm : Form
{
    private readonly ConfigurationService _configService;
    private AppSettings _settings;

    private TextBox _txtApiBaseUrl;
    private TextBox _txtApiKey;
    private TextBox _txtScreenshotFolder;
    private CheckBox _chkAutoDetectFlight;
    private TextBox _txtManualFlightId;
    private CheckBox _chkEnableFsuipc;
    private Button _btnBrowseFolder;
    private Button _btnSave;
    private Button _btnCancel;

    public SettingsForm(ConfigurationService configService)
    {
        _configService = configService;
        _settings = _configService.LoadSettings();
        
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Screenshot Uploader - Settings";
        this.Size = new System.Drawing.Size(600, 400);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        int yPos = 20;
        int labelWidth = 150;
        int controlWidth = 400;
        int spacing = 30;

        // API Base URL
        var lblApiUrl = new Label
        {
            Text = "API Base URL:",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(labelWidth, 20)
        };
        this.Controls.Add(lblApiUrl);

        _txtApiBaseUrl = new TextBox
        {
            Location = new System.Drawing.Point(180, yPos),
            Size = new System.Drawing.Size(controlWidth, 20),
            Text = _settings.ApiBaseUrl
        };
        this.Controls.Add(_txtApiBaseUrl);
        yPos += spacing;

        // API Key
        var lblApiKey = new Label
        {
            Text = "API Key:",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(labelWidth, 20)
        };
        this.Controls.Add(lblApiKey);

        _txtApiKey = new TextBox
        {
            Location = new System.Drawing.Point(180, yPos),
            Size = new System.Drawing.Size(controlWidth, 20),
            UseSystemPasswordChar = true,
            Text = _settings.ApiKey
        };
        this.Controls.Add(_txtApiKey);
        yPos += spacing;

        // Screenshot Folder
        var lblFolder = new Label
        {
            Text = "Screenshot Folder:",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(labelWidth, 20)
        };
        this.Controls.Add(lblFolder);

        _txtScreenshotFolder = new TextBox
        {
            Location = new System.Drawing.Point(180, yPos),
            Size = new System.Drawing.Size(controlWidth - 80, 20),
            Text = _settings.ScreenshotFolderPath
        };
        this.Controls.Add(_txtScreenshotFolder);

        _btnBrowseFolder = new Button
        {
            Text = "Browse...",
            Location = new System.Drawing.Point(500, yPos - 2),
            Size = new System.Drawing.Size(75, 25)
        };
        _btnBrowseFolder.Click += BtnBrowseFolder_Click;
        this.Controls.Add(_btnBrowseFolder);
        yPos += spacing;

        // Auto-detect Flight ID
        _chkAutoDetectFlight = new CheckBox
        {
            Text = "Auto-detect Flight ID",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(200, 20),
            Checked = _settings.AutoDetectFlightId
        };
        _chkAutoDetectFlight.CheckedChanged += ChkAutoDetectFlight_CheckedChanged;
        this.Controls.Add(_chkAutoDetectFlight);
        yPos += spacing;

        // Manual Flight ID
        var lblManualFlightId = new Label
        {
            Text = "Manual Flight ID:",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(labelWidth, 20)
        };
        this.Controls.Add(lblManualFlightId);

        _txtManualFlightId = new TextBox
        {
            Location = new System.Drawing.Point(180, yPos),
            Size = new System.Drawing.Size(controlWidth, 20),
            Text = _settings.ManualFlightId,
            Enabled = !_settings.AutoDetectFlightId
        };
        this.Controls.Add(_txtManualFlightId);
        yPos += spacing;

        // Enable FSUIPC Metadata
        _chkEnableFsuipc = new CheckBox
        {
            Text = "Enable FSUIPC Metadata",
            Location = new System.Drawing.Point(20, yPos),
            Size = new System.Drawing.Size(200, 20),
            Checked = _settings.EnableFsuipcMetadata
        };
        this.Controls.Add(_chkEnableFsuipc);
        yPos += spacing + 20;

        // Buttons
        _btnSave = new Button
        {
            Text = "Save",
            Location = new System.Drawing.Point(400, yPos),
            Size = new System.Drawing.Size(75, 30),
            DialogResult = DialogResult.OK
        };
        _btnSave.Click += BtnSave_Click;
        this.Controls.Add(_btnSave);

        _btnCancel = new Button
        {
            Text = "Cancel",
            Location = new System.Drawing.Point(485, yPos),
            Size = new System.Drawing.Size(75, 30),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(_btnCancel);

        this.AcceptButton = _btnSave;
        this.CancelButton = _btnCancel;
    }

    private void LoadSettings()
    {
        _txtApiBaseUrl.Text = _settings.ApiBaseUrl;
        _txtApiKey.Text = _settings.ApiKey;
        _txtScreenshotFolder.Text = _settings.ScreenshotFolderPath;
        _chkAutoDetectFlight.Checked = _settings.AutoDetectFlightId;
        _txtManualFlightId.Text = _settings.ManualFlightId;
        _txtManualFlightId.Enabled = !_settings.AutoDetectFlightId;
        _chkEnableFsuipc.Checked = _settings.EnableFsuipcMetadata;
    }

    private void ChkAutoDetectFlight_CheckedChanged(object? sender, EventArgs e)
    {
        _txtManualFlightId.Enabled = !_chkAutoDetectFlight.Checked;
    }

    private void BtnBrowseFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where screenshots are saved",
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrWhiteSpace(_txtScreenshotFolder.Text) && Directory.Exists(_txtScreenshotFolder.Text))
        {
            dialog.SelectedPath = _txtScreenshotFolder.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _txtScreenshotFolder.Text = dialog.SelectedPath;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(_txtApiBaseUrl.Text))
        {
            MessageBox.Show("Please enter an API Base URL.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtApiBaseUrl.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtApiKey.Text))
        {
            MessageBox.Show("Please enter an API Key.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtApiKey.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtScreenshotFolder.Text))
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

        if (!_chkAutoDetectFlight.Checked && string.IsNullOrWhiteSpace(_txtManualFlightId.Text))
        {
            MessageBox.Show("Please enter a Flight ID or enable auto-detection.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtManualFlightId.Focus();
            return;
        }

        // Save settings
        _settings.ApiBaseUrl = _txtApiBaseUrl.Text.Trim();
        _settings.ApiKey = _txtApiKey.Text.Trim();
        _settings.ScreenshotFolderPath = _txtScreenshotFolder.Text.Trim();
        _settings.AutoDetectFlightId = _chkAutoDetectFlight.Checked;
        _settings.ManualFlightId = _txtManualFlightId.Text.Trim();
        _settings.EnableFsuipcMetadata = _chkEnableFsuipc.Checked;

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
}
