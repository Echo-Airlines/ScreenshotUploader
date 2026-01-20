using System;
using System.IO;
using Newtonsoft.Json;
using ScreenshotUploader.Models;

namespace ScreenshotUploader.Services;

public class ConfigurationService
{
    private readonly string _settingsPath;
    private AppSettings? _settings;

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(appDataPath, "EchoAirlines", "ScreenshotUploader");
        
        if (!Directory.Exists(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        _settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings LoadSettings()
    {
        if (_settings != null)
        {
            return _settings;
        }

        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                _settings = new AppSettings();
            }
        }
        else
        {
            _settings = new AppSettings();
        }

        return _settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            _settings = settings;
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save settings: {ex.Message}", ex);
        }
    }
}
