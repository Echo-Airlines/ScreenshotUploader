namespace ScreenshotUploader.Models;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://www.echoairlines.com/api";
    public string ApiKey { get; set; } = string.Empty;
    public string ScreenshotFolderPath { get; set; } = string.Empty;
    public bool AutoDetectFlightId { get; set; } = true;
    public string ManualFlightId { get; set; } = string.Empty;
    // Temporarily disabled due to compatibility issues.
    public bool EnableFsuipcMetadata { get; set; } = false;
}
