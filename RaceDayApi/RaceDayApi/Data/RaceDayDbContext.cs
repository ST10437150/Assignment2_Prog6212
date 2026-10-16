using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<WeatherInfo> WeatherInfos => Set<WeatherInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Users ----------
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t =>
            t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(255);
            e.Property(x => x.Role).IsRequired().HasMaxLength(20).HasDefaultValue("Participant");
        });

        // ---------- Events ----------
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            e.HasKey(x => x.EventID);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).IsRequired().HasMaxLength(150);
            e.Property(x => x.RouteDescription).HasMaxLength(500);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(20);

            e.HasOne(x => x.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(x => x.OrganiserID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Events_Users");
        });

        // ---------- Categories ----------
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");

            e.HasOne(x => x.Event)
            .WithMany(ev => ev.Categories)
            .HasForeignKey(x => x.EventID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Categories_Events");
        });

        // ---------- Enrolments ----------
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t =>
            t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')"));
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Confirmed");

            e.HasOne(x => x.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(x => x.ParticipantID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Enrolments_Users");

            e.HasOne(x => x.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(x => x.CategoryID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Enrolments_Categories");

            // A participant can only enrol once per category
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID })
            .IsUnique()
            .HasDatabaseName("UQ_Enrolment");
        });

        // ---------- Results ----------
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");

            // One result per enrolment (unique FK)
            e.HasOne(x => x.Enrolment)
            .WithOne(en => en.Result)
            .HasForeignKey<Result>(x => x.EnrolmentID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Results_Enrolments");
        });

        // ---------- WeatherInfo ----------
        modelBuilder.Entity<WeatherInfo>(e =>
        {
            e.ToTable("WeatherInfo");
            e.HasKey(x => x.WeatherID);
            e.Property(x => x.ForecastSummary).HasMaxLength(200);
            e.Property(x => x.TemperatureC).HasColumnType("decimal(4,1)");

            e.HasOne(x => x.Event)
            .WithMany(ev => ev.WeatherInfos)
            .HasForeignKey(x => x.EventID)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_WeatherInfo_Events");
        });
    }
}