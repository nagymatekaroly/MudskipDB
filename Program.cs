using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendAndUnity", policy =>
    {
        policy.WithOrigins("http://localhost:5173") 
              .AllowCredentials()                  
              .AllowAnyHeader()
              .AllowAnyMethod();

        policy.SetIsOriginAllowed(origin =>
            origin == "https://localhost:7137" ||
            origin == "http://localhost:7137" ||
            origin == "http://localhost:5173" ||
            origin == "https://mudskipthesliem.netlify.app" ||
            origin == "http://localhost" ||
            string.IsNullOrEmpty(origin)
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

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FrontendAndUnity");
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.Run();
