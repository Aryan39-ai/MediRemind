using Microsoft.EntityFrameworkCore;

namespace MediRemind.Data;

// ── MODELS ────────────────────────────────────────────────────────────────────

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Medication> Medications { get; set; } = new List<Medication>();
}

public class Medication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = "";            // e.g. "Atorvastatin"
    public string Dosage { get; set; } = "";          // e.g. "20mg"
    public string Frequency { get; set; } = "";       // "Once daily", "Twice daily", etc.
    public string TimesOfDay { get; set; } = "";      // comma-sep: "08:00,20:00"
    public string Instructions { get; set; } = "";    // "Take with food"
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? PillCount { get; set; }
    public int? PillsPerDose { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DoseLog> DoseLogs { get; set; } = new List<DoseLog>();
}

public class DoseLog
{
    public int Id { get; set; }
    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;
    public DateTime ScheduledFor { get; set; }
    public DateTime? TakenAt { get; set; }
    public string Status { get; set; } = "Pending";   // Pending, Taken, Missed, Skipped
    public string Notes { get; set; } = "";
}

public class Caregiver
{
    public int Id { get; set; }
    public int PatientUserId { get; set; }            // who shared
    public string CaregiverEmail { get; set; } = "";  // who they shared with
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

// ── DBCONTEXT ─────────────────────────────────────────────────────────────────

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<DoseLog> DoseLogs => Set<DoseLog>();
    public DbSet<Caregiver> Caregivers => Set<Caregiver>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Email).IsUnique();

        mb.Entity<Medication>()
            .HasOne(m => m.User)
            .WithMany(u => u.Medications)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<DoseLog>()
            .HasOne(d => d.Medication)
            .WithMany(m => m.DoseLogs)
            .HasForeignKey(d => d.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
