// ╔══════════════════════════════════════════════════════════════════╗
// ║  AttendSync.WebApi — AttendSyncService                          ║
// ║  Singleton that owns the COM reader and exposes a clean async   ║
// ║  interface for controllers and the SSE push channel.            ║
// ╚══════════════════════════════════════════════════════════════════╝

using AttendSync.WebApi.Models;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
// Channel<T> shim so the file compiles without extra imports
using System.Threading.Channels;
using zkemkeeper;
// using zkemkeeper;   ← uncomment once COM DLL is referenced

namespace AttendSync.WebApi.Services;

public sealed class AttendSyncService : IDisposable
{
    // ── State ──────────────────────────────────────────────────────────────────
    private  CZKEM _zkem;          // COM object — typed as object until COM ref added
    private bool                _comLoaded;
    private bool                _connected;
    private bool                _downloading;
    private bool                _realtimeRegistered;
    private int                 _realtimeEventCount;
    private DateTime?           _lastRealtimeRegisteredAt;
    private DateTime?           _lastRealtimeEventAt;
    private string              _lastRealtimeError = string.Empty;
    private DeviceConfig?       _cfg;

    private CancellationTokenSource? _downloadCts;
    private readonly object          _lock = new();
    private readonly ILogger<AttendSyncService> _log;
    private readonly IConfiguration _configuration;

    // ── SSE channel: subscribers receive real-time punch events ───────────────
    private readonly ConcurrentDictionary<Guid, Channel<RealtimePunchEvent>> _sseClients = new();

    public AttendSyncService(ILogger<AttendSyncService> log, IConfiguration configuration)
    {
        _log = log;
        _configuration = configuration;
    }

    // ── Status ─────────────────────────────────────────────────────────────────
    public DeviceStatus GetStatus() => new()
    {
        IsComLoaded   = _comLoaded,
        IsConnected   = _connected,
        IpAddress     = _cfg?.IpAddress ?? string.Empty,
        DeviceId      = _cfg?.DeviceId  ?? 0,
        IsDownloading = _downloading,
        IsRealtimeRegistered = _realtimeRegistered,
        RealtimeSubscribers = _sseClients.Count,
        RealtimeEventCount = _realtimeEventCount,
        LastRealtimeRegisteredAt = _lastRealtimeRegisteredAt,
        LastRealtimeEventAt = _lastRealtimeEventAt,
        LastRealtimeError = _lastRealtimeError,
    };

    // ── COM lifecycle ──────────────────────────────────────────────────────────

    /// <summary>Instantiates the CZKEM COM object and wires device events.</summary>
    public bool LoadCom()
    {
        if (_comLoaded) return true;
        try
        {
            _log.LogInformation("Creating COM instance: zkemkeeper.CZKEM...");

            /* ── Uncomment when COM reference is available ──────────────────*/
            var zkem = new CZKEM();
            zkem.OnAttTransaction += Zkem_OnAttTransaction;
            zkem.OnAttTransactionEx += Zkem_OnAttTransactionEx;
            zkem.OnVerify         += OnVerify;
            zkem.OnDisConnected   += OnDisconnected;
            zkem.OnConnected      += OnConnectedEvt;
            _zkem = zkem;
            /*─────────────────────────────────────────────────────────────── */

            _comLoaded = true;
            _log.LogInformation("COM created OK");
            return true;
        }
        catch (COMException ex)
        {
            _log.LogError("COM create failed: HRESULT=0x{Hr:X8} — {Msg}", ex.HResult, ex.Message);
            return false;
        }
    }

    /// <summary>Releases all COM interfaces.</summary>
    public void UnloadCom()
    {
        if (_connected) Disconnect();
        if (_zkem != null)
        {
            /* ── Uncomment when COM reference is available ──────────────────*/
            var zkem = _zkem;
            zkem.OnAttTransaction -= Zkem_OnAttTransaction;
            zkem.OnAttTransactionEx -= Zkem_OnAttTransactionEx;
            zkem.OnVerify         -= OnVerify;
            zkem.OnDisConnected   -= OnDisconnected;
            zkem.OnConnected      -= OnConnectedEvt;
            Marshal.ReleaseComObject(zkem);
            /*─────────────────────────────────────────────────────────────── */

            _zkem = null;
            _comLoaded = false;
            _realtimeRegistered = false;
            _log.LogInformation("COM interfaces released");
        }
    }

    // ── Device connection ──────────────────────────────────────────────────────

    /// <summary>Opens a TCP connection to the biometric device.</summary>
    public (bool ok, string error) Connect(DeviceConfig cfg)
    {
        lock (_lock)
        {
            LoadCom();
            int err = 0;
            if (!_comLoaded)
                return (false, "COM not loaded. Call /device/load-com first.");

            _cfg = cfg;
            _log.LogInformation("Connecting TCP → {Ip}:{Port} (DeviceID={Id})...",
                cfg.IpAddress, cfg.Port, cfg.DeviceId);

            /* ── Uncomment when COM reference is available ──────────────────*/
            var zkem = (CZKEM)_zkem!;
            if (!string.IsNullOrWhiteSpace(cfg.Password) && int.TryParse(cfg.Password, out var password))
                zkem.SetCommPassword(password);

            bool ok = zkem.Connect_Net(cfg.IpAddress, cfg.Port);
            if (!ok)
            {
                zkem.GetLastError(ref err);
                return (false, $"Connection refused by device — ZKTeco error {err}");
            }

            zkem.SetDeviceTime2(cfg.DeviceId,
                DateTime.Now.Year,   DateTime.Now.Month,  DateTime.Now.Day,
                DateTime.Now.Hour,   DateTime.Now.Minute, DateTime.Now.Second);
            /*─────────────────────────────────────────────────────────────── */

            _connected = true;
            _log.LogInformation("Connected to {Ip}", cfg.IpAddress);
            return (true, string.Empty);
        }
    }

    /// <summary>
    /// Ensures the realtime COM event stream is ready using the configured device.
    /// </summary>
    public (bool ok, string error) StartRealtimeFromConfiguredDevice()
    {
        var cfg = _configuration.GetSection("DeviceConfig").Get<DeviceConfig>();
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.IpAddress))
        {
            if (_connected && _cfg != null)
                return StartRealtime(_cfg);

            return (false, "DeviceConfig is missing. Configure DeviceConfig in appsettings.json or call /api/device/connect first.");
        }

        return StartRealtime(cfg);
    }

    public (bool ok, string error) ConnectDeviceFromConfiguredDevice()
    {
        var cfg = _configuration.GetSection("DeviceConfig").Get<DeviceConfig>();
        if (cfg == null || string.IsNullOrWhiteSpace(cfg.IpAddress))
        {
            if (_connected && _cfg != null)
                return Connect(_cfg);

            return (false, "DeviceConfig is missing. Configure DeviceConfig in appsettings.json or call /api/device/connect first.");
        }

        return Connect(cfg);
    }

    /// <summary>
    /// Loads COM, connects to the device if needed, and registers all realtime events.
    /// </summary>
    public (bool ok, string error) StartRealtime(DeviceConfig cfg)
    {
        if (_connected && _cfg != null && !IsSameDevice(_cfg, cfg))
            Disconnect();

        var (ok, error) = _connected ? (true, string.Empty) : Connect(cfg);
        if (!ok)
            return (false, error);

        lock (_lock)
        {
            int err = 0;
            bool registered = _zkem.RegEvent(_cfg!.DeviceId, 65535);
            if (!registered)
            {
                _zkem.GetLastError(ref err);
                _realtimeRegistered = false;
                _lastRealtimeError = $"Connected, but realtime event registration failed — ZKTeco error {err}";
                return (false, _lastRealtimeError);
            }

            _realtimeRegistered = true;
            _lastRealtimeRegisteredAt = DateTime.Now;
            _lastRealtimeError = string.Empty;
            _log.LogInformation("Realtime attendance events registered for device {DeviceId} at {Ip}",
                _cfg.DeviceId, _cfg.IpAddress);
            return (true, string.Empty);
        }
    }

    /// <summary>Disconnects from the device.</summary>
    public void Disconnect()
    {
        if (_connected)
        {
            _zkem.Disconnect();
            _connected = false;
            _realtimeRegistered = false;
            _log.LogInformation("Device disconnected");
        }
    }

    // ── Device info ────────────────────────────────────────────────────────────

    /// <summary>Reads firmware, serial number, and capacity from the device.</summary>
    public DeviceInfo GetDeviceInfo()
    {
        EnsureConnected();

        string fw = string.Empty;
        int admin = 0, records = 0, users = 0;
        

        /* ── Uncomment when COM reference is available ──────────────────────*/
        var zkem = _zkem!;
        zkem.GetSerialNumber    (_cfg!.DeviceId, out string serial);
        zkem.GetFirmwareVersion (_cfg!.DeviceId, ref fw);
        zkem.GetDeviceStatus    (_cfg!.DeviceId, 1, ref admin);
        zkem.GetDeviceStatus    (_cfg!.DeviceId, 6, ref records);
        zkem.GetDeviceStatus    (_cfg!.DeviceId, 2, ref users);
        return new DeviceInfo
        {
            SerialNumber    = serial,
            FirmwareVersion = fw,
            Administrator   = admin,
            StoredRecords   = records,
            EnrolledUsers   = users,
        };
       /* ─────────────────────────────────────────────────────────────────── */
    }

    // ── Device Photo ────────────────────────────────────────────────────────────
    /// <summary>Reads the full attendance log from the device buffer.</summary>
    public async Task<List<PhotoDownloadResult>> DownloadPhotoAsync(
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        EnsureConnected();
        _downloading = true;

        try
        {
            return await Task.Run(() => ReadAllPhoto(progress, ct), ct);
        }
        finally
        {
            _downloading = false;
        }
    }

    // ── Attendance download ────────────────────────────────────────────────────

    /// <summary>Reads the full attendance log from the device buffer.</summary>
    public async Task<List<AttendanceRecord>> DownloadAllAsync(
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        EnsureConnected();
        _downloading = true;

        try
        {
            return await Task.Run(() => ReadAttendance(null, null, progress, ct), ct);
        }
        finally
        {
            _downloading = false;
        }
    }

    /// <summary>Reads attendance records within a date range.</summary>
    public async Task<List<AttendanceRecord>> DownloadRangeAsync(
        DateTime from, DateTime to,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        EnsureConnected();
        _downloading = true;

        try
        {
            return await Task.Run(() => ReadAttendance(from, to, progress, ct), ct);
        }
        finally
        {
            _downloading = false;
        }
    }

    /// <summary>Cancels any in-progress download.</summary>
    public void CancelDownload() => _downloadCts?.Cancel();

    /// <summary>Wipes the attendance log from device memory.</summary>
    public bool ClearDeviceLog()
    {
        EnsureConnected();
        _log.LogInformation("Clearing device attendance log (ClearGLog)...");

        bool ok = _zkem.ClearGLog(_cfg!.DeviceId);

        _log.LogInformation(ok ? "Device log cleared OK" : "ClearGLog failed");
        return ok;
    }

    // ── SSE subscriptions ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a channel that receives real-time <see cref="RealtimePunchEvent"/>
    /// objects as the device fires OnAttTransaction COM events.
    /// Dispose the subscription when the SSE connection closes.
    /// </summary>
    public (Guid id, ChannelReader<RealtimePunchEvent> reader) SubscribeRealtime()
    {
        var ch  = Channel.CreateUnbounded<RealtimePunchEvent>();
        var id  = Guid.NewGuid();
        _sseClients[id] = ch;
        return (id, ch.Reader);
    }

    public void UnsubscribeRealtime(Guid id)
    {
        if (_sseClients.TryRemove(id, out var ch))
            ch.Writer.TryComplete();
    }

    // ── Private helpers ────────────────────────────────────────────────────────
    
    private List<AttendanceRecord> ReadAttendance(
        DateTime? from, DateTime? to,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        lock (_lock)
        {
            /* ── Uncomment when COM reference is available ──────────────────*/
            var zkem = (CZKEM)_zkem!;
            zkem.EnableDevice(_cfg!.DeviceId, false);
            zkem.ReadAllGLogData(_cfg!.DeviceId);

            var userMap = BuildUserMap(zkem);
            var records = new List<AttendanceRecord>();
            int index   = 0;
            int dwWorkCode = 0;

            while (!ct.IsCancellationRequested)
            {
                bool more = zkem.SSR_GetGeneralLogData(
                    _cfg.DeviceId,
                    out string empId,
                    out int    verifyMode,
                    out int    inOutStatus,
                    out int    dwYear,
                    out int    dwMonth,
                    out int    dwDay,
                    out int    dwHour,
                    out int    dwMinute,
                    out int    dwSeconds,dwWorkCode);

                string verifyDate = DateTime.Parse($"{dwDay}/{dwMonth}/{dwYear}", new CultureInfo("en-GB")).ToString();
                string verifyTime = DateTime.Parse($"{dwHour}:{dwMinute}:{dwSeconds}", new CultureInfo("en-GB")).ToString();

                if (!more) break;

                var rec = new AttendanceRecord
                {
                    RecordId     = ++index,
                    EmployeeId   = empId,
                    EmployeeName = userMap.TryGetValue(empId, out var n) ? n : empId,
                    Timestamp    = ParseDateTime(verifyDate, verifyTime),
                    PunchType    = inOutStatus,
                    VerifyMode   = verifyMode,
                    DeviceId     = _cfg.DeviceId,
                    DeviceIp     = _cfg.IpAddress,
                };

                if (from == null || (rec.Timestamp >= from && rec.Timestamp <= to!.Value.AddDays(1)))
                    records.Add(rec);

                progress?.Report(++index);
            }

            zkem.EnableDevice(_cfg!.DeviceId, true);
            return records;
           /* ─────────────────────────────────────────────────────────────── */

            // Stub — returns empty list until COM wired up
            _log.LogInformation("ReadAttendance stub called (from={From} to={To})", from, to);
            return new List<AttendanceRecord>();
        }
    }

    /* ── COM event callbacks ───────────────────────────────────────────────── */
    private void Zkem_OnAttTransaction(int EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second)
    {
        var evt = new RealtimePunchEvent
        {
            EmployeeId = EnrollNumber.ToString(),
            Direction = GetDirection(AttState),
            Timestamp = new DateTime(Year, Month, Day, Hour, Minute, Second),
            VerifyMode = VerifyMethod,
            Source = nameof(Zkem_OnAttTransaction),
        };

        PublishRealtimeEvent(evt);
    }

    private void Zkem_OnAttTransactionEx(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
    {
        var evt = new RealtimePunchEvent
        {
            EmployeeId = EnrollNumber,
            Direction = GetDirection(AttState),
            Timestamp = new DateTime(Year, Month, Day, Hour, Minute, Second),
            VerifyMode = VerifyMethod,
            WorkCode = WorkCode,
            Source = nameof(Zkem_OnAttTransactionEx),
        };

        PublishRealtimeEvent(evt);
    }

    private void PublishRealtimeEvent(RealtimePunchEvent evt)
    {
        _lastRealtimeEventAt = DateTime.Now;
        Interlocked.Increment(ref _realtimeEventCount);

        _log.LogInformation("REAL-TIME punch accepted from reader via {Source}: {EmpId} {Dir} {Ts} subscribers={Subscribers}",
            evt.Source, evt.EmployeeId, evt.Direction, evt.Timestamp, _sseClients.Count);

        foreach (var ch in _sseClients.Values)
            ch.Writer.TryWrite(evt);
    }

    private static string GetDirection(int attState)
        => attState switch
        {
            0 => "IN",
            1 => "OUT",
            4 => "BREAK",
            _ => "OTHER",
        };

    private void OnVerify(int userId)
        => _log.LogDebug("Device verify event: UserID={Id}", userId);

    private void OnConnectedEvt()
        => _log.LogInformation("Device connected event received");

    private void OnDisconnected()
    {
        _connected = false;
        _realtimeRegistered = false;
        _log.LogWarning("Device disconnected unexpectedly");
    }

    private Dictionary<string, string> BuildUserMap(CZKEM zkem)
    {
        var map = new Dictionary<string, string>();
        zkem.ReadAllUserID(_cfg!.DeviceId);
        while (zkem.SSR_GetAllUserInfo(_cfg.DeviceId,
            out string uid, out string name,
            out string pass, out int priv, out bool enabled))
            map[uid] = name;
        return map;
    }

    private static DateTime ParseDateTime(string date, string time)
    {
        try   { return DateTime.Parse($"{date} {time}"); }
        catch { return DateTime.UtcNow; }
    }

    private static bool IsSameDevice(DeviceConfig current, DeviceConfig configured)
        => string.Equals(current.IpAddress, configured.IpAddress, StringComparison.OrdinalIgnoreCase)
           && current.Port == configured.Port
           && current.DeviceId == configured.DeviceId;
    /*─────────────────────────────────────────────────────────────────────── */

    private void EnsureConnected()
    {
        if (!_connected)
            throw new InvalidOperationException(
                "Device not connected. POST /api/device/connect first.");
    }

    public void Dispose() => UnloadCom();
}


