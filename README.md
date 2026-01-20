# Echo Airlines Screenshot Uploader

A Windows Forms application that monitors a configurable folder for screenshot files, captures Flight Simulator metadata via FSUIPC Client DLL, and automatically uploads them to the Echo Airlines API.

## Features

- **Automatic File Monitoring**: Watches a configured folder for new screenshot files
- **FSUIPC Integration**: Captures flight simulator metadata (position, altitude, heading, speed, etc.)
- **Automatic Flight Detection**: Automatically detects active flights from the API
- **System Tray Integration**: Runs minimized in the system tray
- **Configurable Settings**: Easy-to-use settings dialog for configuration
- **Error Handling**: Comprehensive error handling with retry logic and user notifications

## Requirements

- Windows 10 or later
- .NET 6.0 or later
- Flight Simulator with FSUIPC installed (for metadata capture)
- Valid Echo Airlines API key

## Installation

1. Build the project using Visual Studio or `dotnet build`
2. Run the application
3. Configure settings on first launch

## Configuration

The application requires the following settings:

- **API Base URL**: The base URL of the Echo Airlines API (e.g., `https://echoairlines.com/api`)
- **API Key**: Your JWT authentication token from the Echo Airlines website
- **Screenshot Folder**: The folder where your flight simulator saves screenshots
- **Flight ID Detection**: Choose between automatic detection or manual entry
- **FSUIPC Metadata**: Enable/disable capturing flight simulator metadata

Settings are stored in `%AppData%\EchoAirlines\ScreenshotUploader\settings.json`

## Usage

1. Launch the application - it will run in the system tray
2. Right-click the tray icon to access settings
3. Configure your API URL, API key, and screenshot folder
4. The application will automatically monitor the folder and upload screenshots

## FSUIPC Offsets Used

The application reads the following FSUIPC offsets:

- **0x0560**: Latitude (64-bit double, degrees)
- **0x0568**: Longitude (64-bit double, degrees)
- **0x0570**: Altitude (64-bit integer, FS units = feet * 256)
- **0x0580**: Heading (32-bit integer, degrees = value * 360 / 65536)
- **0x02B4**: Ground Speed (32-bit integer, knots = value * 128 / 65536)
- **0x0366**: On Ground flag (byte, bit 0 = on ground)

## Troubleshooting

- **FSUIPC Connection Failed**: Ensure FSUIPC is installed and Flight Simulator is running
- **Upload Failed**: Check your API key and network connection
- **No Active Flight Found**: Ensure you have an active flight in the system or use manual flight ID
- **File Not Found**: Verify the screenshot folder path is correct

## License

This project is part of the Echo Airlines system.
