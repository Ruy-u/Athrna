using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;

namespace Athrna.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        // Valid license numbers for guide registration
        public static readonly string[] ValidLicenseNumbers = { "TR-1234", "TR-5678", "TR-9999" };

        public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                // Only seed if database is empty
                if (!await _context.City.AnyAsync())
                {
                    _logger.LogInformation("Seeding database...");

                    // Seed Cities
                    await SeedCitiesAsync();

                    // Seed Sites
                    await SeedSitesAsync();

                    // Seed Languages
                    await SeedLanguagesAsync();

                    // Seed Guides
                    await SeedGuidesAsync();

                    // Seed Admin account
                    await SeedAdminAccountAsync();

                    _logger.LogInformation("Database seeding completed successfully.");
                }
                else
                {
                    _logger.LogInformation("Database already contains data. Skipping seeding.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private async Task SeedCitiesAsync()
        {
            var cities = new List<City>
            {
                new City { Name = "Madinah" },
                new City { Name = "Riyadh" },
                new City { Name = "AlUla" },
                new City { Name = "Jeddah" },
                new City { Name = "Makkah" }
            };

            await _context.City.AddRangeAsync(cities);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cities seeded successfully.");
        }

        private async Task SeedSitesAsync()
        {
            // Get city IDs
            var madinahId = await _context.City.Where(c => c.Name == "Madinah").Select(c => c.Id).FirstOrDefaultAsync();
            var riyadhId = await _context.City.Where(c => c.Name == "Riyadh").Select(c => c.Id).FirstOrDefaultAsync();
            var alulaId = await _context.City.Where(c => c.Name == "AlUla").Select(c => c.Id).FirstOrDefaultAsync();

            // Madinah sites
            var madinahSites = new List<Site>
            {
                new Site
                {
                    Name = "Prophet's Mosque (Al-Masjid an-Nabawi)",
                    CityId = madinahId,
                    SiteType = "Mosque",
                    Location = "24.4672° N, 39.6111° E",
                    Description = "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.",
                    ImagePath = "/images/sites/masjid_nabwi.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = 622,
                        Summary = "Al-Masjid an-Nabawi is the second holiest mosque in Islam and the final resting place of Prophet Muhammad. Originally built by the Prophet himself, it has undergone numerous expansions throughout Islamic history."
                    }
                },
                new Site
                {
                    Name = "Quba Mosque",
                    CityId = madinahId,
                    SiteType = "Mosque",
                    Location = "24.4406° N, 39.6157° E",
                    Description = "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.",
                    ImagePath = "/images/sites/quba.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = 622,
                        Summary = "Quba Mosque is the first mosque built in Islamic history. Prophet Muhammad regularly visited this mosque, traveling there on Saturdays, and encouraged others to do so as well."
                    }
                },
                new Site
                {
                    Name = "Battlefield of Uhud",
                    CityId = madinahId,
                    SiteType = "Historical Site",
                    Location = "24.5084° N, 39.6192° E",
                    Description = "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.",
                    ImagePath = "/images/sites/uhud.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = 625,
                        Summary = "The Battle of Uhud was fought on March 23, 625 CE between a force from the Muslim community of Medina and a force from Mecca. It holds important lessons in Islamic history about discipline and following leadership."
                    }
                }
            };

            // Riyadh sites
            var riyadhSites = new List<Site>
            {
                new Site
                {
                    Name = "Diriyah",
                    CityId = riyadhId,
                    SiteType = "UNESCO World Heritage Site",
                    Location = "24.7384° N, 46.5733° E",
                    Description = "UNESCO World Heritage site and the birthplace of the first Saudi state. Features mud-brick structures and is currently being restored as a cultural heritage district.",
                    ImagePath = "/images/sites/diriyah.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = 1744,
                        Summary = "Diriyah is the original home of the Saudi royal family and the capital of the first Saudi state. Its At-Turaif district is a UNESCO World Heritage Site and is being developed into a major cultural tourism destination."
                    }
                },
                new Site
                {
                    Name = "Masmak Fortress",
                    CityId = riyadhId,
                    SiteType = "Fortress",
                    Location = "24.6301° N, 46.7171° E",
                    Description = "A clay and mud-brick fort that played a crucial role in the history of Saudi Arabia. It was the site of Ibn Saud's recapture of Riyadh in 1902.",
                    ImagePath = "/images/sites/masmak.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = 1865,
                        Summary = "The Masmak Fortress is a symbol of the Saudi unification, where in 1902, King Abdulaziz captured the fortress and began his campaign to unify Saudi Arabia."
                    }
                }
            };

            // AlUla sites
            var alulaSites = new List<Site>
            {
                new Site
                {
                    Name = "Hegra (Mada'in Salih)",
                    CityId = alulaId,
                    SiteType = "UNESCO World Heritage Site",
                    Location = "26.7917° N, 37.9539° E",
                    Description = "Saudi Arabia's first UNESCO World Heritage site, featuring over 100 well-preserved tombs with elaborate facades carved into sandstone outcrops.",
                    ImagePath = "/images/sites/hegra.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = -100,
                        Summary = "Hegra is the southernmost settlement of the Nabataean Kingdom and features monumental tombs with elaborate facades similar to those at Petra, but in a better state of preservation."
                    }
                },
                new Site
                {
                    Name = "Dadan",
                    CityId = alulaId,
                    SiteType = "Archaeological Site",
                    Location = "26.8624° N, 37.7653° E",
                    Description = "Ancient capital of the Dadanite and Lihyanite kingdoms, featuring remains of settlements and tombs carved into the mountainside.",
                    ImagePath = "/images/sites/dadan.jpg",
                    CulturalInfo = new CulturalInfo
                    {
                        EstablishedDate = -800,
                        Summary = "Dadan was a major caravan stopping point and the capital of both the Dadanite and Lihyanite kingdoms. Archaeological excavations continue to reveal more about this important ancient Arabian site."
                    }
                }
            };

            // Add all sites to the database
            await _context.Site.AddRangeAsync(madinahSites);
            await _context.Site.AddRangeAsync(riyadhSites);
            await _context.Site.AddRangeAsync(alulaSites);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Sites seeded successfully.");
        }

        private async Task SeedLanguagesAsync()
        {
            var languages = new List<Language>
            {
                new Language { Name = "English", Code = "en" },
                new Language { Name = "Arabic", Code = "ar" },
                new Language { Name = "French", Code = "fr" },
                new Language { Name = "Spanish", Code = "es" }
            };

            await _context.Language.AddRangeAsync(languages);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Languages seeded successfully.");
        }

        private async Task SeedGuidesAsync()
        {
            // Get city IDs
            var madinahId = await _context.City.Where(c => c.Name == "Madinah").Select(c => c.Id).FirstOrDefaultAsync();
            var riyadhId = await _context.City.Where(c => c.Name == "Riyadh").Select(c => c.Id).FirstOrDefaultAsync();
            var alulaId = await _context.City.Where(c => c.Name == "AlUla").Select(c => c.Id).FirstOrDefaultAsync();

            // Create sample guides for Madinah
            var madinahGuides = new List<Guide>
            {
                new Guide
                {
                    CityId = madinahId,
                    FullName = "Ahmed Al-Madani",
                    NationalId = "1234567890",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "ahmed@athrna.com"
                },
                new Guide
                {
                    CityId = madinahId,
                    FullName = "Fatima Al-Ansari",
                    NationalId = "0987654321",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "fatima@athrna.com"
                }
            };

            // Create sample guides for Riyadh
            var riyadhGuides = new List<Guide>
            {
                new Guide
                {
                    CityId = riyadhId,
                    FullName = "Khalid Al-Saud",
                    NationalId = "5678901234",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "khalid@athrna.com"
                },
                new Guide
                {
                    CityId = riyadhId,
                    FullName = "Nora Al-Qahtani",
                    NationalId = "4321098765",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "nora@athrna.com"
                }
            };

            // Create sample guides for AlUla
            var alulaGuides = new List<Guide>
            {
                new Guide
                {
                    CityId = alulaId,
                    FullName = "Omar Al-Harbi",
                    NationalId = "9012345678",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "omar@athrna.com"
                },
                new Guide
                {
                    CityId = alulaId,
                    FullName = "Layla Al-Otaibi",
                    NationalId = "5432109876",
                    Password = "Guide123!", // In a real app, this would be hashed
                    Email = "layla@athrna.com"
                }
            };

            // Add all guides to database
            await _context.Guide.AddRangeAsync(madinahGuides);
            await _context.Guide.AddRangeAsync(riyadhGuides);
            await _context.Guide.AddRangeAsync(alulaGuides);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Guides seeded successfully.");
        }

        private async Task SeedAdminAccountAsync()
        {
            // Create admin user if it doesn't exist
            if (!await _context.Client.AnyAsync(c => c.Username == "admin"))
            {
                // Create admin user
                var adminUser = new Client
                {
                    Username = "admin",
                    Email = "admin@athrna.com",
                    EncryptedPassword = "Admin123!" // In a real application, this would be properly hashed
                };

                await _context.Client.AddAsync(adminUser);
                await _context.SaveChangesAsync();

                // Assign admin role to the user
                var admin = new Administrator
                {
                    ClientId = adminUser.Id
                };

                await _context.Administrator.AddAsync(admin);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin account created successfully.");
            }
        }
    }
}