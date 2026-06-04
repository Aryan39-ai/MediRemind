using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MediRemind.Data;

namespace MediRemind.Pages;

public class TodayDose
{
    public int LogId { get; set; }
    public string MedicationName { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Instructions { get; set; } = "";
    public DateTime ScheduledFor { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? TakenAt { get; set; }
    public bool IsPast => DateTime.Now > ScheduledFor.AddMinutes(60) && Status == "Pending";
}

public class EndDateWarning   { public string MedicationName { get; set; } = ""; public int DaysLeft { get; set; } }
public class LowSupplyWarning { public string MedicationName { get; set; } = ""; public int PillCount  { get; set; } }
public class CaregiverPatient { public int UserId { get; set; }  public string UserName { get; set; } = ""; }

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public DashboardModel(AppDbContext db) => _db = db;

    // Change this constant to adjust the low-supply threshold
    private const int LowSupplyThreshold = 14;

    public bool IsLoggedIn { get; set; }
    public string UserName { get; set; } = "";
    public string Greeting  { get; set; } = "day";
    public bool HasAnyMedications { get; set; }
    public List<TodayDose>      TodayDoses       { get; set; } = new();
    public List<EndDateWarning>  EndDateWarnings  { get; set; } = new();
    public List<LowSupplyWarning> LowSupplyWarnings { get; set; } = new();
    public List<CaregiverPatient> CaregiverPatients { get; set; } = new();

    public int TodayTotal  => TodayDoses.Count;
    public int TodayTaken  => TodayDoses.Count(d => d.Status == "Taken" || d.Status == "PendingConfirm");
    public int WeekAdherence { get; set; }
    public int Streak        { get; set; }

    // ── Next dose countdown banner ────────────────────────────────────────────
    public string BannerType    { get; set; } = "";  // "upcoming"|"soon"|"overdue"|"allgood"
    public string BannerMedName { get; set; } = "";
    public string BannerTime    { get; set; } = "";
    public string AlreadyTakenId { get; set; } = "";

    public async Task OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }

        IsLoggedIn = true;
        int uid = int.Parse(userId);
        UserName = HttpContext.Session.GetString("UserName") ?? "there";

        var hour = DateTime.Now.Hour;
        Greeting = hour < 12 ? "morning" : hour < 18 ? "afternoon" : "evening";

        var today = DateTime.Today;

        // ── Auto-promote expired PendingConfirm → Taken ──────────────────────
        var expired = await _db.DoseLogs
            .Where(d => d.Status == "PendingConfirm" && d.ConfirmedAt != null && d.ConfirmedAt <= DateTime.Now)
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid)
            .ToListAsync();
        foreach (var e in expired)
            e.Status = "Taken";
        if (expired.Any()) await _db.SaveChangesAsync();

        var meds = await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive
                && m.StartDate <= today
                && (m.EndDate == null || m.EndDate >= today))
            .ToListAsync();

        HasAnyMedications = meds.Any();

        // End-date warnings (within 7 days)
        EndDateWarnings = (await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive
                && m.EndDate != null && m.EndDate >= today && m.EndDate <= today.AddDays(7))
            .ToListAsync())
            .Select(m => new EndDateWarning
            {
                MedicationName = m.Name,
                DaysLeft       = (m.EndDate!.Value - today).Days
            }).ToList();

        // Low-supply warnings
        LowSupplyWarnings = (await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive
                && m.PillCount != null && m.PillCount <= LowSupplyThreshold)
            .ToListAsync())
            .Select(m => new LowSupplyWarning
            {
                MedicationName = m.Name,
                PillCount      = m.PillCount!.Value
            }).ToList();

        // Generate today's dose log entries if not already there
        foreach (var med in meds)
        {
            var times = med.TimesOfDay
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => TimeSpan.TryParse(s, out _))
                .Select(TimeSpan.Parse);

            foreach (var t in times)
            {
                var scheduledFor = today.Add(t);
                if (!_db.DoseLogs.Any(d => d.MedicationId == med.Id && d.ScheduledFor == scheduledFor))
                    _db.DoseLogs.Add(new DoseLog { MedicationId = med.Id, ScheduledFor = scheduledFor, Status = "Pending" });
            }
        }
        await _db.SaveChangesAsync();

        // Today's doses
        TodayDoses = (await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid
                && d.ScheduledFor >= today
                && d.ScheduledFor < today.AddDays(1))
            .OrderBy(d => d.ScheduledFor)
            .ToListAsync())
            .Select(l => new TodayDose
            {
                LogId          = l.Id,
                MedicationName = l.Medication.Name,
                Dosage         = l.Medication.Dosage,
                Instructions   = l.Medication.Instructions,
                ScheduledFor   = l.ScheduledFor,
                Status         = l.Status,
                TakenAt        = l.TakenAt
            }).ToList();

        SetBanner();
        AlreadyTakenId = TempData.Peek("AlreadyTakenId")?.ToString() ?? "";

        // 7-day adherence — Taken / (Taken + Missed + Skipped), excludes still-Pending
        var weekStart = today.AddDays(-7);
        var weekLogs  = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid
                && d.ScheduledFor >= weekStart
                && d.ScheduledFor < today)
            .ToListAsync();

        var settled = weekLogs.Where(l => l.Status != "Pending").ToList();
        WeekAdherence = settled.Count == 0
            ? 100
            : (int)Math.Round(settled.Count(l => l.Status == "Taken") * 100.0 / settled.Count);

        // Streak — consecutive days (back from yesterday) where at least one dose was Taken
        Streak = 0;
        var checkDate = today.AddDays(-1);
        while (checkDate >= today.AddDays(-365))
        {
            var dayLogs = await _db.DoseLogs
                .Include(d => d.Medication)
                .Where(d => d.Medication.UserId == uid
                    && d.ScheduledFor >= checkDate
                    && d.ScheduledFor < checkDate.AddDays(1))
                .ToListAsync();

            if (!dayLogs.Any()) break;
            if (dayLogs.Any(l => l.Status == "Taken")) Streak++;
            else break;

            checkDate = checkDate.AddDays(-1);
        }

        // Caregiver — patients this user cares for
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? "";
        if (!string.IsNullOrEmpty(userEmail))
        {
            foreach (var link in await _db.Caregivers.Where(c => c.CaregiverEmail == userEmail).ToListAsync())
            {
                var patient = await _db.Users.FindAsync(link.PatientUserId);
                if (patient != null)
                    CaregiverPatients.Add(new CaregiverPatient { UserId = patient.Id, UserName = patient.Name });
            }
        }
    }

    public async Task<IActionResult> OnPostMarkTakenAsync(int logId, string? notes)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        var log = await _db.DoseLogs.Include(d => d.Medication)
            .FirstOrDefaultAsync(d => d.Id == logId && d.Medication.UserId == uid);

        if (log != null)
        {
            if (log.Status == "Taken" || log.Status == "PendingConfirm")
            {
                TempData["AlreadyTakenId"] = logId.ToString();
            }
            else
            {
                log.Status      = "PendingConfirm";
                log.TakenAt     = DateTime.Now;
                log.ConfirmedAt = DateTime.Now.AddSeconds(10);
                if (!string.IsNullOrWhiteSpace(notes)) log.Notes = notes.Trim();
                await _db.SaveChangesAsync();
                TempData["UndoDoseId"]   = log.Id.ToString();
                TempData["UndoDoseName"] = log.Medication.Name;
            }
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUndoAsync(int doseLogId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        var log = await _db.DoseLogs.Include(d => d.Medication)
            .FirstOrDefaultAsync(d => d.Id == doseLogId && d.Medication.UserId == uid);

        if (log != null && log.Status == "PendingConfirm")
        {
            log.Status      = "Pending";
            log.TakenAt     = null;
            log.ConfirmedAt = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{log.Medication.Name} has been undone.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSkipAsync(int logId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        var log = await _db.DoseLogs.Include(d => d.Medication)
            .FirstOrDefaultAsync(d => d.Id == logId && d.Medication.UserId == uid);

        if (log != null) { log.Status = "Skipped"; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    // ── Banner helpers ───────────────────────────────────────────────────────
    private void SetBanner()
    {
        var now     = DateTime.Now;
        var pending = TodayDoses.Where(d => d.Status == "Pending").ToList();

        if (!pending.Any())
        {
            if (TodayDoses.Any(d => d.Status == "Taken")) BannerType = "allgood";
            return;
        }

        var next = pending.OrderBy(d => d.ScheduledFor).First();
        BannerMedName = next.MedicationName;
        var diff = next.ScheduledFor - now;

        if (diff.TotalSeconds < -60)
        {
            BannerType = "overdue";
            BannerTime = FormatSpan(-diff) + " overdue";
        }
        else if (diff.TotalMinutes < 60)
        {
            BannerType = "soon";
            BannerTime = diff.TotalSeconds < 60 ? "right now" : "in " + FormatSpan(diff);
        }
        else
        {
            BannerType = "upcoming";
            BannerTime = "in " + FormatSpan(diff);
        }
    }

    private static string FormatSpan(TimeSpan ts)
    {
        var h = (int)ts.TotalHours;
        var m = ts.Minutes;
        if (h == 0) return $"{m} minute{(m == 1 ? "" : "s")}";
        if (m == 0) return $"{h} hour{(h == 1 ? "" : "s")}";
        return $"{h} hour{(h == 1 ? "" : "s")} {m} minute{(m == 1 ? "" : "s")}";
    }
}
