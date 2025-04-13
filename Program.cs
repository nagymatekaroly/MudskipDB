using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Adatbázis
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// 🌐 CORS – weboldal sessionnel (cookie), Unity engedélyezve
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendAndUnity", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // weboldal origin
              .AllowCredentials()                  // sütik engedélyezése weboldalnak
              .AllowAnyHeader()
              .AllowAnyMethod();

        policy.SetIsOriginAllowed(origin =>
            origin == "http://localhost:5173" ||   // web frontend
            origin == "http://localhost" ||        // Unity Editor (biztonsági ráhagyás)
            string.IsNullOrEmpty(origin)           // Unity standalone build (origin nélkül)
        );
    });
});

// 🧠 Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 🚀 Swagger, Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔧 Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 🔐 Middleware sorrend
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FrontendAndUnity");
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.Run();
