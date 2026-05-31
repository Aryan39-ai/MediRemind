using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MediRemind.Data;

namespace MediRemind.Pages;

public class CaregiverDose
{
    public string MedicationName { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Instructions { get; set; } = "";
    public DateTime ScheduledFor { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? TakenAt { get; set; }
    public bool IsPast => DateTime.Now > ScheduledFor.AddMinutes(60) && Status == "Pending";
}

public class CaregiverViewModel : PageModel
{
    private readonly AppDbContext _db;
    public CaregiverViewModel(AppDbContext db) => _db = db;

    public bool IsAuthorised { get; set; }
    public string PatientName { get; set; } = "";
    public List<CaregiverDose> TodayDoses { get; set; } = new();
    public int TodayTotal => TodayDoses.Count;
    public int TodayTaken => TodayDoses.Count(d => d.Status == "Taken");
    public int WeekAdherence { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrEmpty(userEmail)) return RedirectToPage("/Login");

        // Verify this user is actually a caregiver for patient {id}
        var link = await _db.Caregivers
            .FirstOrDefaultAsync(c => c.PatientUserId == id && c.CaregiverEmail == userEmail);

        if (link == null)
        {
            IsAuthorised = false;
            return Page();
        }

        IsAuthorised = true;
        var patient = await _db.Users.FindAsync(id);
        if (patient == null) return RedirectToPage("/Dashboard");
        PatientName = patient.Name;

        var today = DateTime.Today;

        // Pull today's dose logs for the patient (read-only, no generation)
        var logs = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == id
                && d.ScheduledFor >= today
                && d.ScheduledFor < today.AddDays(1))
            .OrderBy(d => d.ScheduledFor)
            .ToListAsync();

        TodayDoses = logs.Select(l => new CaregiverDose
        {
            MedicationName = l.Medication.Name,
            Dosage         = l.Medication.Dosage,
            Instructions   = l.Medication.Instructions,
            ScheduledFor   = l.ScheduledFor,
            Status         = l.Status,
            TakenAt        = l.TakenAt
        }).ToList();

        // 7-day adherence
        var weekStart = today.AddDays(-7);
        var weekLogs = await _db.DoseLogs
            .Include(d => d.Medication)
            .Where(d => d.Medication.UserId == id
                && d.ScheduledFor >= weekStart
                && d.ScheduledFor < today)
            .ToListAsync();

        WeekAdherence = weekLogs.Count == 0
            ? 100
            : (int)Math.Round(weekLogs.Count(l => l.Status == "Taken") * 100.0 / weekLogs.Count);

        return Page();
    }
}
