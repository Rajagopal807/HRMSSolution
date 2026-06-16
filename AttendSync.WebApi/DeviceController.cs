// ╔══════════════════════════════════════════════════════════════════╗
// ║  AttendSync.WebApi — DeviceController                           ║
// ║  Manages COM loading, device connection, and device metadata.   ║
// ╚══════════════════════════════════════════════════════════════════╝

using AttendSync.WebApi.Models;
using AttendSync.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendSync.WebApi.Controllers;

[ApiController]
[Route("api/device")]
[Produces("application/json")]
public class DeviceController : ControllerBase
{
    private readonly AttendSyncService _svc;
    private readonly ILogger<DeviceController> _log;

    public DeviceController(AttendSyncService svc, ILogger<DeviceController> log)
    {
        _svc = svc;
        _log = log;
    }

    // ── GET /api/device/status ─────────────────────────────────────────────────
    /// <summary>Returns current connection state and service flags.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<DeviceStatus>), 200)]
    public IActionResult GetStatus()
        => Ok(ApiResponse<DeviceStatus>.Ok(_svc.GetStatus()));

    // ── POST /api/device/load-com ──────────────────────────────────────────────
    /// <summary>
    /// Instantiates the ZKTeco CZKEM COM object and wires device events.
    /// Must be called before <c>connect</c>.
    /// Requires <c>regsvr32 zkemkeeper.dll</c> on the host.
    /// </summary>
    [HttpPost("load-com")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public IActionResult LoadCom()
    {
        bool ok = _svc.LoadCom();
        return ok
            ? Ok(ApiResponse<object>.Ok(null, "COM loaded"))
            : StatusCode(500, ApiResponse<object>.Fail(
                "Failed to create COM object. Ensure zkemkeeper.dll is registered (regsvr32)."));
    }

    // ── POST /api/device/unload-com ────────────────────────────────────────────
    /// <summary>
    /// Disconnects the device (if connected) and releases all COM interfaces.
    /// </summary>
    [HttpPost("unload-com")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult UnloadCom()
    {
        _svc.UnloadCom();
        return Ok(ApiResponse<object>.Ok(null, "COM unloaded"));
    }

    // ── POST /api/device/connect ───────────────────────────────────────────────
    /// <summary>
    /// Opens a TCP connection to a biometric device and syncs its clock.
    /// </summary>
    /// <remarks>
    /// Sample body:
    /// <code>
    /// {
    ///   "ipAddress":   "192.168.1.201",
    ///   "port":        4370,
    ///   "deviceId":    1,
    ///   "timeoutSecs": 30,
    ///   "password":    "",
    ///   "brand":       "ZKTeco"
    /// }
    /// </code>
    /// </remarks>
    [HttpPost("connect")]
    [ProducesResponseType(typeof(ApiResponse<DeviceInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 503)]
    public IActionResult Connect([FromBody] DeviceConfig cfg)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Invalid device configuration."));

        var (ok, error) = _svc.Connect(cfg);
        if (!ok)
            return StatusCode(503, ApiResponse<object>.Fail(error));

        // Return device info so the caller knows what they connected to
        try
        {
            var info = _svc.GetDeviceInfo();
            return Ok(ApiResponse<DeviceInfo>.Ok(info, "Connected"));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Connected but could not read device info");
            return Ok(ApiResponse<DeviceInfo>.Ok(null, "Connected (device info unavailable)"));
        }
    }

    // ── POST /api/device/disconnect ────────────────────────────────────────────
    /// <summary>Closes the TCP connection to the device.</summary>
    [HttpPost("disconnect")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Disconnect()
    {
        _svc.Disconnect();
        return Ok(ApiResponse<object>.Ok(null, "Disconnected"));
    }

    // ── GET /api/device/info ───────────────────────────────────────────────────
    /// <summary>
    /// Reads firmware version, serial number, device type, and capacity counters
    /// from the connected device.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<DeviceInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public IActionResult GetInfo()
    {
        try
        {
            return Ok(ApiResponse<DeviceInfo>.Ok(_svc.GetDeviceInfo()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
