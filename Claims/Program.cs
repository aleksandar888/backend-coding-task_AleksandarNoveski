using Claims.Auditing;
using Claims.Data;
using Claims.Services.Claims;
using Claims.Services.Covers;
using Claims.Services.CoverPremiumCalculator;
using Claims.Services.Logging;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var auditSqlConnectionString = builder.Configuration["ConnectionStrings:AuditSql"];
if (string.IsNullOrWhiteSpace(auditSqlConnectionString))
{
    throw new InvalidOperationException("The 'ConnectionStrings:AuditSql' configuration value is required.");
}

var mongoConnectionString = builder.Configuration["ConnectionStrings:MongoDb"];
if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException("The 'MongoDb:ConnectionString' configuration value is required.");
}

var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"];
if (string.IsNullOrWhiteSpace(mongoDatabaseName))
{
    throw new InvalidOperationException("The 'MongoDb:DatabaseName' configuration value is required.");
}

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer(auditSqlConnectionString));
builder.Services.AddSingleton<IAuditQueue, AuditQueue>();
builder.Services.AddHostedService<AuditBackgroundService>();

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

builder.Services.AddDbContext<ClaimsContext>((serviceProvider, options) =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoDatabaseName);
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
