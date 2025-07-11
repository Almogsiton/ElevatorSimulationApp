using Microsoft.EntityFrameworkCore;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.DataAccess.Context
{
    /// <summary>
    /// The Entity Framework context for accessing user data.
    /// </summary>
    public class ElevatorDbContext : DbContext
    {
        public ElevatorDbContext(DbContextOptions<ElevatorDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Elevator> Elevators { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Building>().ToTable("Building"); 
            modelBuilder.Entity<Elevator>().ToTable("Elevator");

            modelBuilder.Entity<Elevator>()
        .HasOne<Building>()
        .WithMany()
        .HasForeignKey(e => e.BuildingId)
        .OnDelete(DeleteBehavior.Cascade);
        }



    }
}
