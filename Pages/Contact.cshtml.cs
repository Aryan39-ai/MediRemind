using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using MediRemind.Data;

namespace MediRemind.Pages;

public class ContactModel : PageModel
{
    private readonly AppDbContext _db;
    public ContactModel(AppDbContext db) => _db = db;
    [BindProperty] public ContactInput Input { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _db.ContactMessages.Add(new ContactMessage
        {
            Name = Input.Name.Trim(), Email = Input.Email.Trim().ToLower(),
            Message = Input.Message.Trim(), SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        ModelState.Clear();
        Input = new ContactInput();
        SuccessMessage = "Thanks for your message — we'll be in touch soon.";
        return Page();
    }
}

public class ContactInput
{
    [Required(ErrorMessage = "Name is required.")] [StringLength(100, MinimumLength = 2)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Email is required.")] [EmailAddress] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Message is required.")] [StringLength(2000, MinimumLength = 10)] public string Message { get; set; } = "";
}
