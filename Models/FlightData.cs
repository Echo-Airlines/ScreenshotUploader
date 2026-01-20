using FSUIPC;

namespace ScreenshotUploader.Models;

public class FlightData
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Heading { get; set; }
    public double? GroundSpeed { get; set; }
    public bool? OnGround { get; set; }
}
