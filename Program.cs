using Microsoft.Extensions.Azure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting dd-cleaneveryday-api");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var storageConnection = builder.Configuration["AzureStorage:ConnectionString"]
    ?? throw new InvalidOperationException("AzureStorage:ConnectionString is required");

builder.Services.AddAzureClients(clients =>
{
    clients.AddTableServiceClient(storageConnection);
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("dd-cleaneveryday-api started successfully");
app.Run();
