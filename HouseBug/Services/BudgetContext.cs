using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HouseBug.Models;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;


namespace HouseBug.Services
{
    public class BudgetContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MonthlyBudget> MonthlyBudgets { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=budget.db");
            optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ContextDisposed)); // Wyłączenie logowania dla ContextDisposed
            
            #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(Console.WriteLine);
            #endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MonthlyBudget>()
                .HasOne(mb => mb.Category)
                .WithMany()
                .HasForeignKey(mb => mb.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date)
                .HasDatabaseName("IX_Transaction_Date");

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.CategoryId)
                .HasDatabaseName("IX_Transaction_CategoryId");

            modelBuilder.Entity<MonthlyBudget>()
                .HasIndex(mb => new { mb.Month, mb.Year, mb.CategoryId })
                .HasDatabaseName("IX_MonthlyBudget_Period_Category")
                .IsUnique();

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Jedzenie", Description = "Zakupy spożywcze i restauracje", Color = "#E74C3C" },
                new Category { Id = 2, Name = "Transport", Description = "Paliwo, bilety komunikacji publicznej", Color = "#3498DB" },
                new Category { Id = 3, Name = "Rozrywka", Description = "Kino, gry, hobby", Color = "#9B59B6" },
                new Category { Id = 4, Name = "Rachunki", Description = "Czynsz, prąd, gaz, internet", Color = "#F39C12" },
                new Category { Id = 5, Name = "Wynagrodzenie", Description = "Pensja i dodatkowe dochody", Color = "#27AE60" },
                new Category { Id = 6, Name = "Zdrowie", Description = "Lekarze, apteka, suplementy", Color = "#E67E22" }
            );

            modelBuilder.Entity<AppSettings>().HasData(
                new AppSettings 
                { 
                    Id = 1,
                    DefaultCurrency = "PLN",
                }
            );
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Transaction && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var transaction = (Transaction)entry.Entity;
                
                if (entry.State == EntityState.Added)
                {
                    transaction.CreatedAt = DateTime.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    transaction.UpdatedAt = DateTime.Now;
                }
            }
        }
    }
}