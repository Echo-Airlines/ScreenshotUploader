using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScreenshotUploader.Models;
using ScreenshotUploader.Services;

namespace ScreenshotUploader;

public partial class MainForm : Form
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private ConfigurationService _configService;
    private FileWatcherService? _fileWatcher;
    private FsuipcService? _fsuipcService;
    private ApiClient? _apiClient;
    private AppSettings _settings;
    private bool _isProcessing = false;

    public MainForm()
    {
        _configService = new ConfigurationService();
        _settings = _configService.LoadSettings();
        
        InitializeComponent();
        InitializeServices();
    }

    private void InitializeComponent()
    {
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MinimizeBox = false;
        this.MaximizeBox = false;

        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Settings", null, OnSettingsClick);
        _contextMenu.Items.Add("About", null, OnAboutClick);
        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("Exit", null, OnExitClick);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = _contextMenu,
            Text = "Echo Airlines Screenshot Uploader",
            Visible = true
        };

        _trayIcon.DoubleClick += OnTrayIconDoubleClick;
        _trayIcon.MouseClick += OnTrayIconClick;
    }

    private void InitializeServices()
    {
        // Validate settings
        if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl) ||
            string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.IsNullOrWhiteSpace(_settings.ScreenshotFolderPath))
        {
            ShowNotification("Configuration Required", "Please configure the application settings.", ToolTipIcon.Warning);
            ShowSettings();
            return;
        }

        // Initialize API client
        try
        {
            _apiClient = new ApiClient(_settings.ApiBaseUrl, _settings.ApiKey);
        }
        catch (Exception ex)
        {
            ShowNotification("Configuration Error", $"Failed to initialize API client: {ex.Message}", ToolTipIcon.Error);
            return;
        }

        // Initialize FSUIPC service if enabled
        if (_settings.EnableFsuipcMetadata)
        {
            _fsuipcService = new FsuipcService();
            if (!_fsuipcService.Connect())
            {
                ShowNotification("FSUIPC Warning", "Could not connect to FSUIPC. Screenshots will be uploaded without metadata.", ToolTipIcon.Warning);
            }
        }

        // Initialize file watcher
        try
        {
            _fileWatcher = new FileWatcherService();
            _fileWatcher.FileCreated += OnScreenshotDetected;
            
            if (Directory.Exists(_settings.ScreenshotFolderPath))
            {
                _fileWatcher.StartWatching(_settings.ScreenshotFolderPath);
                ShowNotification("Monitoring Started", $"Watching folder: {_settings.ScreenshotFolderPath}", ToolTipIcon.Info);
            }
            else
            {
                ShowNotification("Configuration Error", "Screenshot folder does not exist. Please check settings.", ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            ShowNotification("File Watcher Error", $"Failed to initialize file watcher: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private void OnScreenshotDetected(object? sender, string filePath)
    {
        if (_isProcessing)
        {
            // Queue for later processing or skip
            return;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        Task.Run(async () =>
        {
            _isProcessing = true;
            try
            {
                await ProcessScreenshotAsync(filePath);
            }
            catch (Exception ex)
            {
                try
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        var errorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                        ShowNotification("Upload Error", $"Failed to upload screenshot: {errorMessage}", ToolTipIcon.Error);
                    });
                }
                catch
                {
                    // If invoke fails, log to debug
                    System.Diagnostics.Debug.WriteLine($"Error in OnScreenshotDetected: {ex.Message}");
                }
            }
            finally
            {
                _isProcessing = false;
            }
        });
    }

    private async Task ProcessScreenshotAsync(string filePath)
    {
        if (_apiClient == null)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("Configuration Error", "API client is not initialized. Please check your settings.", ToolTipIcon.Error);
            });
            return;
        }

        // Validate file exists and is accessible
        if (!File.Exists(filePath))
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("File Error", $"Screenshot file not found: {Path.GetFileName(filePath)}", ToolTipIcon.Warning);
            });
            return;
        }

        // Get flight ID
        string? flightId = null;
        
        if (_settings.AutoDetectFlightId)
        {
            try
            {
                flightId = await _apiClient.GetActiveFlightIdAsync();
                if (string.IsNullOrWhiteSpace(flightId))
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        ShowNotification("Flight Detection", "No active flight found. Please check your settings or start a flight.", ToolTipIcon.Warning);
                    });
                    return;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowNotification("Authentication Error", ex.Message, ToolTipIcon.Error);
                });
                return;
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowNotification("Flight Detection Error", $"Failed to get active flight: {ex.Message}", ToolTipIcon.Error);
                });
                return;
            }
        }
        else
        {
            flightId = _settings.ManualFlightId;
            if (string.IsNullOrWhiteSpace(flightId))
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowNotification("Configuration Error", "Flight ID is not configured.", ToolTipIcon.Error);
                });
                return;
            }
        }

        // Read FSUIPC data if enabled
        FlightData? flightData = null;
        if (_settings.EnableFsuipcMetadata && _fsuipcService != null)
        {
            try
            {
                // Try to reconnect if not connected
                if (!_fsuipcService.IsConnected)
                {
                    _fsuipcService.Connect();
                }

                if (_fsuipcService.IsConnected)
                {
                    flightData = _fsuipcService.ReadFlightData();
                }
            }
            catch (Exception ex)
            {
                // Continue without metadata - log but don't fail upload
                System.Diagnostics.Debug.WriteLine($"FSUIPC read error: {ex.Message}");
            }
        }

        // Upload screenshot
        try
        {
            await _apiClient.UploadScreenshotAsync(filePath, flightId, flightData);
            
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("Upload Success", $"Screenshot uploaded successfully: {Path.GetFileName(filePath)}", ToolTipIcon.Info);
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("Authentication Error", ex.Message, ToolTipIcon.Error);
            });
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                var errorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                ShowNotification("Upload Error", $"Failed to upload screenshot: {errorMessage}", ToolTipIcon.Error);
            });
        }
    }

    private void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Single click - could show a menu or do nothing
        }
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
        ShowSettings();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings()
    {
        using var settingsForm = new SettingsForm(_configService);
        if (settingsForm.ShowDialog() == DialogResult.OK)
        {
            // Reload settings and reinitialize services
            _settings = settingsForm.GetSettings();
            
            // Dispose old services
            _fileWatcher?.Dispose();
            _fsuipcService?.Dispose();
            _apiClient?.Dispose();

            // Reinitialize
            InitializeServices();
        }
    }

    private void OnAboutClick(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "Echo Airlines Screenshot Uploader\n\n" +
            "Monitors a folder for screenshots and automatically uploads them to the Echo Airlines API with Flight Simulator metadata.\n\n" +
            "Version 1.0",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to exit?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Application.Exit();
        }
    }

    private void ShowNotification(string title, string message, ToolTipIcon icon)
    {
        if (_trayIcon != null)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(3000);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        this.Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileWatcher?.Dispose();
            _fsuipcService?.Dispose();
            _apiClient?.Dispose();
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
