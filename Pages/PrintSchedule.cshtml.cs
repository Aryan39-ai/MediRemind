using Microsoft.AspNetCore.Mvc.RazorPages;
using MediRemind.Data;

namespace MediRemind.Pages;

public class PrintScheduleModel : PageModel
{
    private readonly AppDbContext _db;
    public PrintScheduleModel(AppDbContext db) => _db = db;

    public bool IsLoggedIn { get; set; }
    public string UserName { get; set; } = "";
    public string PrintDate { get; set; } = "";
    public List<Medication> Medications { get; set; } = new();

    public void OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) { IsLoggedIn = false; return; }

        IsLoggedIn = true;
        int uid = int.Parse(userId);
        UserName  = HttpContext.Session.GetString("UserName") ?? "Patient";
        PrintDate = DateTime.Now.ToString("dd MMMM yyyy, h:mm tt");

        Medications = _db.Medications
            .Where(m => m.UserId == uid && m.IsActive)
            .OrderBy(m => m.Name)
            .ToList();
    }
}
