// ╔══════════════════════════════════════════════════════════════════╗
// ║  AttendSync.WebApi — AttendanceController                       ║
// ║  Attendance download (full / date-range), clear, and           ║
// ║  Server-Sent Events (SSE) for real-time punch notifications.   ║
// ╚══════════════════════════════════════════════════════════════════╝

using System.Text;
using AttendSync.WebApi.Models;
using AttendSync.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AttendSync.WebApi.Controllers;

[ApiController]
[Route("api/attendance")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly AttendSyncService _svc;

    public AttendanceController(AttendSyncService svc) => _svc = svc;

    // ── GET /api/attendance ────────────────────────────────────────────────────
    /// <summary>
    /// Downloads the complete attendance log from the device buffer.
    /// This temporarily disables the device during the read.
    /// Pass <c>?clearAfter=true</c> to wipe the log from the device once downloaded.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DownloadResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> DownloadAll(
        [FromQuery] bool clearAfter = false,
        CancellationToken ct = default)
    {
        try
        {
            var progress = new Progress<int>(); // wired to SSE / logs inside service
            var records  = await _svc.DownloadAllAsync(progress, ct);

            if (clearAfter)
                _svc.ClearDeviceLog();

            return Ok(ApiResponse<DownloadResult>.Ok(
                new DownloadResult
                {
                    RecordCount = records.Count,
                    Status      = clearAfter ? "downloaded_and_cleared" : "downloaded",
                },
                $"{records.Count} record(s) downloaded."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, ApiResponse<object>.Fail("Download cancelled by client."));
        }
    }

    // ── GET /api/attendance/records ────────────────────────────────────────────
    /// <summary>
    /// Downloads and returns the full attendance log as a JSON array.
    /// For large fleets prefer <c>GET /api/attendance</c> (count only)
    /// and stream via <c>GET /api/attendance/stream</c>.
    /// </summary>
    [HttpGet("records")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceRecord>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> GetAllRecords(CancellationToken ct = default)
    {
        try
        {
            var records = await _svc.DownloadAllAsync(null, ct);
            return Ok(ApiResponse<List<AttendanceRecord>>.Ok(
                records, $"{records.Count} record(s)"));
        }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
        catch (OperationCanceledException)   { return StatusCode(499, ApiResponse<object>.Fail("Cancelled.")); }
    }

    // ── GET /api/attendance/range ──────────────────────────────────────────────
    /// <summary>
    /// Downloads attendance records within a date range (inclusive).
    /// </summary>
    /// <param name="from">Start date/time  e.g. 2024-06-01T00:00:00</param>
    /// <param name="to">  End date/time    e.g. 2024-06-30T23:59:59</param>
    /// <param name="clearAfter">Wipe device log after download.</param>
    [HttpGet("range")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceRecord>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> GetByRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool clearAfter = false,
        CancellationToken ct = default)
    {
        if (from > to)
            return BadRequest(ApiResponse<object>.Fail("'from' must be before 'to'."));

        try
        {
            var records = await _svc.DownloadRangeAsync(from, to, null, ct);

            if (clearAfter)
                _svc.ClearDeviceLog();

            return Ok(ApiResponse<List<AttendanceRecord>>.Ok(
                records, $"{records.Count} record(s) between {from:d} and {to:d}."));
        }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
        catch (OperationCanceledException)   { return StatusCode(499, ApiResponse<object>.Fail("Cancelled.")); }
    }

    // ── POST /api/attendance/range ─────────────────────────────────────────────
    /// <summary>
    /// Body-based alternative to <c>GET /api/attendance/range</c>
    /// for clients that cannot send query parameters.
    /// </summary>
    [HttpPost("range")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceRecord>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> PostByRange(
        [FromBody] DateRangeRequest req,
        CancellationToken ct = default)
        => await GetByRange(req.From, req.To, false, ct);

    // ── DELETE /api/attendance ─────────────────────────────────────────────────
    /// <summary>
    /// Wipes the attendance log from device memory (ClearGLog).
    /// ⚠️ This is irreversible — download first.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public IActionResult ClearLog()
    {
        try
        {
            bool ok = _svc.ClearDeviceLog();
            return ok
                ? Ok(ApiResponse<object>.Ok(null, "Device log cleared."))
                : StatusCode(500, ApiResponse<object>.Fail("ClearGLog returned false."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST /api/attendance/cancel ────────────────────────────────────────────
    /// <summary>Cancels any in-progress attendance download.</summary>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Cancel()
    {
        _svc.CancelDownload();
        return Ok(ApiResponse<object>.Ok(null, "Cancel signal sent."));
    }

    // ── GET /api/attendance/realtime ───────────────────────────────────────────
    /// <summary>
    /// Opens a Server-Sent Events (SSE) stream.
    /// The server pushes a <c>RealtimePunchEvent</c> JSON object every time
    /// the device fires an <c>OnAttTransaction</c> COM event.
    ///
    /// Keep the connection open; close it to unsubscribe.
    /// </summary>
    /// <remarks>
    /// Connect with:
    /// <code>
    /// const src = new EventSource('/api/attendance/realtime');
    /// src.onmessage = e => console.log(JSON.parse(e.data));
    /// </code>
    /// </remarks>
    [HttpGet("realtime")]
    [Produces("text/event-stream")]
    public async Task StreamRealtime(CancellationToken ct)
    {
        var (ok, error) = _svc.StartRealtimeFromConfiguredDevice();
        if (!ok)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(ApiResponse<object>.Fail(error), ct);
            return;
        }

        Response.Headers["Content-Type"]  = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"]    = "keep-alive";
        await Response.Body.FlushAsync(ct);

        var (subId, reader) = _svc.SubscribeRealtime();

        try
        {
            // Keep-alive ping every 20 s
            using var pingTimer = new PeriodicTimer(TimeSpan.FromSeconds(20));
            var pingTask = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await pingTimer.WaitForNextTickAsync(ct);
                    await WriteEvent(": ping\n\n", ct);
                }
            }, ct);

            await foreach (var evt in reader.ReadAllAsync(ct))
            {
                var json    = JsonConvert.SerializeObject(evt);
                var payload = $"data: {json}\n\n";
                await WriteEvent(payload, ct);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
            _svc.UnsubscribeRealtime(subId);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task WriteEvent(string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await Response.Body.WriteAsync(bytes, ct);
        await Response.Body.FlushAsync(ct);
    }
}
