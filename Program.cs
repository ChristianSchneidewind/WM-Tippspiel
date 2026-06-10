using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using TippSpiel.Data;
using TippSpiel.Models;
using TippSpiel.Models.Admin;
using TippSpiel.Services;

namespace TippSpiel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Lade lokale .env (muss vor CreateBuilder passieren)
            Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
            builder.Services.Configure<SeedUsersOptions>(builder.Configuration.GetSection("SeedUsers"));

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IGameRepository, EfGameRepository>();

            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
            });

            builder.Services.AddAuthorization();

            // Registriere den Excel Service
            builder.Services.AddScoped<ExcelImportService>();

            var app = builder.Build();

            // --- DATEN-SYNCHRONISIERUNG BEIM START ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<ApplicationDbContext>();
                var excelService = services.GetRequiredService<ExcelImportService>();

                try
                {
                    db.Database.Migrate();

                    // 2. FIFA Teams importieren
                    Console.WriteLine("Starte FIFA Team-Import...");
                    await FifaSeeder.SeedTeamsAsync(db);
                    Console.WriteLine("FIFA Team-Import abgeschlossen.");

                    // 3. FIFA Spieler importieren
                    Console.WriteLine("Starte FIFA Spieler-Import...");
                    await PlayerSeeder.SeedPlayersAsync(db);
                    Console.WriteLine("FIFA Spieler-Import abgeschlossen.");

                    // 4. Excel-Import (nur Gruppen, Spiele und Stadien)
                    Console.WriteLine("Starte Excel-Import...");
                    string excelPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "WCup_2026_4.2.5_de.xlsx"
                        );

                    var venueMap = ExcelSeeder.SeedFromExcel(db, excelPath);

                    Console.WriteLine("Excel-Import abgeschlossen.");

                    Console.WriteLine("===== TEAMS =====");

                    foreach (var team in db.Teams.Take(10))
                    {
                        Console.WriteLine
                        (
                            $"{team.Name} | {team.ExternalId} | {team.Slug}"
                        );
}

                    // 4. Fehlende K.o.-Spiele ergänzen
                    await KnockoutSeeder.SeedAsync(db, venueMap);

                    // 5. User anlegen
                    await UserSeeder.SeedAsync(services);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kritischer Fehler beim Datenbank-Setup: {ex.Message}");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}