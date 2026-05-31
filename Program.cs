using Microsoft.EntityFrameworkCore;
using SiakadKeuanganAPI.Data;
using SiakadKeuanganAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database PostgreSQL ────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── HttpClient untuk API Mahasiswa Eksternal ───────────────────────────────
builder.Services.AddHttpClient("MahasiswaApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ─── Services ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IMahasiswaSyncService, MahasiswaSyncService>();
builder.Services.AddScoped<IKeuanganService, KeuanganService>();

// ─── Controllers + Swagger ──────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "SIAKAD Keuangan API",
        Version = "v1",
        Description = "API Keuangan SIAKAD - Sinkronisasi dengan API Mahasiswa"
    });
});

// ─── CORS untuk Flutter ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ─── Auto Migrate Database ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ─── Middleware ──────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SIAKAD Keuangan API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.MapControllers();

// Root endpoint
app.MapGet("/", () => new
{
    success = true,
    message = "SIAKAD Keuangan API is Running",
    version = "1.0.0",
    endpoints = new
    {
        swagger = "/swagger",
        mahasiswa = "/api/mahasiswa",
        keuangan_tagihan = "/api/keuangan/tagihan",
        keuangan_pembayaran = "/api/keuangan/pembayaran",
        keuangan_dashboard = "/api/keuangan/dashboard",
        sinkronisasi = "/api/sinkronisasi/mahasiswa"
    }
});

app.Run();