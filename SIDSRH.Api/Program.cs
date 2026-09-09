using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIDSRH.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var sqliteCs = builder.Configuration["Database:Sqlite"] ?? "Data Source=sidsrh_offline.db";
var sqlServerCs = builder.Configuration["Database:SqlServer"];
var mySqlCs = builder.Configuration["Database:MySql"];

if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(mySqlCs))
{
    try
    {
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseMySql(mySqlCs, ServerVersion.AutoDetect(mySqlCs)));
        Console.WriteLine("Database provider: MySQL (Phase 4)");
    }
    catch
    {
        builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqliteCs));
        Console.WriteLine("MySQL unavailable - fell back to SQLite offline");
        provider = "Sqlite";
    }
}
else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(sqlServerCs))
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlServerCs));
    Console.WriteLine("Database provider: SQL Server");
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(sqliteCs));
    Console.WriteLine("Database provider: SQLite (offline / local demo)");
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SIDSRH-Full-Merged-Secret-Key-Change-In-Production-2026!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SIDSRH",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SIDSRH-Clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
        DbSeed.EnsureSeed(db);
        Console.WriteLine($"Database ready. Users: {db.Users.Count()} | Mode: {provider}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database init error: " + ex.Message);
        Console.WriteLine(ex.InnerException?.Message);
    }
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

Console.WriteLine("SIDSRH FULL MERGED — http://localhost:5080");
app.Run();
