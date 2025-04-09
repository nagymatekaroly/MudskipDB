using Microsoft.AspNetCore.Session;
using Microsoft.EntityFrameworkCore;
using MudskipDB;
using MudskipDB.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Adatbázis
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// 🌐 CORS engedélyezés (bármilyen originről jöhet kérés)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 🧠 Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🚀 API és Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection(); // Ha van
app.UseRouting();

// 💡 FONTOS: CORS mindig a routing után jöjjön!
app.UseCors("AllowAll");

// 🔐 Session és authorization csak ezután jön!
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.Run();
