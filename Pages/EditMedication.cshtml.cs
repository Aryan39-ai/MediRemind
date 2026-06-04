using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

// Shared input DTO — also referenced by AddMedication wizard's final save
public class MedicationInput
{
    [Required(ErrorMessage = "Medication name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Dosage is required.")]
    [StringLength(50)]
    public string Dosage { get; set; } = "";

    [Required]
    public string Frequency { get; set; } = "Once daily";

    [StringLength(200)]
    public string? TimesOfDay { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Range(0, 9999)]
    public int? PillCount { get; set; }

    [Range(1, 99)]
    public int? PillsPerDose { get; set; }
}

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
