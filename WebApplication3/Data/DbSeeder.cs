using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public static class DbSeeder
    {
        public static async Task SeedIcdAsync(AppDbContext db, string jsonPath)
        {
            Console.WriteLine($"SeedIcdAsync: path={jsonPath}");

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"ICD-10 JSON not found at {jsonPath}. Seeding skipped.");
                return;
            }

            // ПРОВЕРКА: если в таблице уже есть данные, пропускаем сидинг
            if (await db.IcdCodes.AnyAsync())
            {
                Console.WriteLine("ICD-10 table already has data. Seeding skipped.");
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("ICD-10 JSON is empty.");
                return;
            }

            // Десериализация с учетом маленькой буквы
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<IcdRoot>(json, options);

            if (root?.Records == null || root.Records.Count == 0)
            {
                Console.WriteLine("ICD-10 JSON empty or invalid.");
                return;
            }

            Console.WriteLine($"Parsed {root.Records.Count} ICD records from JSON.");

            var idMap = root.Records.ToDictionary(r => r.ID.ToString(), r => Guid.NewGuid());
            int added = 0;

            foreach (var r in root.Records)
            {
                // Дополнительная проверка на дублирование (на всякий случай)
                if (await db.IcdCodes.AnyAsync(x => x.Code == r.MKB_CODE))
                {
                    Console.WriteLine($"Code {r.MKB_CODE} already exists, skipping.");
                    continue;
                }

                var icd = new IcdCode
                {
                    Id = idMap[r.ID.ToString()],
                    RecCode = r.REC_CODE,
                    Code = r.MKB_CODE,
                    Name = r.MKB_NAME,
                    ParentId = r.ID_PARENT != null && idMap.ContainsKey(r.ID_PARENT.ToString())
                        ? idMap[r.ID_PARENT.ToString()]
                        : null,
                    IsActual = r.ACTUAL == 1
                };

                await db.IcdCodes.AddAsync(icd);
                added++;
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"ICD-10 seeding completed. Added {added} records.");
        }

        public static async Task SeedPatientsAsync(AppDbContext db)
        {
            // ПРОВЕРКА: если в таблице уже есть пациенты, пропускаем сидинг
            if (await db.Patients.AnyAsync())
            {
                Console.WriteLine("Patients table already has data. Seeding skipped.");
                return;
            }

            // Используем DateTimeKind.Utc
            DateTime ToUtc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

            var p1 = new Patient
            {
                LastName = "Иванов",
                FirstName = "Иван",
                MiddleName = "Иванович",
                BirthDate = ToUtc(1985, 5, 12),
                Phone = "+7 (999) 123-45-67"
            };

            var p2 = new Patient
            {
                LastName = "Петрова",
                FirstName = "Мария",
                MiddleName = "Сергеевна",
                BirthDate = ToUtc(1992, 11, 2),
                Phone = "+7 (901) 555-00-11"
            };

            var p3 = new Patient
            {
                LastName = "Сидоров",
                FirstName = "Алексей",
                MiddleName = "Павлович",
                BirthDate = ToUtc(1978, 3, 20),
                Phone = "+7 (912) 222-33-44"
            };

            await db.Patients.AddRangeAsync(p1, p2, p3);
            await db.SaveChangesAsync();

            var visits = new List<Visit>
            {
                new Visit { PatientId = p1.Id, VisitDate = DateTime.UtcNow.AddDays(-10), IcdCodeText = "R51", Description = "Головная боль" },
                new Visit { PatientId = p1.Id, VisitDate = DateTime.UtcNow.AddDays(-3), IcdCodeText = "Z09", Description = "Повторный приём" },
                new Visit { PatientId = p2.Id, VisitDate = DateTime.UtcNow.AddDays(-5), IcdCodeText = "K29.8", Description = "Дуоденит" },
                new Visit { PatientId = p3.Id, VisitDate = DateTime.UtcNow.AddDays(-1), IcdCodeText = "I89.1", Description = "Лимфангит" }
            };

            await db.Visits.AddRangeAsync(visits);
            await db.SaveChangesAsync();

            Console.WriteLine($"Seeded {visits.Count} visits and 3 patients.");
        }

        private class IcdRoot
        {
            public List<IcdRecord> Records { get; set; } = new();
        }

        private class IcdRecord
        {
            public int ID { get; set; }
            public string REC_CODE { get; set; } = string.Empty;
            public string MKB_CODE { get; set; } = string.Empty;
            public string MKB_NAME { get; set; } = string.Empty;
            public string ID_PARENT { get; set; } = null!;
            public int ACTUAL { get; set; }
        }
    }
}