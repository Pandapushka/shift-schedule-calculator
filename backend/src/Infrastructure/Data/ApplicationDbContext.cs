using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ShiftSchedule> ShiftSchedules { get; set; }
    public DbSet<Overtime> Overtimes { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.CalendarJson).HasColumnType("TEXT");
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.HasMany(e => e.Overtimes)
                .WithOne(o => o.ShiftSchedule)
                .HasForeignKey(o => o.ShiftScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Overtime>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.Rating).HasDefaultValue(5);
            entity.Property(e => e.AdminReply).HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
