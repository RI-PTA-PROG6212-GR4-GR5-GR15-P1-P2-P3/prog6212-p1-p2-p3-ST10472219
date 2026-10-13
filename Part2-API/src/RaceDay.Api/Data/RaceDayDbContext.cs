using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// EF Core code-first context. The model mirrors the Part 1 ERD and SQL script
/// (same table names, column names, keys, unique constraints and check constraints).
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organiser>(e =>
        {
            e.ToTable("Organiser");
            e.HasKey(x => x.OrganiserID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.HasKey(x => x.ParticipantID);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.DateOfBirth).HasColumnType("date");
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Event", t =>
            {
                t.HasCheckConstraint("CK_Event_Type", "[EventType] IN ('Running','Cycling','Walking')");
                t.HasCheckConstraint("CK_Event_Status", "[Status] IN ('Scheduled','Cancelled','Completed')");
            });
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("datetime2");
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired().HasDefaultValue(EventTypes.Running);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(EventStatuses.Scheduled);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            e.HasOne(x => x.Organiser)
             .WithMany(o => o.Events)
             .HasForeignKey(x => x.OrganiserID)
             .HasConstraintName("FK_Event_Organiser")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Category", t =>
            {
                t.HasCheckConstraint("CK_Category_Distance", "[DistanceKm] > 0");
                t.HasCheckConstraint("CK_Category_MaxParticipants", "[MaxParticipants] > 0");
            });
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2).HasDefaultValue(0m);
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);

            e.HasOne(x => x.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(x => x.EventID)
             .HasConstraintName("FK_Category_Event")
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment", t =>
            {
                t.HasCheckConstraint("CK_Enrolment_Status", "[Status] IN ('Pending','Confirmed','Cancelled')");
            });
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.BibNumber).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.BibNumber).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(EnrolmentStatuses.Confirmed);
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID })
             .IsUnique()
             .HasDatabaseName("UQ_Enrolment_Participant_Category");

            e.HasOne(x => x.Participant)
             .WithMany(p => p.Enrolments)
             .HasForeignKey(x => x.ParticipantID)
             .HasConstraintName("FK_Enrolment_Participant")
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryID)
             .HasConstraintName("FK_Enrolment_Category")
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Result", t =>
            {
                t.HasCheckConstraint("CK_Result_Status", "[Status] IN ('Finished','DNF','DSQ')");
            });
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue(ResultStatuses.Finished);
            e.Property(x => x.CapturedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");

            // 1 Enrolment produces 0..1 Result (EnrolmentID is UNIQUE).
            e.HasOne(x => x.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(x => x.EnrolmentID)
             .HasConstraintName("FK_Result_Enrolment")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
