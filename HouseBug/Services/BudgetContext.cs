using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HouseBug.Models;
using System.Threading.Tasks;
using System.Threading;


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
            
            // Włącz szczegółowe logowanie w trybie debugowania
            #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.LogTo(Console.WriteLine);
            #endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfiguracja relacji Transaction -> Category
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Konfiguracja relacji MonthlyBudget -> Category
            modelBuilder.Entity<MonthlyBudget>()
                .HasOne(mb => mb.Category)
                .WithMany()
                .HasForeignKey(mb => mb.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indeksy dla lepszej wydajności
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

            // Seeding danych domyślnych
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Domyślne kategorie
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Jedzenie", Description = "Zakupy spożywcze i restauracje", Color = "#E74C3C", Icon = "🍕" },
                new Category { Id = 2, Name = "Transport", Description = "Paliwo, bilety komunikacji publicznej", Color = "#3498DB", Icon = "🚗" },
                new Category { Id = 3, Name = "Rozrywka", Description = "Kino, gry, hobby", Color = "#9B59B6", Icon = "🎮" },
                new Category { Id = 4, Name = "Rachunki", Description = "Czynsz, prąd, gaz, internet", Color = "#F39C12", Icon = "💡" },
                new Category { Id = 5, Name = "Wynagrodzenie", Description = "Pensja i dodatkowe dochody", Color = "#27AE60", Icon = "💰" },
                new Category { Id = 6, Name = "Zdrowie", Description = "Lekarze, apteka, suplementy", Color = "#E67E22", Icon = "⚕️" },
                new Category { Id = 7, Name = "Zakupy", Description = "Ubrania, elektronika, inne", Color = "#1ABC9C", Icon = "🛍️" },
                new Category { Id = 8, Name = "Edukacja", Description = "Kursy, książki, szkolenia", Color = "#34495E", Icon = "📚" }
            );

            // Domyślne ustawienia aplikacji
            modelBuilder.Entity<AppSettings>().HasData(
                new AppSettings 
                { 
                    Id = 1,
                    DefaultCurrency = "PLN",
                    DateFormat = "dd.MM.yyyy",
                    ShowNotifications = true,
                    AutoBackup = false,
                    BackupFrequencyDays = 7,
                    BackupPath = "",
                    DarkMode = false,
                    MonthlyBudgetLimit = 5000,
                    EnableBudgetWarnings = true,
                    BudgetWarningPercentage = 80
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