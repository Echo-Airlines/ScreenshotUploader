using System;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;
using Sentry;

namespace ScreenshotUploader;

static class Program
{
    private const string AppUserModelId = "EchoAirlines.ScreenshotUploader";
    private static string baseUrl = "https://www.echoairlines.com";
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Set AppUserModelID for toast notifications
        // This must be set before showing any toast notifications
        // ToastNotificationManagerCompat.SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        
        var envBaseUrl = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrWhiteSpace(envBaseUrl))
        {
            baseUrl = envBaseUrl;
        }

        // Init Sentry from environment variable so we don't commit DSNs to git.
        var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            SentrySdk.Init(o =>
            {
                o.Dsn = sentryDsn;
                // Optional: enable debug logs by setting SENTRY_DEBUG=true
                o.Debug = string.Equals(
                    Environment.GetEnvironmentVariable("SENTRY_DEBUG"),
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );
            });

        }
        
        // Configure WinForms to throw exceptions so Sentry can capture them.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    private static void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        // Handle toast activation (clicking on notification)
        var args = ToastArguments.Parse(e.Argument);
        
        if (args.Contains("action") && args["action"] == "viewFlight")
        {
            var flightId = args["flightId"];
            if (!string.IsNullOrWhiteSpace(flightId))
            {
                var flightUrl = $"{baseUrl}/flight/{flightId}";
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = flightUrl,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // If browser launch fails, silently ignore
                }
            }
        }
    }
}
