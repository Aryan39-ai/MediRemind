using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MediRemind.Data;

namespace MediRemind.Pages;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    public HistoryModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    public List<DoseLog> Logs { get; set; } = new();
    public string Filter { get; set; } = "All";
    public int TotalScheduled { get; set; }
    public int TotalTaken { get; set; }
    public int OverallAdherence { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }
        IsLoggedIn = true;
        int uid = int.Parse(userId);
        Filter = filter ?? "All";

        var monthStart = DateTime.Today.AddDays(-30);

        var query = _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid && d.ScheduledFor >= monthStart);

        var allLogs = await query.OrderByDescending(d => d.ScheduledFor).ToListAsync();

        // Compute overall stats (on past logs only)
        var pastLogs = allLogs.Where(l => l.ScheduledFor < DateTime.Now).ToList();
        TotalScheduled = pastLogs.Count;
        TotalTaken = pastLogs.Count(l => l.Status == "Taken");
        OverallAdherence = TotalScheduled == 0 ? 100 : (int)Math.Round(TotalTaken * 100.0 / TotalScheduled);

        // Apply filter
        Logs = Filter switch
        {
            "Taken"  => allLogs.Where(l => l.Status == "Taken").ToList(),
            "Missed" => allLogs.Where(l => l.Status != "Taken" && l.ScheduledFor < DateTime.Now.AddHours(-1)).ToList(),
            _        => allLogs
        };
    }
}
