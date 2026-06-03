using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediRemind.Data;

namespace MediRemind.Pages;

[Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
public class SubscribeModel : PageModel
{
    private readonly AppDbContext _db;
    public SubscribeModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnPostAsync([FromBody] PushSubscriptionPayload payload)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return Unauthorized();
        int uid = int.Parse(userId);

        // Remove old subscriptions for this user
        var old = _db.PushSubscriptions.Where(s => s.UserId == uid).ToList();
        _db.PushSubscriptions.RemoveRange(old);

        _db.PushSubscriptions.Add(new PushSubscription
        {
            UserId   = uid,
            Endpoint = payload.Endpoint,
            P256dh   = payload.Keys.P256dh,
            Auth     = payload.Keys.Auth
        });
        await _db.SaveChangesAsync();
        return new OkResult();
    }
}

public class PushSubscriptionPayload
{
    public string Endpoint { get; set; } = "";
    public PushKeys Keys   { get; set; } = new();
}
public class PushKeys
{
    public string P256dh { get; set; } = "";
    public string Auth   { get; set; } = "";
}
