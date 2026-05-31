using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class CaregiversModel : PageModel
{
    private readonly AppDbContext _db;
    public CaregiversModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    [BindProperty] public CaregiverInput Input { get; set; } = new();
    public List<Caregiver> Caregivers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }
        IsLoggedIn = true;
        int uid = int.Parse(userId);
        Caregivers = _db.Caregivers.Where(c => c.PatientUserId == uid).OrderByDescending(c => c.AddedAt).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please enter a valid email address.";
            OnGet();
            return Page();
        }

        var email = Input.Email.Trim().ToLower();
        if (_db.Caregivers.Any(c => c.PatientUserId == uid && c.CaregiverEmail == email))
        {
            ErrorMessage = "That person already has access.";
            OnGet();
            return Page();
        }

        _db.Caregivers.Add(new Caregiver { PatientUserId = uid, CaregiverEmail = email, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        SuccessMessage = $"{email} has been given access.";
        Input = new CaregiverInput();
        ModelState.Clear();
        OnGet();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);
        var c = _db.Caregivers.FirstOrDefault(x => x.Id == id && x.PatientUserId == uid);
        if (c != null) { _db.Caregivers.Remove(c); await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}

public class CaregiverInput
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = "";
}
