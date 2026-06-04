using Microsoft.EntityFrameworkCore;
using MediRemind.Data;
using MediRemind.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=mediremind.db"));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => {
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<PushNotificationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Medications ADD COLUMN PillCount INTEGER NULL"); }    catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Medications ADD COLUMN PillsPerDose INTEGER NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE DoseLogs ADD COLUMN ConfirmedAt TEXT NULL"); } catch { }
    try { db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS PushSubscriptions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Endpoint TEXT NOT NULL,
            P256dh TEXT NOT NULL,
            Auth TEXT NOT NULL
        )"); } catch { }
}

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.Run();
