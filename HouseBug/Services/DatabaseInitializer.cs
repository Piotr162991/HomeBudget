using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
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
                
                var canConnect = context.Database.CanConnect();
                
                if (!canConnect)
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Baza danych została utworzona z migracjami.");
                }
                else
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        await context.Database.MigrateAsync();
                        Console.WriteLine("Migracje zostały wykonane.");
                    }
                    else
                    {
                        Console.WriteLine("Baza danych jest aktualna.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas inicjalizacji bazy danych: {ex.Message}");
                
                if (ex.Message.Contains("already exists"))
                {
                    Console.WriteLine("Wykryto konflikt tabel. Usuwanie starej bazy...");
                    if (File.Exists("budget.db"))
                    {
                        File.Delete("budget.db");
                        Console.WriteLine("Stara baza została usunięta. Ponowne tworzenie...");
                        await InitializeAsync(); 
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public static bool DatabaseExists()
        {
            try
            {
                using var context = new BudgetContext();
                return context.Database.CanConnect();
            }
            catch
            {
                return false;
            }
        }

        public static void BackupDatabase(string backupPath)
        {
            try
            {
                var databasePath = "budget.db";
                if (File.Exists(databasePath))
                {
                    File.Copy(databasePath, backupPath, true);
                    Console.WriteLine($"Kopia zapasowa utworzona: {backupPath}");
                }
                else
                {
                    throw new FileNotFoundException("Plik bazy danych nie został znaleziony.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas tworzenia kopii zapasowej: {ex.Message}");
                throw;
            }
        }

        public static void RestoreDatabase(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    var databasePath = "budget.db";
                    File.Copy(backupPath, databasePath, true);
                    Console.WriteLine($"Baza danych przywrócona z: {backupPath}");
                }
                else
                {
                    throw new FileNotFoundException("Plik kopii zapasowej nie został znaleziony.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas przywracania bazy danych: {ex.Message}");
                throw;
            }
        }
    }
}