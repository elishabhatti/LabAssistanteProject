using LabAssistanteProject.Models;
using Microsoft.EntityFrameworkCore;

namespace LabAssistanteProject.Data
{
    public class MyAppContext : DbContext
    {
        public MyAppContext(DbContextOptions<MyAppContext> options)
            : base(options)
        { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Facilities> Facilities { get; set; }
        public DbSet<Requests> Requests { get; set; }
        public DbSet<Sessions> Sessions { get; set; }
        public DbSet<History> History { get; set; }

        // 👇 THIS IS THE FIX
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // History → Requests (CASCADE OK)
            modelBuilder.Entity<History>()
                .HasOne(h => h.Request)
                .WithMany(r => r.StatusHistories)
                .HasForeignKey(h => h.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // History → Users (NO CASCADE)
            modelBuilder.Entity<History>()
                .HasOne(h => h.UpdatedByUser)
                .WithMany()
                .HasForeignKey(h => h.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Requests → Requestor (RESTRICT to avoid cascade loops)
            modelBuilder.Entity<Requests>()
                .HasOne(r => r.Requestor)
                .WithMany()
                .HasForeignKey(r => r.RequestorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Requests → Assignee (RESTRICT)
            modelBuilder.Entity<Requests>()
                .HasOne(r => r.Assignee)
                .WithMany()
                .HasForeignKey(r => r.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
