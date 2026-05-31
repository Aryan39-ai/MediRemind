using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MediRemind.Data;

namespace MediRemind.Pages;

public class MedicationsModel : PageModel
{
    private readonly AppDbContext _db;
    public MedicationsModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    public List<Medication> Medications { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }
        IsLoggedIn = true;
        int uid = int.Parse(userId);
        Medications = _db.Medications
            .Where(m => m.UserId == uid && m.IsActive)
            .OrderBy(m => m.Name)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);
        var med = _db.Medications.FirstOrDefault(m => m.Id == id && m.UserId == uid);
        if (med != null) { _db.Medications.Remove(med); await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
