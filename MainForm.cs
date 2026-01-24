using FSUIPC;
using Microsoft.Toolkit.Uwp.Notifications;
using ScreenshotUploader.Models;
using ScreenshotUploader.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenshotUploader;

public partial class MainForm : Form
{
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private ConfigurationService _configService;
    private FileWatcherService? _fileWatcher;
    private FSUIPCService? _fsuipcService;
    private ApiClient? _apiClient;
    private AppSettings _settings;
    private readonly Queue<string> _screenshotQueue = new Queue<string>();
    private readonly object _queueLock = new object();
    private bool _isProcessing = false;
    private System.Windows.Forms.Timer timerMain;
    private System.Windows.Forms.Timer timerConnection;
    private System.ComponentModel.IContainer? components = null;

    public MainForm()
    {
        _configService = new ConfigurationService();
        _settings = _configService.LoadSettings();
        
        InitializeComponent();
        InitializeServices();
    }

    // Connection timer ticks every 1 second - set in the timer properties.
    private void timerConnection_Tick(object sender, EventArgs e)
    {
        // try to open the connection
        try
        {
            FSUIPCConnection.Open();
        }
        catch
        {
            // connection failed. No need to do anything, we just keep trying
        }
        if (FSUIPCConnection.IsOpen)
        {
            // connection opened
            // stop the timer that looks for a connection
            this.timerConnection.Stop();
            setConnectionStatus();
            // start the main timer
            this.timerMain.Start();
        }
    }

    private void setConnectionStatus()
    {
        // set the text of the status label depending on the connection status
        //if (FSUIPCConnection.IsOpen)
        //{
        //    this.lblStatus.Text = "Connected to " + FSUIPCConnection.FlightSimVersionConnected.ToString();
        //    this.lblStatus.ForeColor = Color.DarkGreen;
        //}
        //else
        //{
        //    this.lblStatus.Text = "Disconnected. Looking for Flight Sim...";
        //    this.lblStatus.ForeColor = Color.Red;
        //}
    }

    // Main Timer ticks every 50ms - Set in the timer properties
    private void timerMain_Tick(object sender, EventArgs e)
    {
        // This method will be called automatically by the main timer at your chosen interval set in the timer properties.
        // This is where you would put code to read/write offsets in real time.
        // See example BC003_ReadingOffsets
    }

    private void InitializeComponent()
    {
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MinimizeBox = false;
        this.MaximizeBox = false;
        this.components = new System.ComponentModel.Container();
        this.timerMain = new System.Windows.Forms.Timer(this.components);
        this.timerConnection = new System.Windows.Forms.Timer(this.components);

        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Settings", null, OnSettingsClick);
        _contextMenu.Items.Add("About", null, OnAboutClick);
        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("Exit", null, OnExitClick);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Text = "Echo Airlines Screenshot Uploader",
            Visible = true
        };

        var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        this.Icon = appIcon;

        _trayIcon.Icon = appIcon;

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
            _fsuipcService = new FSUIPCService();
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
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        bool shouldStartProcessing = false;
        lock (_queueLock)
        {
            _screenshotQueue.Enqueue(filePath);
            if (!_isProcessing)
            {
                _isProcessing = true;
                shouldStartProcessing = true;
            }
        }

        // Start processing if not already processing
        if (shouldStartProcessing)
        {
            Task.Run(async () => await ProcessScreenshotQueueAsync());
        }
    }

    private async Task ProcessScreenshotQueueAsync()
    {
        try
        {
            // Process all queued screenshots, grouping by flight ID
            var uploadResults = new List<(string flightId, int count)>();

            while (true)
            {
                string? filePath = null;
                lock (_queueLock)
                {
                    if (_screenshotQueue.Count == 0)
                    {
                        break;
                    }
                    filePath = _screenshotQueue.Dequeue();
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                var result = await ProcessScreenshotAsync(filePath);
                if (result != null)
                {
                    uploadResults.Add(result.Value);
                }
            }

            // Group uploads by flight ID and show notifications
            var groupedResults = uploadResults
                .GroupBy(r => r.flightId)
                .ToList();

            foreach (var group in groupedResults)
            {
                var flightId = group.Key;
                var count = group.Sum(r => r.count);
                
                this.Invoke((MethodInvoker)delegate
                {
                    ShowToastNotification(flightId, count);
                });
            }
        }
        catch (Exception ex)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    var errorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                    ShowNotification("Upload Error", $"Failed to process screenshots: {errorMessage}", ToolTipIcon.Error);
                });
            }
            catch
            {
                Debug.WriteLine($"Error in ProcessScreenshotQueueAsync: {ex.Message}");
            }
        }
        finally
        {
            lock (_queueLock)
            {
                // Reset processing flag only if queue is empty
                // If queue has more items, they will be picked up by the next OnScreenshotDetected call
                if (_screenshotQueue.Count == 0)
                {
                    _isProcessing = false;
                }
                else
                {
                    // Queue has more items that arrived while processing, continue processing
                    Task.Run(async () => await ProcessScreenshotQueueAsync());
                }
            }
        }
    }

    private async Task<(string flightId, int count)?> ProcessScreenshotAsync(string filePath)
    {
        if (_apiClient == null)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("Configuration Error", "API client is not initialized. Please check your settings.", ToolTipIcon.Error);
            });
            return null;
        }

        // Validate file exists and is accessible
        if (!File.Exists(filePath))
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("File Error", $"Screenshot file not found: {Path.GetFileName(filePath)}", ToolTipIcon.Warning);
            });
            return null;
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
                    return null;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowNotification("Authentication Error", ex.Message, ToolTipIcon.Error);
                });
                return null;
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowNotification("Flight Detection Error", $"Failed to get active flight: {ex.Message}", ToolTipIcon.Error);
                });
                return null;
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
                return null;
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
                Debug.WriteLine($"FSUIPC read error: {ex.Message}");
            }
        }

        // Upload screenshot
        try
        {
            await _apiClient.UploadScreenshotAsync(filePath, flightId, flightData);
            return (flightId, 1);
        }
        catch (UnauthorizedAccessException ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                ShowNotification("Authentication Error", ex.Message, ToolTipIcon.Error);
            });
            return null;
        }
        catch (Exception ex)
        {
            this.Invoke((MethodInvoker)delegate
            {
                var errorMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                ShowNotification("Upload Error", $"Failed to upload screenshot: {errorMessage}", ToolTipIcon.Error);
            });
            return null;
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
        var versionString = GetVersionString();
        MessageBox.Show(
            "Echo Airlines Screenshot Uploader\n\n" +
            "Monitors a folder for screenshots and automatically uploads them to the Echo Airlines API.\n\n" +
            $"Version {versionString}",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string GetVersionString()
    {
        // Prefer InformationalVersion if present, else fall back to AssemblyName.Version
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        var info = System.Reflection.CustomAttributeExtensions
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(asm)
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plusIndex = info.IndexOf('+');
            return plusIndex >= 0 ? info[..plusIndex] : info;
        }

        var ver = asm.GetName().Version;
        return ver?.ToString() ?? "unknown";
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

    private string GetBaseUrl()
    {
        // Get base URL from API URL (remove /api suffix if present)
        var apiUrl = _settings.ApiBaseUrl ?? "https://www.echoairlines.com/api";
        if (apiUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return apiUrl.Substring(0, apiUrl.Length - 4);
        }
        // If no /api suffix, try to extract base URL
        var uri = new Uri(apiUrl);
        return $"{uri.Scheme}://{uri.Host}";
    }

    private void ShowToastNotification(string flightId, int screenshotCount)
    {
        try
        {
            var baseUrl = GetBaseUrl();
            var flightUrl = $"{baseUrl}/flight/{flightId}";
            
            var title = screenshotCount == 1 
                ? "Screenshot Uploaded" 
                : $"{screenshotCount} Screenshots Uploaded";
            
            var message = screenshotCount == 1
                ? $"Your screenshot has been uploaded successfully.\nView flight"
                : $"{screenshotCount} screenshots have been uploaded successfully.\nView flight";

            Debug.WriteLine($"Attempting to show toast notification for flight {flightId}");

            // Check if we can create a notifier first
            try
            {
                var notifier = ToastNotificationManagerCompat.CreateToastNotifier();
                if (notifier == null)
                {
                    throw new InvalidOperationException("Toast notifier is null");
                }
                Debug.WriteLine($"Toast notifier created successfully");
            }
            catch (Exception notifierEx)
            {
                Debug.WriteLine($"Failed to create toast notifier: {notifierEx.Message}");
                throw new InvalidOperationException($"Cannot create toast notifier. Make sure the app is properly installed and Windows notifications are enabled. Error: {notifierEx.Message}", notifierEx);
            }

            // Use the Show() extension method directly
            // Make the entire notification clickable by setting launch argument
            new ToastContentBuilder()
                .AddArgument("action", "viewFlight")
                .AddArgument("flightId", flightId)
                .SetToastScenario(ToastScenario.Default)
                .AddText(title)
                .AddText(message)
                .AddButton(new ToastButton()
                    .SetContent("Click to view flight")
                    .AddArgument("action", "viewFlight")
                    .AddArgument("flightId", flightId))
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddMinutes(5);
                });
            
            Debug.WriteLine($"Toast notification shown successfully for flight {flightId}");
        }
        catch (Exception ex)
        {
            // Fallback to balloon tip if toast fails
            Debug.WriteLine($"Failed to show toast notification: {ex.Message}");
            Debug.WriteLine($"Exception type: {ex.GetType().FullName}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Always show balloon tip as fallback so user gets some notification
            var baseUrl = GetBaseUrl();
            var flightUrl = $"{baseUrl}/flight/{flightId}";
            ShowNotification(
                screenshotCount == 1 ? "Screenshot Uploaded" : "Screenshots Uploaded",
                screenshotCount == 1 
                    ? $"Screenshot uploaded successfully.\nView flight"
                    : $"{screenshotCount} screenshots uploaded successfully.\nView flight",
                ToolTipIcon.Info);
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
