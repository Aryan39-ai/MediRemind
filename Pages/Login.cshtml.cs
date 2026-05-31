using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;
    public LoginModel(AppDbContext db) => _db = db;

    [BindProperty] public LoginInput Input { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) { ErrorMessage = "Please fill in all fields."; return Page(); }
        var user = _db.Users.FirstOrDefault(u => u.Email == Input.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(Input.Password, user.PasswordHash))
        {
            ErrorMessage = "Incorrect email or password.";
            return Page();
        }
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserEmail", user.Email);
        return RedirectToPage("/Dashboard");
    }
}

public class LoginInput
{
    [Required(ErrorMessage = "Email is required.")] [EmailAddress] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Password is required.")] public string Password { get; set; } = "";
}
