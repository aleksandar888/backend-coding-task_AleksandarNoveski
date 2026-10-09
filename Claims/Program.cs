using Claims.Auditing;
using Claims.Data;
using Claims.Services.Claims;
using Claims.Services.Covers;
using Claims.Services.CoverPremiumCalculator;
using Claims.Services.Logging;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

var builder = WebApplication.CreateBuilder(args);

// Start Testcontainers for SQL Server and MongoDB
var sqlContainer = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        : new()

    ).Build();

var mongoContainer = new MongoDbBuilder()
    .WithImage("mongo:latest")
    .Build();

await sqlContainer.StartAsync();
await mongoContainer.StartAsync();

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(sqlContainer.GetConnectionString()));
builder.Services.AddSingleton<IAuditQueue, AuditQueue>();
builder.Services.AddHostedService<AuditBackgroundService>();

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoContainer.GetConnectionString()));

builder.Services.AddDbContext<ClaimsContext>((serviceProvider, options) =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(builder.Configuration["MongoDb:DatabaseName"]); // Use a default/test database name
    options.UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName);
});

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IErrorLoggingService, ErrorLoggingService>();
builder.Services.AddScoped<IClaimsRepository, ClaimsRepository>();
builder.Services.AddScoped<ICoversRepository, CoversRepository>();
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<ICoversService, CoversService>();
builder.Services.AddSingleton<ICoverPremiumCalculator, CoverPremiumCalculator>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
    context.Database.Migrate();
}

app.Run();

public partial class Program { }
