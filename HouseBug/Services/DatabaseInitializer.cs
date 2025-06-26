using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HouseBug.Services
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync()
        {
            try
            {
                using var context = new BudgetContext();
                
                if (!context.Database.CanConnect())
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Baza danych została utworzona z migracjami.");
                    return;
                }
                
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Migracje zostały wykonane.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas inicjalizacji bazy danych: {ex.Message}");
                throw;
            }
        }
    }
}