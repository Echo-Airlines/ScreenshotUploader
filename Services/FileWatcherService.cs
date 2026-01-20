using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScreenshotUploader.Services;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly HashSet<string> _processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly string[] _imageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    public event EventHandler<string>? FileCreated;

    public bool IsWatching => _watcher?.EnableRaisingEvents ?? false;

    public void StartWatching(string folderPath)
    {
        StopWatching();

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        _watcher = new FileSystemWatcher(folderPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            Filter = "*.*",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Error += OnError;
    }

    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.Created -= OnFileCreated;
            _watcher.Error -= OnError;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    public void MarkFileAsProcessed(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _processedFiles.Add(filePath);
        }
    }

    public void ClearProcessedFiles()
    {
        _processedFiles.Clear();
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.FullPath))
        {
            return;
        }

        // Check if it's an image file
        var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
        if (!_imageExtensions.Contains(extension))
        {
            return;
        }

        // Check if we've already processed this file
        if (_processedFiles.Contains(e.FullPath))
        {
            return;
        }

        // Wait a bit for the file to be fully written
        System.Threading.Thread.Sleep(500);

        // Verify file exists and is accessible
        if (!File.Exists(e.FullPath))
        {
            return;
        }

        try
        {
            // Try to open the file to ensure it's not locked
            using (var stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // File is accessible
            }

            MarkFileAsProcessed(e.FullPath);
            FileCreated?.Invoke(this, e.FullPath);
        }
        catch
        {
            // File might still be locked, ignore for now
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        // Log error
        System.Diagnostics.Debug.WriteLine($"FileWatcher error: {e.GetException().Message}");
        // Continue watching - the watcher will attempt to recover
    }

    public void Dispose()
    {
        StopWatching();
    }
}
