// ╔══════════════════════════════════════════════════════════════════╗
// ║  AttendSync.WebApi — Program.cs                                 ║
// ╚══════════════════════════════════════════════════════════════════╝

using AttendSync.WebApi.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddNewtonsoftJson(opt =>
    {
        opt.SerializerSettings.DateTimeZoneHandling =
            Newtonsoft.Json.DateTimeZoneHandling.Local;
    });

// COM object must live for the process lifetime → Singleton
// Note: ZKTeco COM requires MTA; ASP.NET Core thread pool is MTA by default.
builder.Services.AddSingleton<AttendSyncService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "AttendSync Biometric Reader API",
        Version     = "v1",
        Description =
            "REST interface for ZKTeco biometric attendance devices. " +
            "Wraps the CZKEM COM SDK with a clean HTTP layer and SSE real-time push.",
        Contact = new OpenApiContact { Name = "AttendSync" },
    });

    // Include XML doc comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// CORS — tighten in production
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ── App pipeline ───────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AttendSync v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// ── Graceful shutdown — release COM on exit ────────────────────────────────────
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var svc = app.Services.GetRequiredService<AttendSyncService>();
    svc.Dispose();
});

app.Run();
