using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    /// Контекст базы данных EF Core. Здесь регистрируем конфигурации.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Visit> Visits => Set<Visit>();
        public DbSet<IcdCode> IcdCodes => Set<IcdCode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>()
                .HasIndex(p => new { p.LastName, p.FirstName, p.MiddleName });

            modelBuilder.Entity<IcdCode>()
                .HasIndex(i => i.Code);

            modelBuilder.Entity<Visit>()
                .HasOne(v => v.Patient)
                .WithMany(p => p.Visits)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.Property(p => p.LastName).HasMaxLength(50).IsRequired();
                entity.Property(p => p.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(p => p.MiddleName).HasMaxLength(50);
                entity.Property(p => p.Phone).HasMaxLength(15);
            });

            modelBuilder.Entity<Visit>(entity =>
            {
                entity.Property(v => v.IcdCodeText).HasMaxLength(36);
                entity.Property(v => v.Description).HasMaxLength(200);
            });
        }
    }
}
