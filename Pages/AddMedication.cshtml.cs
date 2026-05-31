using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class AddMedicationModel : PageModel
{
    private readonly AppDbContext _db;
    public AddMedicationModel(AppDbContext db) => _db = db;

    [BindProperty] public MedicationInput Input { get; set; } = new() { StartDate = DateTime.Today };
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");

        if (!ModelState.IsValid) { ErrorMessage = "Please correct the errors above."; return Page(); }

        // Validate end date if provided
        if (Input.EndDate.HasValue && Input.EndDate.Value < Input.StartDate)
        {
            ErrorMessage = "End date cannot be before start date.";
            return Page();
        }

        var med = new Medication
        {
            UserId = int.Parse(userId),
            Name = Input.Name.Trim(),
            Dosage = Input.Dosage.Trim(),
            Frequency = Input.Frequency,
            TimesOfDay = Input.TimesOfDay?.Trim() ?? "",
            Instructions = Input.Instructions?.Trim() ?? "",
            StartDate    = Input.StartDate,
            EndDate      = Input.EndDate,
            PillCount    = Input.PillCount,
            PillsPerDose = Input.PillsPerDose,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Medications.Add(med);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{med.Name} added to your medications.";
        return RedirectToPage("/Medications");
    }
}

public class MedicationInput
{
    [Required(ErrorMessage = "Medication name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Dosage is required.")]
    [StringLength(50)]
    public string Dosage { get; set; } = "";

    [Required(ErrorMessage = "Frequency is required.")]
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
