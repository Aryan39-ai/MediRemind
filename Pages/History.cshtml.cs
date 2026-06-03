using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MediRemind.Data;

namespace MediRemind.Pages;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    public HistoryModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    public List<DoseLog>    Logs            { get; set; } = new();
    public List<Medication> UserMedications { get; set; } = new();
    public int TotalScheduled   { get; set; }
    public int TotalTaken       { get; set; }
    public int OverallAdherence { get; set; }

    // Chart data (JSON arrays for Chart.js)
    public string ChartLabels { get; set; } = "[]";
    public string ChartData   { get; set; } = "[]";

    // Filter/sort bound from GET query string
    [BindProperty(SupportsGet = true)] public int?      MedId     { get; set; }
    [BindProperty(SupportsGet = true)] public string    Status    { get; set; } = "All";
    [BindProperty(SupportsGet = true)] public DateTime? DateFrom  { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? DateTo    { get; set; }
    [BindProperty(SupportsGet = true)] public string    SortOrder { get; set; } = "desc";

    public async Task OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }
        IsLoggedIn = true;
        int uid = int.Parse(userId);

        var monthStart = DateTime.Today.AddDays(-30);

        // Medication dropdown
        UserMedications = await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync();

        // Base query
        var query = _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid && d.ScheduledFor >= monthStart);

        if (MedId.HasValue)
            query = query.Where(d => d.MedicationId == MedId.Value);

        if (DateFrom.HasValue)
            query = query.Where(d => d.ScheduledFor >= DateFrom.Value);

        if (DateTo.HasValue)
            query = query.Where(d => d.ScheduledFor < DateTo.Value.AddDays(1));

        query = SortOrder == "asc"
            ? query.OrderBy(d => d.ScheduledFor)
            : query.OrderByDescending(d => d.ScheduledFor);

        var allLogs = await query.ToListAsync();

        // Status filter (in memory — "Missed" is derived)
        Logs = Status switch
        {
            "Taken"   => allLogs.Where(l => l.Status == "Taken").ToList(),
            "Missed"  => allLogs.Where(l => l.Status != "Taken" && l.Status != "Skipped" && l.ScheduledFor < DateTime.Now.AddHours(-1)).ToList(),
            "Skipped" => allLogs.Where(l => l.Status == "Skipped").ToList(),
            _         => allLogs
        };

        // Summary stats (unfiltered, past only)
        var pastLogs    = allLogs.Where(l => l.ScheduledFor < DateTime.Now).ToList();
        TotalScheduled  = pastLogs.Count;
        TotalTaken      = pastLogs.Count(l => l.Status == "Taken");
        OverallAdherence = TotalScheduled == 0 ? 100
            : (int)Math.Round(TotalTaken * 100.0 / TotalScheduled);

        // Chart — daily adherence % (skip days with no settled logs)
        var chartLogs = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid
                && d.ScheduledFor >= monthStart
                && d.ScheduledFor < DateTime.Today)
            .ToListAsync();

        var dailyPoints = chartLogs
            .GroupBy(l => l.ScheduledFor.Date)
            .Select(g => new { Date = g.Key, Settled = g.Where(l => l.Status != "Pending").ToList() })
            .Where(g => g.Settled.Any())
            .OrderBy(g => g.Date)
            .ToList();

        ChartLabels = JsonSerializer.Serialize(dailyPoints.Select(d => d.Date.ToString("dd MMM")));
        ChartData   = JsonSerializer.Serialize(dailyPoints.Select(d =>
            (int)Math.Round(d.Settled.Count(l => l.Status == "Taken") * 100.0 / d.Settled.Count)));
    }
}
