using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ScreenshotUploader.Models;

namespace ScreenshotUploader.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public ApiClient(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string?> GetActiveFlightIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_baseUrl}/flights/mine/api-token?completed=false";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Invalid API key. Please check your settings.");
                }
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<FlightListResponse>(json);
            
            if (result?.Flights != null && result.Flights.Count > 0)
            {
                // Return the most recent flight (first in list, assuming sorted by date)
                return result.Flights[0].Id;
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error while fetching active flight: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get active flight ID: {ex.Message}", ex);
        }
    }

    public async Task<bool> UploadScreenshotAsync(
        string filePath,
        string flightId,
        FlightData? flightData = null,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                
                // Add file
                var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                var fileName = Path.GetFileName(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
                content.Add(fileContent, "file", fileName);

                // Add flight ID
                content.Add(new StringContent(flightId), "flightId");

                // Add optional metadata
                if (flightData != null)
                {
                    if (flightData.Latitude.HasValue)
                    {
                        content.Add(new StringContent(flightData.Latitude.Value.ToString("F8")), "latitude");
                    }
                    if (flightData.Longitude.HasValue)
                    {
                        content.Add(new StringContent(flightData.Longitude.Value.ToString("F8")), "longitude");
                    }
                    if (flightData.Altitude.HasValue)
                    {
                        content.Add(new StringContent(flightData.Altitude.Value.ToString("F2")), "altitude");
                    }
                    if (flightData.Heading.HasValue)
                    {
                        content.Add(new StringContent(flightData.Heading.Value.ToString("F2")), "heading");
                    }
                    if (flightData.GroundSpeed.HasValue)
                    {
                        content.Add(new StringContent(flightData.GroundSpeed.Value.ToString("F2")), "groundSpeed");
                    }
                    if (flightData.OnGround.HasValue)
                    {
                        content.Add(new StringContent(flightData.OnGround.Value.ToString().ToLower()), "onGround");
                    }
                }

                var url = $"{_baseUrl}/screenshot/upload/api-token";
                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // If authentication error, don't retry
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Invalid API key. Please check your settings.");
                }

                // If bad request, don't retry (likely validation error)
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new Exception($"Bad request: {errorContent}");
                }

                // If not found, don't retry
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception("Flight not found. Please check your flight ID.");
                }

                // For other errors, retry with exponential backoff
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Upload failed after {maxRetries} attempts. Status: {response.StatusCode}. Error: {errorMsg}");
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                throw new Exception($"Upload failed after {maxRetries} attempts: {ex.Message}", ex);
            }
        }

        throw new Exception("Upload failed after all retry attempts.");
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private class FlightListResponse
    {
        [JsonProperty("flights")]
        public List<FlightInfo>? Flights { get; set; }
    }

    private class FlightInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
    }
}
