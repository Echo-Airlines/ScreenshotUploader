# Echo Airlines Screenshot Uploader

A Windows Forms application that monitors a configurable folder for screenshot files, captures Flight Simulator metadata via FSUIPC Client DLL, and automatically uploads them to the Echo Airlines API.

## Features

- **Automatic File Monitoring**: Watches a configured folder for new screenshot files
- **FSUIPC Integration** (coming soon): Captures flight simulator metadata (position, altitude, heading, speed, etc.)
- **Automatic Flight Detection**: Automatically detects active flights from the API
- **System Tray Integration**: Runs minimized in the system tray
- **Configurable Settings**: Easy-to-use settings dialog for configuration
- **Error Handling**: Comprehensive error handling with retry logic and user notifications

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime (download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0) if not already installed)
- Flight Simulator with FSUIPC installed (for metadata capture - optional)
- Valid Echo Airlines API key, obtainable from your [profile](https://www.echoairlines.com/profile) page

## Quick Start Guide for Pilots

### Step 1: Download

1. Navigate to the [Releases](https://github.com/EchoAirlines/ScreenshotUploader/releases) page on GitHub
2. Find the latest release (look for the most recent version number)
3. Click to download the `ScreenshotUploader-{version}-win-x64.zip` file
4. Save the ZIP file to a location you can easily find (e.g., your Downloads folder)

### Step 2: Extract

1. **Locate the downloaded ZIP file** in your Downloads folder (or wherever you saved it)
2. **Right-click the ZIP file** and select **"Extract All..."**
3. **Choose an extraction location**:
   - Recommended: `C:\Program Files\EchoAirlines\ScreenshotUploader` (you may need to create the folder first)
   - Alternative: `C:\Users\YourName\AppData\Local\EchoAirlines\ScreenshotUploader`
   - Or any folder of your choice
4. **Click "Extract"** and wait for the files to be extracted
5. **Note the location** where you extracted the files - you'll need to run the application from there

### Step 3: Check Prerequisites

Before running the application, ensure you have the required software:

1. **Check if .NET 8.0 Runtime is installed**:
   - Press `Windows Key + R` to open the Run dialog
   - Type `cmd` and press Enter
   - In the Command Prompt, type: `dotnet --version`
   - If you see version `8.0.x` or higher, you're good to go!
   - If you get an error or a version lower than 8.0, download and install .NET 8.0 Runtime from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0)
     - Choose "Run desktop apps" → Download .NET Desktop Runtime 8.0.x
     - Run the installer and follow the prompts

### Step 4: Run the Application

1. **Navigate to the folder** where you extracted the files
2. **Double-click** `Echo Airlines Screenshot Uploader.exe` to launch the application
3. **Look for the Echo Airlines icon** in your system tray (bottom-right corner, near the clock)
   - If you don't see it, click the `^` arrow to show hidden icons
4. The application is now running in the background and ready to be configured

### Step 5: Configure the Application

**First-time setup is required before the app can upload screenshots:**

1. **Right-click the Echo Airlines icon** in your system tray
2. **Click "Settings"** from the context menu
3. **Fill in the configuration form**:

   **API Base URL** (usually pre-filled):
   - Default: `https://www.echoairlines.com/api`
   - If this doesn't work, try: `https://echoairlines.com/api`
   - Leave as default unless instructed otherwise

   **API Key** (required):
   - Log in to the [Echo Airlines website](https://www.echoairlines.com)
   - Navigate to your **Profile** or **Settings** page
   - Create or Find your **API Key** or **JWT Token** (it's a long string of characters)
   - Copy the entire API key
   - Paste it into the "API Key" field in the settings window

   **Screenshot Folder** (required):
   - Click the **"Browse"** button
   - Navigate to the folder where your Flight Simulator saves screenshots
   - **Common locations**:
     - **Microsoft Flight Simulator (MSFS)**: 
       - `C:\Users\YourName\Pictures\Microsoft Flight Simulator`
       - Or check Settings → General → Screenshots in MSFS
     - **FSX / Prepar3D**: 
       - `C:\Users\YourName\Documents\My Pictures\Flight Simulator X`
       - Or check your Flight Simulator settings
   - Select the folder and click "OK"

   **Flight ID Detection**:
   - **Automatic** (recommended): The app will automatically find your active flight
   - **Manual**: Only use if you need to upload to a specific flight ID

   **FSUIPC Metadata** (optional):
   - Leave enabled if you have FSUIPC installed
   - Disable if you don't have FSUIPC or don't want metadata captured

4. **Test your connection**:
   - Click the **"Test Connection"** button
   - Wait for a success message confirming your API key works
   - If you get an error, double-check your API key and API Base URL

5. **Save your settings**:
   - Click the **"Save"** button
   - Your settings are now saved and the app is ready to use!

**Note**: Your settings are saved to `%AppData%\EchoAirlines\ScreenshotUploader\settings.json` and will persist between application restarts.

## Using the Application

Once you've completed the configuration, the application works automatically:

1. **Start the application** (if not already running):
   - Double-click `Echo Airlines Screenshot Uploader.exe` from your installation folder
   - The app will minimize to your system tray

2. **Start your flight**:
   - Launch your Flight Simulator
   - Begin a flight in the Echo Airlines system (create a flight on the website if needed)
   - The app will automatically detect your active flight

3. **Take screenshots**:
   - Take screenshots in Flight Simulator as you normally would (usually `V` key or `Print Screen`)
   - The application **automatically monitors** your screenshot folder
   - When a new screenshot is detected, it will be **automatically uploaded** to your active flight
   - You'll receive a **Windows notification** when the upload is successful

4. **View your screenshots**:
   - Log in to the Echo Airlines website
   - Navigate to your flight details page
   - Your uploaded screenshots will appear in the screenshot gallery

**System Tray Controls**:
- **Right-click** the Echo Airlines icon in your system tray to access:
  - **Settings**: Modify your configuration
  - **View Logs**: Check recent upload activity and any errors
  - **Exit**: Close the application

**Tips**:
- The application runs in the background - you don't need to keep any windows open
- Make sure the app is running before you start taking screenshots
- If you're not seeing uploads, check the logs via the system tray menu
- The app will automatically find your active flight, so you don't need to manually enter flight IDs

## FSUIPC Offsets Used

The application reads the following FSUIPC offsets:

- **0x0560**: Latitude (64-bit double, degrees)
- **0x0568**: Longitude (64-bit double, degrees)
- **0x0570**: Altitude (64-bit integer, FS units = feet * 256)
- **0x0580**: Heading (32-bit integer, degrees = value * 360 / 65536)
- **0x02B4**: Ground Speed (32-bit integer, knots = value * 128 / 65536)
- **0x0366**: On Ground flag (byte, bit 0 = on ground)

## Troubleshooting

### Common Issues

**Screenshots aren't uploading:**
- Check that the application is running (look for the icon in your system tray)
- Verify your API key is correct - try clicking "Test Connection" in Settings
- Ensure you have an active flight in the Echo Airlines system
- Check the application logs via right-click → View Logs in the system tray menu
- Verify your screenshot folder path is correct and screenshots are actually being saved there

**"No Active Flight Found" error:**
- Make sure you've created a flight on the Echo Airlines website before taking screenshots
- Ensure your flight status is "In Progress" or "Active"
- Try using Manual Flight ID mode and enter your flight ID directly

**"Upload Failed" or connection errors:**
- Verify your internet connection is working
- Check that your API key hasn't expired (get a new one from your profile if needed)
- Ensure the API Base URL is correct (default: `https://www.echoairlines.com/api`)
- Check Windows Firewall isn't blocking the application

**FSUIPC Connection Failed:**
- This is only needed if you want metadata capture (optional feature)
- Ensure FSUIPC is installed and Flight Simulator is running
- You can disable FSUIPC metadata in Settings if you don't need it

**Application won't start:**
- Verify .NET 8.0 Runtime is installed (see Step 3 in Quick Start Guide)
- Try running as Administrator (right-click → Run as Administrator)
- Check Windows Event Viewer for error details

**Can't find the system tray icon:**
- Click the `^` arrow in the bottom-right corner to show hidden icons
- The icon may be hidden - check Windows notification area settings
- Restart the application if the icon doesn't appear

**Need help?**
- Check the application logs: Right-click system tray icon → View Logs
- Contact Echo Airlines support with your error messages

## License

This project is part of the Echo Airlines system.
