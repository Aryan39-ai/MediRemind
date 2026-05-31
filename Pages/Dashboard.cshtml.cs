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

public class EndDateWarning
{
    public string MedicationName { get; set; } = "";
    public int DaysLeft { get; set; }
}

public class CaregiverPatient
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
}

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    public DashboardModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    public string UserName { get; set; } = "";
    public string Greeting { get; set; } = "day";
    public bool HasAnyMedications { get; set; }
    public List<TodayDose> TodayDoses { get; set; } = new();
    public int TodayTotal => TodayDoses.Count;
    public int TodayTaken => TodayDoses.Count(d => d.Status == "Taken");
    public int WeekAdherence { get; set; }
    public int Streak { get; set; }
    public List<EndDateWarning> EndDateWarnings { get; set; } = new();
    public List<CaregiverPatient> CaregiverPatients { get; set; } = new();

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

        var meds = await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive
                && m.StartDate <= today
                && (m.EndDate == null || m.EndDate >= today))
            .ToListAsync();

        HasAnyMedications = meds.Any();

        // End date warnings (within 7 days)
        var allMeds = await _db.Medications
            .Where(m => m.UserId == uid && m.IsActive && m.EndDate != null && m.EndDate >= today && m.EndDate <= today.AddDays(7))
            .ToListAsync();
        EndDateWarnings = allMeds.Select(m => new EndDateWarning
        {
            MedicationName = m.Name,
            DaysLeft = (m.EndDate!.Value - today).Days
        }).ToList();

        // Generate today's dose log entries if not already there
        foreach (var med in meds)
        {
            var times = med.TimesOfDay.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => TimeSpan.TryParse(s, out _))
                .Select(TimeSpan.Parse);

            foreach (var t in times)
            {
                var scheduledFor = today.Add(t);
                var existing = _db.DoseLogs.FirstOrDefault(d =>
                    d.MedicationId == med.Id && d.ScheduledFor == scheduledFor);
                if (existing == null)
                    _db.DoseLogs.Add(new DoseLog { MedicationId = med.Id, ScheduledFor = scheduledFor, Status = "Pending" });
            }
        }
        await _db.SaveChangesAsync();

        // Today's doses
        var logs = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid
                && d.ScheduledFor >= today
                && d.ScheduledFor < today.AddDays(1))
            .OrderBy(d => d.ScheduledFor)
            .ToListAsync();

        TodayDoses = logs.Select(l => new TodayDose
        {
            LogId        = l.Id,
            MedicationName = l.Medication.Name,
            Dosage       = l.Medication.Dosage,
            Instructions = l.Medication.Instructions,
            ScheduledFor = l.ScheduledFor,
            Status       = l.Status,
            TakenAt      = l.TakenAt
        }).ToList();

        // 7-day adherence
        var weekStart = today.AddDays(-7);
        var weekLogs = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == uid
                && d.ScheduledFor >= weekStart
                && d.ScheduledFor < today
                && d.ScheduledFor < DateTime.Now)
            .ToListAsync();

        WeekAdherence = weekLogs.Count == 0
            ? 100
            : (int)Math.Round(weekLogs.Count(l => l.Status == "Taken") * 100.0 / weekLogs.Count);

        // Streak — consecutive days back from yesterday where all doses were taken
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
            if (dayLogs.All(l => l.Status == "Taken"))
                Streak++;
            else
                break;

            checkDate = checkDate.AddDays(-1);
        }

        // Caregiver — find patients this user is carer for
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? "";
        if (!string.IsNullOrEmpty(userEmail))
        {
            var caregiverLinks = await _db.Caregivers
                .Where(c => c.CaregiverEmail == userEmail)
                .ToListAsync();

            foreach (var link in caregiverLinks)
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

        var log = await _db.DoseLogs
            .Include(d => d.Medication)
            .FirstOrDefaultAsync(d => d.Id == logId && d.Medication.UserId == uid);

        if (log != null)
        {
            log.Status = "Taken";
            log.TakenAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(notes))
                log.Notes = notes.Trim();
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSkipAsync(int logId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        var log = await _db.DoseLogs
            .Include(d => d.Medication)
            .FirstOrDefaultAsync(d => d.Id == logId && d.Medication.UserId == uid);

        if (log != null)
        {
            log.Status = "Skipped";
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
