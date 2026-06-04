using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using MediRemind.Data;

namespace MediRemind.Pages;

// Persisted across wizard steps via TempData (serialised as JSON)
public class WizardState
{
    public string  Name         { get; set; } = "";
    public string  Dosage       { get; set; } = "";
    public string? Instructions { get; set; }
    public string  Frequency    { get; set; } = "Once daily";
    public string? TimesOfDay   { get; set; }
    public int?    PillCount    { get; set; }
    public int?    PillsPerDose { get; set; }
}

public class AddMedicationModel : PageModel
{
    private readonly AppDbContext _db;
    public AddMedicationModel(AppDbContext db) => _db = db;

    public int     CurrentStep  { get; set; } = 1;
    public string? ErrorMessage { get; set; }
    public WizardState Wizard   { get; set; } = new();

    // Step 1
    [BindProperty] public string  S_Name         { get; set; } = "";
    [BindProperty] public string  S_Dosage        { get; set; } = "";
    [BindProperty] public string? S_Instructions  { get; set; }

    // Step 2
    [BindProperty] public string? S_Frequency { get; set; }
    [BindProperty] public string? S_Time1     { get; set; }
    [BindProperty] public string? S_Time2     { get; set; }
    [BindProperty] public string? S_Time3     { get; set; }

    // Step 3
    [BindProperty] public int? S_PillCount    { get; set; }
    [BindProperty] public int? S_PillsPerDose { get; set; }

    public void OnGet(int step = 1)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return;

        CurrentStep = step;
        Wizard = PeekWizard();

        // Pre-fill bound properties so the view renders correct values on back-navigation
        S_Name         = Wizard.Name;
        S_Dosage       = Wizard.Dosage;
        S_Instructions = Wizard.Instructions;
        S_Frequency    = Wizard.Frequency;
        S_PillCount    = Wizard.PillCount;
        S_PillsPerDose = Wizard.PillsPerDose;

        if (!string.IsNullOrEmpty(Wizard.TimesOfDay))
        {
            var parts = Wizard.TimesOfDay
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            S_Time1 = parts.ElementAtOrDefault(0);
            S_Time2 = parts.ElementAtOrDefault(1);
            S_Time3 = parts.ElementAtOrDefault(2);
        }
    }

    // ── Step 1 ──────────────────────────────────────────────────────────────────
    public IActionResult OnPostStep1()
    {
        var w = PopWizard();

        if (string.IsNullOrWhiteSpace(S_Name))
        {
            ErrorMessage = "Please enter the medication name.";
            return ReturnStep(1, w);
        }
        if (string.IsNullOrWhiteSpace(S_Dosage))
        {
            ErrorMessage = "Please enter the dosage (e.g. 500 mg, 1 tablet).";
            return ReturnStep(1, w);
        }

        w.Name         = S_Name.Trim();
        w.Dosage       = S_Dosage.Trim();
        w.Instructions = S_Instructions?.Trim();
        PushWizard(w);
        return RedirectToPage(new { step = 2 });
    }

    // ── Step 2 ──────────────────────────────────────────────────────────────────
    public IActionResult OnPostStep2()
    {
        var w = PopWizard();
        w.Frequency  = S_Frequency ?? "Once daily";
        w.TimesOfDay = BuildTimes(S_Time1, S_Time2, S_Time3);
        PushWizard(w);
        return RedirectToPage(new { step = 3 });
    }

    // ── Back (any step) ─────────────────────────────────────────────────────────
    public IActionResult OnPostBack(int toStep)
    {
        var w = PopWizard();
        // Preserve whichever step's data was just visible in the form
        if (!string.IsNullOrWhiteSpace(S_Name))      w.Name         = S_Name.Trim();
        if (!string.IsNullOrWhiteSpace(S_Dosage))    w.Dosage       = S_Dosage.Trim();
        if (S_Instructions != null)                   w.Instructions = S_Instructions.Trim();
        if (!string.IsNullOrWhiteSpace(S_Frequency)) w.Frequency    = S_Frequency!;
        var newTimes = BuildTimes(S_Time1, S_Time2, S_Time3);
        if (newTimes != null) w.TimesOfDay = newTimes;
        if (S_PillCount.HasValue)    w.PillCount    = S_PillCount;
        if (S_PillsPerDose.HasValue) w.PillsPerDose = S_PillsPerDose;
        PushWizard(w);
        return RedirectToPage(new { step = toStep });
    }

    // ── Final save ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostSaveAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        var w = PopWizard();
        if (S_PillCount.HasValue)    w.PillCount    = S_PillCount;
        if (S_PillsPerDose.HasValue) w.PillsPerDose = S_PillsPerDose;

        if (string.IsNullOrWhiteSpace(w.Name))
            return RedirectToPage(new { step = 1 });

        var med = new Medication
        {
            UserId       = uid,
            Name         = w.Name,
            Dosage       = w.Dosage,
            Frequency    = w.Frequency,
            TimesOfDay   = w.TimesOfDay ?? "",
            Instructions = w.Instructions ?? "",
            StartDate    = DateTime.Today,
            PillCount    = w.PillCount,
            PillsPerDose = w.PillsPerDose,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _db.Medications.Add(med);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{med.Name} added to your medications.";
        return RedirectToPage("/Medications");
    }

    // ── TempData helpers ────────────────────────────────────────────────────────

    // Peek: read without consuming — used in GET handlers so POST can still read it
    private WizardState PeekWizard()
    {
        var json = TempData.Peek("WizData") as string;
        return json is null ? new() : JsonSerializer.Deserialize<WizardState>(json) ?? new();
    }

    // Pop: read + consume — used in POST handlers before re-storing updated state
    private WizardState PopWizard()
    {
        var json = TempData["WizData"] as string;
        return json is null ? new() : JsonSerializer.Deserialize<WizardState>(json) ?? new();
    }

    private void PushWizard(WizardState w)
        => TempData["WizData"] = JsonSerializer.Serialize(w);

    private IActionResult ReturnStep(int step, WizardState w)
    {
        PushWizard(w);   // re-store so the error path doesn't lose prior steps
        CurrentStep = step;
        Wizard = w;
        return Page();
    }

    private static string? BuildTimes(string? t1, string? t2, string? t3)
    {
        var parts = new[] { t1, t2, t3 }
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .ToArray();
        return parts.Length == 0 ? null : string.Join(", ", parts);
    }
}
