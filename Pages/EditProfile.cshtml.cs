using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class EditProfileModel : PageModel
{
    private readonly AppDbContext _db;
    public EditProfileModel(AppDbContext db) => _db = db;

    [BindProperty] public ProfileInput Input { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return;
        var user = _db.Users.Find(int.Parse(userId));
        if (user != null)
            Input = new ProfileInput { Name = user.Name, Email = user.Email };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToPage("/Login");
        int uid = int.Parse(userId);
        var user = await _db.Users.FindAsync(uid);
        if (user == null) return RedirectToPage("/Login");

        if (!ModelState.IsValid) { ErrorMessage = "Please correct the errors above."; return Page(); }

        // Check email uniqueness (excluding self)
        if (_db.Users.Any(u => u.Email == Input.Email.ToLower().Trim() && u.Id != uid))
        {
            ErrorMessage = "That email address is already in use.";
            return Page();
        }

        // Password change requested
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(Input.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, user.PasswordHash))
            {
                ErrorMessage = "Current password is incorrect.";
                return Page();
            }
            if (Input.NewPassword != Input.ConfirmNewPassword)
            {
                ErrorMessage = "New passwords do not match.";
                return Page();
            }
            if (Input.NewPassword.Length < 8)
            {
                ErrorMessage = "New password must be at least 8 characters.";
                return Page();
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
        }

        user.Name  = Input.Name.Trim();
        user.Email = Input.Email.ToLower().Trim();
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserEmail", user.Email);

        SuccessMessage = "Profile updated successfully.";
        return Page();
    }
}

public class ProfileInput
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    public string? CurrentPassword { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string? NewPassword { get; set; }

    public string? ConfirmNewPassword { get; set; }
}
