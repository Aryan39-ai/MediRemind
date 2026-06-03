using Microsoft.EntityFrameworkCore;
using MediRemind.Data;
using WebPush;

namespace MediRemind.Services;

public class PushNotificationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IConfiguration _config;

    public PushNotificationService(IServiceProvider services,
        ILogger<PushNotificationService> logger, IConfiguration config)
    {
        _services = services;
        _logger   = logger;
        _config   = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SendUpcomingDoseNotificationsAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Push notification error"); }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task SendUpcomingDoseNotificationsAsync()
    {
        var vapidPublic  = _config["Vapid:PublicKey"]  ?? "";
        var vapidPrivate = _config["Vapid:PrivateKey"] ?? "";
        var vapidSubject = _config["Vapid:Subject"]    ?? "mailto:admin@mediremind.app";

        if (string.IsNullOrEmpty(vapidPublic) || string.IsNullOrEmpty(vapidPrivate)) return;

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var window     = DateTime.Now.AddMinutes(5);
        var windowStart = DateTime.Now;

        // Find doses due in the next 5 minutes that haven't been notified yet
        var upcoming = await db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Status == "Pending"
                && d.ScheduledFor >= windowStart
                && d.ScheduledFor <= window)
            .ToListAsync();

        var client = new WebPushClient();
        client.SetVapidDetails(vapidSubject, vapidPublic, vapidPrivate);

        foreach (var dose in upcoming)
        {
            var subscriptions = db.PushSubscriptions
                .Where(s => s.UserId == dose.Medication.UserId)
                .ToList();

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                title = "Dose reminder",
                body  = $"{dose.Medication.Name} {dose.Medication.Dosage} is due at {dose.ScheduledFor:HH:mm}",
                url   = "/Dashboard"
            });

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    await client.SendNotificationAsync(pushSub, payload);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to push to subscription {id}: {msg}", sub.Id, ex.Message);
                }
            }
        }
    }
}
