using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediRemind.Data;

namespace MediRemind.Pages;

public class EditMedicationModel : PageModel
{
    private readonly AppDbContext _db;
    public EditMedicationModel(AppDbContext db) => _db = db;

    public Medication? Medication { get; set; }
    [BindProperty] public MedicationInput Input { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public void OnGet(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return;
        int uid = int.Parse(userId);
        Medication = _db.Medications.FirstOrDefault(m => m.Id == id && m.UserId == uid);

        if (Medication != null)
        {
            Input = new MedicationInput
            {
                Name         = Medication.Name,
                Dosage       = Medication.Dosage,
                Frequency    = Medication.Frequency,
                TimesOfDay   = Medication.TimesOfDay,
                Instructions = Medication.Instructions,
                StartDate    = Medication.StartDate,
                EndDate      = Medication.EndDate,
                PillCount    = Medication.PillCount,
                PillsPerDose = Medication.PillsPerDose
            };
        }
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        // Ownership check
        Medication = _db.Medications.FirstOrDefault(m => m.Id == id && m.UserId == uid);
        if (Medication == null) return RedirectToPage("/Medications");

        if (!ModelState.IsValid) { ErrorMessage = "Please correct the errors above."; return Page(); }

        Medication.Name = Input.Name.Trim();
        Medication.Dosage = Input.Dosage.Trim();
        Medication.Frequency = Input.Frequency;
        Medication.TimesOfDay   = Input.TimesOfDay?.Trim() ?? "";
        Medication.Instructions = Input.Instructions?.Trim() ?? "";
        Medication.PillCount    = Input.PillCount;
        Medication.PillsPerDose = Input.PillsPerDose;
        await _db.SaveChangesAsync();

        return RedirectToPage("/Medications");
    }
}
