// ╔══════════════════════════════════════════════════════╗
// ║  AttendSync.WebApi — Models                         ║
// ╚══════════════════════════════════════════════════════╝

namespace AttendSync.WebApi.Models;

// ── Domain entities ────────────────────────────────────────────────────────────

public class AttendanceRecord
{
    public int      RecordId     { get; set; }
    public string   EmployeeId   { get; set; } = string.Empty;
    public string   EmployeeName { get; set; } = string.Empty;
    public DateTime Timestamp    { get; set; }
    public int      PunchType    { get; set; }   // 0=IN, 1=OUT, 4=Break
    public int      VerifyMode   { get; set; }   // 1=FP, 3=PIN, 4=Card
    public int      DeviceId     { get; set; }
    public string   DeviceIp     { get; set; } = string.Empty;

    public string PunchTypeLabel => PunchType switch
    {
        0 => "IN", 1 => "OUT", 4 => "BREAK", _ => "OTHER"
    };

    public string VerifyLabel => VerifyMode switch
    {
        1 => "FP:Pass", 3 => "PIN", 4 => "CARD", _ => "FP:Pass"
    };
}

public class DeviceInfo
{
    public string FirmwareVersion { get; set; } = string.Empty;
    public string SerialNumber    { get; set; } = string.Empty;
    public int    Administrator      { get; set; }
    public int    StoredRecords   { get; set; }
    public int    EnrolledUsers   { get; set; }
}

// ── Request / Response DTOs ────────────────────────────────────────────────────

public class DeviceConfig
{
    public string IpAddress   { get; set; } = "192.168.1.201";
    public int    Port        { get; set; } = 4370;
    public int    DeviceId    { get; set; } = 1;
    public int    TimeoutSecs { get; set; } = 30;
    public string Password    { get; set; } = string.Empty;
    public string Brand       { get; set; } = "ZKTeco";
    public string PhotoPath   { get; set; } = string.Empty; // optional path to save downloaded photos
}

public class DateRangeRequest
{
    public DateTime From { get; set; }
    public DateTime To   { get; set; }
}

public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T?     Data    { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "OK")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class DownloadResult
{
    public int    RecordCount { get; set; }
    public string Status      { get; set; } = string.Empty;
}

public class DeviceStatus
{
    public bool   IsComLoaded  { get; set; }
    public bool   IsConnected  { get; set; }
    public string IpAddress    { get; set; } = string.Empty;
    public int    DeviceId     { get; set; }
    public bool   IsDownloading{ get; set; }
    public bool   IsRealtimeRegistered { get; set; }
    public int    RealtimeSubscribers  { get; set; }
    public int    RealtimeEventCount   { get; set; }
    public DateTime? LastRealtimeRegisteredAt { get; set; }
    public DateTime? LastRealtimeEventAt      { get; set; }
    public string LastRealtimeError { get; set; } = string.Empty;
}

// ── Real-time punch event (for SSE) ────────────────────────────────────────────
public class RealtimePunchEvent
{
    public string   EmployeeId { get; set; } = string.Empty;
    public string   Direction  { get; set; } = string.Empty;  // IN | OUT
    public DateTime Timestamp  { get; set; }
    public int      VerifyMode { get; set; }
    public int      WorkCode   { get; set; }
    public string   Source     { get; set; } = string.Empty;
}
public class PhotoDownloadResult
{
    public string PhotoName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string LocalPath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
