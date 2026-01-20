using System;
using System.Windows.Forms;

namespace ScreenshotUploader;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
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
}
