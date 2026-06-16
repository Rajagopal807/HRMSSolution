# AttendSync Web API

REST + SSE wrapper around the ZKTeco / Anviz CZKEM COM SDK.  
Replaces the original named-pipe IPC layer with clean HTTP endpoints.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Windows 10 / Server 2016+ | COM is Windows-only |
| .NET 8 SDK | `dotnet --version` → `8.x` |
| `zkemkeeper.dll` registered | `regsvr32 "C:\ZKTeco\zkemkeeper.dll"` (run as Admin) |
| COM interop assembly | `tlbimp.exe zkemkeeper.dll /out:zkemkeeper.Interop.dll` → place in `lib\` |

---

## Setup

```bash
# 1. Add COM interop DLL reference (after tlbimp)
# Edit AttendSync.WebApi.csproj — uncomment the <Reference> element

# 2. Restore & run
dotnet restore
dotnet run --project AttendSync.WebApi

# Swagger UI → http://localhost:5100
```

To auto-connect on startup, populate `DeviceConfig` in `appsettings.json`.

---

## API Reference

### Device Management  `api/device`

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/api/device/status`     | COM loaded?, connected?, downloading? |
| `POST` | `/api/device/load-com`   | Instantiate CZKEM COM object |
| `POST` | `/api/device/unload-com` | Release COM interfaces |
| `POST` | `/api/device/connect`    | TCP connect + clock sync (body: `DeviceConfig`) |
| `POST` | `/api/device/disconnect` | Close TCP connection |
| `GET`  | `/api/device/info`       | Firmware, serial number, capacity |

### Attendance  `api/attendance`

| Method   | Path | Description |
|----------|------|-------------|
| `GET`    | `/api/attendance`              | Download all logs (returns count). `?clearAfter=true` wipes device after. |
| `GET`    | `/api/attendance/records`      | Download all logs and return as JSON array |
| `GET`    | `/api/attendance/range`        | `?from=YYYY-MM-DD&to=YYYY-MM-DD` — filtered download |
| `POST`   | `/api/attendance/range`        | Same as above, date range in request body |
| `DELETE` | `/api/attendance`              | ⚠️ Wipe device log (ClearGLog) |
| `POST`   | `/api/attendance/cancel`       | Cancel an in-progress download |
| `GET`    | `/api/attendance/realtime`     | SSE stream of real-time punch events |

---

## Request / Response Examples

### Connect to device
```http
POST /api/device/connect
Content-Type: application/json

{
  "ipAddress":   "192.168.1.201",
  "port":        4370,
  "deviceId":    1,
  "timeoutSecs": 30,
  "password":    "",
  "brand":       "ZKTeco"
}
```

```json
{
  "success": true,
  "message": "Connected",
  "data": {
    "firmwareVersion": "Ver 6.60 Apr 17 2023",
    "serialNumber":    "ACD1234567",
    "deviceType":      72,
    "storedRecords":   1048,
    "enrolledUsers":   215
  }
}
```

### Download records by date range
```http
GET /api/attendance/range?from=2024-06-01&to=2024-06-30
```

```json
{
  "success": true,
  "message": "320 record(s) between 01/06/2024 and 30/06/2024.",
  "data": [
    {
      "recordId":     1,
      "employeeId":   "1042",
      "employeeName": "Jane Doe",
      "timestamp":    "2024-06-03T08:47:22",
      "punchType":    0,
      "punchTypeLabel": "IN",
      "verifyMode":   1,
      "verifyLabel":  "FP:Pass",
      "deviceId":     1,
      "deviceIp":     "192.168.1.201"
    }
  ]
}
```

### Real-time SSE (JavaScript)
```js
const src = new EventSource('http://localhost:5100/api/attendance/realtime');

src.onmessage = (e) => {
  const punch = JSON.parse(e.data);
  console.log(`${punch.employeeId} punched ${punch.direction} at ${punch.timestamp}`);
};
```

---

## Typical workflow

```
POST /api/device/load-com          ← initialise COM once per process
POST /api/device/connect           ← connect to device
GET  /api/device/info              ← verify firmware / capacity
GET  /api/attendance/records       ← pull full log
DELETE /api/attendance             ← clear device log
POST /api/device/disconnect        ← optional — free TCP slot
```

---

## Error responses

All endpoints return `ApiResponse<T>`:

```json
{
  "success": false,
  "message": "Device not connected. POST /api/device/connect first.",
  "data": null
}
```

| HTTP Status | Meaning |
|-------------|---------|
| `200` | OK |
| `400` | Bad request (validation) |
| `409` | Conflict — device not connected / COM not loaded |
| `499` | Client closed request (download cancelled) |
| `500` | COM error / device error |
| `503` | Device refused connection |
