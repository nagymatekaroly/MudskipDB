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

////await using (var scope = app.Services.CreateAsyncScope())
//{
  //  var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //await dbContext.Database.MigrateAsync();
    //Console.WriteLine("SEED INDUL!!!");
    //await Seeder.SeedData(dbContext);
    //Console.WriteLine("SEED LEFUTOTT!!!");
//}

// 🔹 Middleware konfiguráció
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();  // 👉 EZ legyen a LEGUTOLSÓ sor
