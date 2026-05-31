using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MediRemind.Pages;

public class Feature { public string Title = ""; public string Description = ""; public string SvgPath = ""; }

public class IndexModel : PageModel
{
    public List<Feature> Features = new()
    {
        new() {
            Title       = "Daily Schedule",
            Description = "See all your doses for today in one clear view, organised by time.",
            SvgPath     = "M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
        },
        new() {
            Title       = "Status Indicators",
            Description = "Each dose shows its status — green for taken, amber for due, red for missed.",
            SvgPath     = "M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
        },
        new() {
            Title       = "Adherence Tracking",
            Description = "See how consistently you are taking your medications over the past week.",
            SvgPath     = "M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
        },
        new() {
            Title       = "Caregiver Access",
            Description = "Share your schedule with a family member so they can help keep you on track.",
            SvgPath     = "M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"
        },
        new() {
            Title       = "Medication Notes",
            Description = "Add instructions like 'take with food' or 'avoid driving' to each medication.",
            SvgPath     = "M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
        },
        new() {
            Title       = "Private and Secure",
            Description = "Your health data is password-protected and never shared with third parties.",
            SvgPath     = "M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
        },
    };
    public void OnGet() { }
}
