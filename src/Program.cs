using Microsoft.AspNetCore.HttpLogging;
using PowerRqlite.Interfaces;
using PowerRqlite.Interfaces.rqlite;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Services;
using PowerRqlite.Services.PowerDNS;
using PowerRqlite.Services.rqlite;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.Configure<RqliteOptions>(builder.Configuration.GetSection("rqliteOptions"));
builder.Services.AddSingleton<IRqliteContext, RqliteContext>();
builder.Services.AddSingleton<ITransactionManager, TransactionManager>();
builder.Services.AddSingleton<IRqliteService, RqliteService>();
builder.Services.AddHostedService<InitService>();

builder.Services.AddHttpLogging(o => { o.LoggingFields = HttpLoggingFields.All; });

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

await app.StartAsync();

Log.Information("Hosting environment: {HostingEnv}", app.Environment.EnvironmentName);
Log.Information("Now listening on: {Urls}", string.Join(",", app.Urls));
Log.Information("PowerRqlite started. Press  Ctrl+C to shut down.");

await app.WaitForShutdownAsync();
