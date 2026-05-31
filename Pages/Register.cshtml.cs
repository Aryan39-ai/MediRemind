using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    public RegisterModel(AppDbContext db) => _db = db;

    [BindProperty] public RegisterInput Input { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { ErrorMessage = "Please correct the errors above."; return Page(); }
        if (Input.Password != Input.ConfirmPassword) { ErrorMessage = "Passwords do not match."; return Page(); }
        if (_db.Users.Any(u => u.Email == Input.Email.ToLower().Trim())) { ErrorMessage = "An account with that email already exists."; return Page(); }

        var user = new User
        {
            Name = Input.Name.Trim(),
            Email = Input.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserEmail", user.Email);
        return RedirectToPage("/Dashboard");
    }
}

public class RegisterInput
{
    [Required(ErrorMessage = "Name is required.")] [StringLength(100, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Email is required.")] [EmailAddress(ErrorMessage = "Enter a valid email.")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Password is required.")] [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] public string Password { get; set; } = "";
    [Required(ErrorMessage = "Please confirm your password.")] public string ConfirmPassword { get; set; } = "";
}
