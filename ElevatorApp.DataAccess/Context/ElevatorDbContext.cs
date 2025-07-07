using Microsoft.EntityFrameworkCore;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.DataAccess.Context
{
    /// <summary>
    /// The Entity Framework context for accessing elevator-related data.
    /// This class maps domain entities to database tables and provides
    /// an interface for querying and saving data.
    /// </summary>
    public class ElevatorDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorDbContext"/> class
        /// with the specified options.
        /// </summary>
        /// <param name="options">The options to configure the context.</param>
        public ElevatorDbContext(DbContextOptions<ElevatorDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the users table in the database.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the buildings table in the database.
        /// </summary>
        public DbSet<Building> Buildings { get; set; }

        /// <summary>
        /// Gets or sets the elevators table in the database.
        /// </summary>
        public DbSet<Elevator> Elevators { get; set; }

        /// <summary>
        /// Gets or sets the elevator calls table in the database.
        /// </summary>
        public DbSet<ElevatorCall> ElevatorCalls { get; set; }

        /// <summary>
        /// Gets or sets the elevator call assignments table in the database.
        /// </summary>
        public DbSet<ElevatorCallAssignment> ElevatorCallAssignments { get; set; }

        /// <summary>
        /// Configures the model mappings and relationships between entities and database tables.
        /// </summary>
        /// <param name="modelBuilder">The builder used to configure entity mappings.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Explicitly map entities to corresponding table names (singular form as per schema)
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Building>().ToTable("Building");
            modelBuilder.Entity<Elevator>().ToTable("Elevator");
            modelBuilder.Entity<ElevatorCall>().ToTable("ElevatorCall");
            modelBuilder.Entity<ElevatorCallAssignment>().ToTable("ElevatorCallAssignment");

            // Configure the relationship: ElevatorCallAssignment → Elevator (many-to-one)
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(e => e.Elevator)
                .WithMany(e => e.Assignments)
                .HasForeignKey(e => e.ElevatorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent multiple cascade paths

            // Configure the relationship: ElevatorCallAssignment → ElevatorCall (many-to-one)
            modelBuilder.Entity<ElevatorCallAssignment>()
                .HasOne(e => e.ElevatorCall)
                .WithMany(e => e.Assignments)
                .HasForeignKey(e => e.ElevatorCallId)
                .OnDelete(DeleteBehavior.Cascade);
        }


    }


}
