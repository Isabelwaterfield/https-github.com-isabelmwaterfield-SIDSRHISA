using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIDSRH.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var sqlServerCs = builder.Configuration["Database:SqlServer"];
var sqliteCs = builder.Configuration["Database:Sqlite"] ?? "Data Source=HealthcareDB.db";

if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlServerCs));
    Console.WriteLine("Database provider: SQL Server");
}
else
{
    // Pin SQLite file under content root so Backup and runtime share one file
    var dbFile = Path.Combine(builder.Environment.ContentRootPath, "HealthcareDB.db");
    var cs = $"Data Source={dbFile}";
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));
    Console.WriteLine("Database provider: SQLite -> " + dbFile);
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SIDSRH-SuperSecret-Key-Change-In-Production-2026!";
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
        Console.WriteLine("Database ready. Users: " + db.Users.Count());
    }
    catch (Exception ex)
    {
        Console.WriteLine("Database init error: " + ex.Message);
        Console.WriteLine("Tip: set Database:Provider to Sqlite in appsettings.json if SQL Server/LocalDB is unavailable.");
    }
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

