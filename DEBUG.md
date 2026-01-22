# Debugging ScreenshotUploader in Cursor

Since Cursor doesn't support the Microsoft C# debugger extension, here are alternative debugging methods:

## Method 1: Terminal (Recommended)

### Run the application:
```powershell
dotnet run
```

### Build first, then run:
```powershell
dotnet build
dotnet run --no-build
```

### Run with debug output:
```powershell
$env:SENTRY_DEBUG="true"
dotnet run
```

## Method 2: Add Debug Output

Add `System.Diagnostics.Debug.WriteLine()` statements in your code. These will appear in the **Output** panel in Cursor when you run the app.

Example:
```csharp
System.Diagnostics.Debug.WriteLine($"Processing screenshot: {filePath}");
```

To view debug output:
1. Go to View → Output (or press `Ctrl+Shift+U`)
2. Select "Debug Console" or "Tasks" from the dropdown

## Method 3: Console Output

Since this is a Windows Forms app that runs in the system tray, you can add console output for debugging:

```csharp
// In Program.cs, you can add:
Console.WriteLine("Application starting...");
```

Then run with:
```powershell
dotnet run
```

The console output will appear in the terminal.

## Method 4: Use Visual Studio

If you need full debugging with breakpoints:
1. Open the `.sln` file in Visual Studio
2. Set breakpoints
3. Press F5 to debug

## Quick Debug Commands

Add these to your terminal aliases or use them directly:

```powershell
# Build
dotnet build

# Run
dotnet run

# Clean and rebuild
dotnet clean
dotnet build

# Watch for changes (auto-rebuilds)
dotnet watch run
```

## Viewing Logs

The application uses `System.Diagnostics.Debug.WriteLine()` for logging. To see these:
- Check the Output panel in Cursor
- Or use a tool like DebugView (Sysinternals) to see all debug output system-wide
