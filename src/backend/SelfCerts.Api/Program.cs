using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=selfcerts.db";

builder.Services.AddDbContext<SelfCerts.Api.Infrastructure.SelfCertsDbContext>(options =>
    options.UseSqlite(connectionString));

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddScoped<SelfCerts.Api.Services.OpenSslService>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register Global Exception Handler
builder.Services.AddExceptionHandler<SelfCerts.Api.Infrastructure.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Auto create database tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SelfCerts.Api.Infrastructure.SelfCertsDbContext>();
    db.Database.EnsureCreated();

    // 修复 SQLite 挂载空文件或 0 字节文件时，EnsureCreated 跳过建表的问题
    if (db.Database.IsSqlite())
    {
        var creator = db.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
        if (creator != null && !creator.HasTables())
        {
            try
            {
                creator.CreateTables();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("SQLite tables have been successfully created.");
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to create SQLite tables. Please ensure the /app/data directory on the host has the correct write permissions.");
                throw;
            }
        }
    }
}

// Use Exception Handler
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
//app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
