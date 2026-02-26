using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbPasswordFile = Environment.GetEnvironmentVariable("DB_PASSWORD_FILE");
if (!string.IsNullOrEmpty(dbPasswordFile) && File.Exists(dbPasswordFile))
{
    var password = File.ReadAllText(dbPasswordFile).Trim();
    var csBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Password = password
    };
    connectionString = csBuilder.ConnectionString;
}

builder.Services.AddDbContext<SelfCerts.Api.Infrastructure.SelfCertsDbContext>(options =>
    options.UseNpgsql(connectionString, sqlOptions => 
    {
        // 增加数据库连接重试策略，应对数据库容器尚未就绪的情况
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    }));

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
