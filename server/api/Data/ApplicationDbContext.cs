using Microsoft.EntityFrameworkCore;
using ElevatorSimulationApi.Models.Entities;

namespace ElevatorSimulationApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Elevator> Elevators { get; set; }
    public DbSet<ElevatorCall> ElevatorCalls { get; set; }
    public DbSet<ElevatorCallAssignment> ElevatorCallAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.NumberOfFloors).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.Buildings).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<Elevator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrentFloor).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Direction).IsRequired();
            entity.Property(e => e.DoorStatus).IsRequired();
            entity.HasOne(e => e.Building).WithOne(b => b.Elevator).HasForeignKey<Elevator>(e => e.BuildingId);
        });

        modelBuilder.Entity<ElevatorCall>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestedFloor).IsRequired();
            entity.Property(e => e.CallTime).IsRequired();
            entity.Property(e => e.IsHandled).IsRequired();
            entity.HasOne(e => e.Building).WithMany(b => b.ElevatorCalls).HasForeignKey(e => e.BuildingId);
            entity.HasOne(e => e.Assignment).WithOne(a => a.ElevatorCall).HasForeignKey<ElevatorCallAssignment>(a => a.ElevatorCallId);
        });

        modelBuilder.Entity<ElevatorCallAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssignmentTime).IsRequired();
            entity.HasOne(e => e.Elevator).WithMany(e => e.CallAssignments).HasForeignKey(e => e.ElevatorId);
        });
    }
} 