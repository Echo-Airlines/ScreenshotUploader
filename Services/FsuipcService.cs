using System;
using System.Threading;
using FSUIPC;
using ScreenshotUploader.Models;

namespace ScreenshotUploader.Services;

public class FsuipcService : IDisposable
{
    private bool _isConnected = false;
    private bool _isDisposed = false;

    // FSUIPC offsets
    private Offset<FsLatitude>? _latitudeOffset;
    private Offset<FsLongitude>? _longitudeOffset;
    private Offset<FsAltitude>? _altitudeOffset;
    private Offset<uint>? _groundSpeedOffset;
    private Offset<double>? _headingOffset;
    private Offset<ushort>? _onGroundOffset;
    private FsLatitude latitude;
    private FsLongitude longitude;
    private FsAltitude altitude;

    public bool IsConnected => _isConnected;

    public bool Connect()
    {
        if (_isDisposed)
        {
            return false;
        }

        try
        {
            if (!FSUIPCConnection.IsOpen)
            {
                FSUIPCConnection.Open();
                _isConnected = true;
            }

            _latitudeOffset = new Offset<FsLatitude>("LatLong", 0x0560, 8);
            _longitudeOffset = new Offset<FsLongitude>("LatLong", 0x0568, 8);
            _groundSpeedOffset = new Offset<uint>(0x02BC);
            _altitudeOffset = new Offset<FsAltitude>(0x0B4C);

            this.latitude = _latitudeOffset.Value;
            this.longitude = _longitudeOffset.Value;
            // Initialize offsets
            this.altitude = _altitudeOffset.Value;
            _headingOffset = new Offset<double>(0x02CC);
            _onGroundOffset = new Offset<ushort>(0x0366);

            return true;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            // Log or handle FSUIPC connection error
            // For now, we'll just return false and let the caller handle it
            System.Diagnostics.Debug.WriteLine($"FSUIPC connection failed: {ex.Message}");
            return false;
        }
    }

    private bool openConnection()
    {
        bool success = false;
        // Tries to open a connection to a flight sim.
        try
        {
            FSUIPCConnection.Open();
            // If we get here then the connection is open
            success = true;
        }
        catch
        {
            // connection failed. No need to do anything, we just keep trying
        }
        return success;
    }

    private void timerConnection_Tick(object sender, EventArgs e)
    {
        // try to open the connection
        if (openConnection())
        {
            //// stop the timer that looks for a connection
            //this.timerConnection.Stop();
            //// start the main timer
            //this.timerMain.Start();
            //// call on connected
            onConnected();
        }
    }
    protected virtual void onConnected() { }
    protected virtual void timerMain_Tick(object sender, EventArgs e)
    {

    }

    public FlightData? ReadFlightData()
    {
        if (!_isConnected || _isDisposed)
        {
            return null;
        }

        try
        {
            // Process FSUIPC to read all offsets
            FSUIPCConnection.Process();

            var flightData = new FlightData();

            // Read latitude (degrees)
            if (_latitudeOffset != null)
            {
                flightData.Latitude = this.latitude.DecimalDegrees;
            }

            // Read longitude (degrees)
            if (_longitudeOffset != null)
            {
                flightData.Longitude = this.longitude.DecimalDegrees;
            }

            // Read altitude (FS units = feet * 256, convert to feet)
            if (_altitudeOffset != null)
            {
                flightData.Altitude = this.altitude.Feet;
            }

            // Read heading (degrees = value * 360 / 65536)
            if (_headingOffset != null)
            {
                flightData.Heading = (_headingOffset.Value * 360.0) / 65536.0;
            }

            // Read ground speed (knots = value * 128 / 65536)
            if (_groundSpeedOffset != null)
            {
                flightData.GroundSpeed = (_groundSpeedOffset.Value * 128.0) / 65536.0;
            }

            // Read on ground flag (bit 0 of byte)
            if (_onGroundOffset != null)
            {
                flightData.OnGround = (_onGroundOffset.Value & 0x01) != 0;
            }

            return flightData;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - allow uploads to continue without metadata
            System.Diagnostics.Debug.WriteLine($"FSUIPC read error: {ex.Message}");
            return null;
        }
    }

    public void Disconnect()
    {
        if (_isConnected)
        {
            try
            {
                FSUIPCConnection.Close();
            }
            catch
            {
                // Ignore errors on close
            }
            _isConnected = false;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Disconnect();
            _isDisposed = true;
        }
    }
}
