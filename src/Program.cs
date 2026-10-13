using Microsoft.AspNetCore.HttpLogging;
using PowerRqlite.Interfaces;
using PowerRqlite.Interfaces.rqlite;
using PowerRqlite.Models;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Services;
using PowerRqlite.Services.PowerDNS;
using PowerRqlite.Services.rqlite;
using Serilog;
using Serilog.Events;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.Configure<RqliteOptions>(builder.Configuration.GetSection("rqliteOptions"));
builder.Services.Configure<PowerRqliteOptions>(builder.Configuration.GetSection("PowerRqlite"));
builder.Services.AddSingleton<IRqliteContext, RqliteContext>();
builder.Services.AddSingleton<ITransactionManager, TransactionManager>();
builder.Services.AddSingleton<IRqliteService, RqliteService>();
builder.Services.AddHostedService<InitService>();
builder.Services.AddHealthChecks().AddCheck<RqliteHealthCheck>("rqlite");

builder.Services.AddHttpLogging(o => { o.LoggingFields = HttpLoggingFields.All; });

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
WebApplication app = builder.Build();

app.UseHttpLogging();
// PowerDNS sends several requests per DNS query; logging each one at Information floods the log under load.
// Successful requests are logged at Debug, failures stay at Error.
app.UseSerilogRequestLogging(options => options.GetLevel = (context, _, ex) =>
    ex is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error : LogEventLevel.Debug);
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
await app.StartAsync();

Log.Information("Hosting environment: {HostingEnv}", app.Environment.EnvironmentName);
Log.Information("Now listening on: {Urls}", string.Join(",", app.Urls));
Log.Information("PowerRqlite started. Press  Ctrl+C to shut down.");

await app.WaitForShutdownAsync();
