using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication3.Data;

namespace WebApplication3
{
    public class Program
    {
        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // EF Core + PostgreSQL
            builder.Services.AddDbContext<AppDbContext>(opts =>
                opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Посев данных при старте
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<AppDbContext>();

                    Console.WriteLine("=== Starting migration ===");
                    await db.Database.MigrateAsync();
                    Console.WriteLine("Migration completed.");

                    var icdPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "data", "icd10.json");
                    Console.WriteLine($"Looking for ICD-10 JSON at {icdPath}");

                    if (File.Exists(icdPath))
                    {
                        Console.WriteLine("File found. Starting ICD-10 seeding...");
                        await DbSeeder.SeedIcdAsync(db, icdPath);
                        Console.WriteLine("ICD-10 seeding finished.");
                    }
                    else
                    {
                        Console.WriteLine($"ICD-10 JSON not found at {icdPath}. Seeding skipped.");
                    }

                    Console.WriteLine("Starting patient/visit seeding...");
                    await DbSeeder.SeedPatientsAsync(db);
                    Console.WriteLine("Patient/visit seeding finished.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== ERROR during migration/seeding ===");
                    Console.WriteLine(ex.ToString());
                }
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Patients}/{action=Index}/{id?}");

            Console.WriteLine("=== Application starting ===");
            await app.RunAsync();
        }
    }
}
