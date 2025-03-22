using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using MudskipDB;
using MudskipDB.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Connection string beállítása az appsettings.json alapján
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 🔹 Adatbázis kapcsolat beállítása
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// 🔹 Session kezelése
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 MVC, Swagger és API támogatás
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔹 Swagger mindig fusson Renderen is
app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Middleware konfiguráció
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();  // 👉 EZ legyen a LEGUTOLSÓ sor
