using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;

namespace Athrna.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;

        public DatabaseSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedDatabaseAsync()
        {
            // Only seed if database is empty
            if (await _context.City.AnyAsync())
            {
                return; // Database has been seeded already
            }

            // Add languages
            var languages = new List<Language>
            {
                new Language { Name = "English", Code = "en" },
                new Language { Name = "Arabic", Code = "ar" }
            };
            await _context.Language.AddRangeAsync(languages);
            await _context.SaveChangesAsync();

            // Add cities
            var cities = new List<City>
            {
                new City { Name = "Madinah" },
                new City { Name = "Riyadh" },
                new City { Name = "AlUla"}
            };
            await _context.City.AddRangeAsync(cities);
            await _context.SaveChangesAsync();

            // Retrieve cities by name for referencing
            var madinah = await _context.City.FirstOrDefaultAsync(c => c.Name == "Madinah");
            var riyadh = await _context.City.FirstOrDefaultAsync(c => c.Name == "Riyadh");
            var alula = await _context.City.FirstOrDefaultAsync(c => c.Name == "AlUla");

            // Retrieve languages by code for referencing
            var english = await _context.Language.FirstOrDefaultAsync(l => l.Code == "en");
            var arabic = await _context.Language.FirstOrDefaultAsync(l => l.Code == "ar");

            // Add historical sites for Madinah
            if (madinah != null)
            {
                var madinahSites = new List<Site>
                {
                    new Site
                    {
                        Name = "Prophet's Mosque",
                        CityId = madinah.Id,
                        Location = "24.4672° N, 39.6111° E",
                        Description = "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.",
                        SiteType = "Mosque"
                    },
                    new Site
                    {
                        Name = "Quba Mosque",
                        CityId = madinah.Id,
                        Location = "24.4408° N, 39.6168° E",
                        Description = "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.",
                        SiteType = "Mosque"
                    },
                    new Site
                    {
                        Name = "Battlefield of Uhud",
                        CityId = madinah.Id,
                        Location = "24.5075° N, 39.5850° E",
                        Description = "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.",
                        SiteType = "Historical Site"
                    },
                    new Site
                    {
                        Name = "Al-Baqi Cemetery",
                        CityId = madinah.Id,
                        Location = "24.4667° N, 39.6169° E",
                        Description = "Ancient cemetery containing the graves of many of Prophet Muhammad's companions and family members.",
                        SiteType = "Cemetery"
                    }
                };
                await _context.Site.AddRangeAsync(madinahSites);
                await _context.SaveChangesAsync();

                // Add guides for Madinah
                var madinahGuides = new List<Guide>
                {
                    new Guide
                    {
                        CityId = madinah.Id,
                        FullName = "Ahmed Khalid",
                        Email = "ahmed.khalid@example.com",
                        NationalId = "1122334455",
                        Password = "guide123"
                    },
                    new Guide
                    {
                        CityId = madinah.Id,
                        FullName = "Fatima Ali",
                        Email = "fatima.ali@example.com",
                        NationalId = "5544332211",
                        Password = "guide123"
                    }
                };
                await _context.Guide.AddRangeAsync(madinahGuides);
                await _context.SaveChangesAsync();
            }

            // Add historical sites for Riyadh
            if (riyadh != null)
            {
                var riyadhSites = new List<Site>
                {
                    new Site
                    {
                        Name = "Diriyah",
                        CityId = riyadh.Id,
                        Location = "24.7470° N, 46.5724° E",
                        Description = "UNESCO World Heritage site and the birthplace of the first Saudi state. Features mud-brick structures and is currently being restored as a cultural heritage district.",
                        SiteType = "Heritage Site"
                    },
                    new Site
                    {
                        Name = "Masmak Fortress",
                        CityId = riyadh.Id,
                        Location = "24.6308° N, 46.7137° E",
                        Description = "A clay and mud-brick fort that played a crucial role in the history of Saudi Arabia. It was the site of Ibn Saud's recapture of Riyadh in 1902.",
                        SiteType = "Fortress"
                    },
                    new Site
                    {
                        Name = "National Museum",
                        CityId = riyadh.Id,
                        Location = "24.6294° N, 46.7161° E",
                        Description = "Saudi Arabia's most comprehensive museum, showcasing the country's history from prehistoric times to the modern era across eight galleries.",
                        SiteType = "Museum"
                    },
                    new Site
                    {
                        Name = "Murabba Palace",
                        CityId = riyadh.Id,
                        Location = "24.6383° N, 46.7124° E",
                        Description = "Built by King Abdulaziz in 1937, this palace exemplifies traditional Najdi architecture and served as his residence and court.",
                        SiteType = "Palace"
                    }
                };
                await _context.Site.AddRangeAsync(riyadhSites);
                await _context.SaveChangesAsync();

                // Add guides for Riyadh
                var riyadhGuides = new List<Guide>
                {
                    new Guide
                    {
                        CityId = riyadh.Id,
                        FullName = "Mohammed Ibrahim",
                        Email = "mohammed.ibrahim@example.com",
                        NationalId = "9988776655",
                        Password = "guide123"
                    },
                    new Guide
                    {
                        CityId = riyadh.Id,
                        FullName = "Sara Ahmad",
                        Email = "sara.ahmad@example.com",
                        NationalId = "1199887766",
                        Password = "guide123"
                    }
                };
                await _context.Guide.AddRangeAsync(riyadhGuides);
                await _context.SaveChangesAsync();
            }

            // Add historical sites for AlUla
            if (alula != null)
            {
                var alulaSites = new List<Site>
                {
                    new Site
                    {
                        Name = "Hegra (Mada'in Salih)",
                        CityId = alula.Id,
                        Location = "26.7917° N, 37.9542° E",
                        Description = "Saudi Arabia's first UNESCO World Heritage site, featuring over 100 well-preserved tombs with elaborate facades carved into sandstone outcrops.",
                        SiteType = "Heritage Site"
                    },
                    new Site
                    {
                        Name = "Dadan",
                        CityId = alula.Id,
                        Location = "26.8456° N, 37.9344° E",
                        Description = "Ancient capital of the Dadanite and Lihyanite kingdoms, featuring remains of settlements and tombs carved into the mountainside.",
                        SiteType = "Archaeological Site"
                    },
                    new Site
                    {
                        Name = "Jabal Ikmah",
                        CityId = alula.Id,
                        Location = "26.8233° N, 37.9511° E",
                        Description = "An open-air library with thousands of inscriptions in various ancient scripts, offering insights into the history and culture of the region.",
                        SiteType = "Archaeological Site"
                    },
                    new Site
                    {
                        Name = "Old Town of AlUla",
                        CityId = alula.Id,
                        Location = "26.6183° N, 37.9142° E",
                        Description = "A labyrinth of over 900 mud-brick houses built on a hillside, abandoned in the 1980s but now being carefully restored.",
                        SiteType = "Historical Town"
                    }
                };
                await _context.Site.AddRangeAsync(alulaSites);
                await _context.SaveChangesAsync();

                // Add guides for AlUla
                var alulaGuides = new List<Guide>
                {
                    new Guide
                    {
                        CityId = alula.Id,
                        FullName = "Hassan Abdullah",
                        Email = "hassan.abdullah@example.com",
                        NationalId = "6677889900",
                        Password = "guide123"
                    },
                    new Guide
                    {
                        CityId = alula.Id,
                        FullName = "Noura Salem",
                        Email = "noura.salem@example.com",
                        NationalId = "5566778899",
                        Password = "guide123"
                    }
                };
                await _context.Guide.AddRangeAsync(alulaGuides);
                await _context.SaveChangesAsync();
            }

            // Add cultural information for sites
            var sites = await _context.Site.ToListAsync();
            foreach (var site in sites)
            {
                var culturalInfo = new CulturalInfo
                {
                    SiteId = site.Id,
                    Summary = $"Cultural and historical information about {site.Name}.",
                    EstablishedDate = new DateTime(622, 1, 1) // Default date for example purposes
                };

                // For specific sites, set actual dates
                if (site.Name == "Prophet's Mosque")
                {
                    culturalInfo.EstablishedDate = new DateTime(622, 1, 1);
                    culturalInfo.Summary = "The Prophet's Mosque was established by Prophet Muhammad upon his arrival in Medina in 622 CE. It has been expanded many times throughout Islamic history.";
                }
                else if (site.Name == "Diriyah")
                {
                    culturalInfo.EstablishedDate = new DateTime(1744, 1, 1);
                    culturalInfo.Summary = "Diriyah was the original home of the Saudi royal family and served as the capital of the First Saudi State from 1744 to 1818.";
                }
                else if (site.Name == "Hegra (Mada'in Salih)")
                {
                    culturalInfo.EstablishedDate = new DateTime(-100, 1, 1); // 1st century BCE
                    culturalInfo.Summary = "Hegra was the southernmost settlement of the Nabataean Kingdom, dating from the 1st century BCE to the 1st century CE. It features well-preserved monumental tombs with decorated facades.";
                }

                await _context.CulturalInfo.AddAsync(culturalInfo);
                await _context.SaveChangesAsync();

                // Add translations for cultural info
                if (english != null && arabic != null)
                {
                    var culturalInfoTranslations = new List<CulturalInfoTranslation>
                    {
                        new CulturalInfoTranslation
                        {
                            CulturalInfoId = culturalInfo.Id,
                            LanguageId = english.Id,
                            TranslatedSummary = culturalInfo.Summary // Same as default for English
                        },
                        new CulturalInfoTranslation
                        {
                            CulturalInfoId = culturalInfo.Id,
                            LanguageId = arabic.Id,
                            TranslatedSummary = "معلومات ثقافية وتاريخية." // Generic Arabic translation
                        }
                    };
                    await _context.CulturalInfoTranslation.AddRangeAsync(culturalInfoTranslations);
                    await _context.SaveChangesAsync();
                }
            }

            // Create admin user
            var adminClient = new Client
            {
                Username = "admin",
                Email = "admin@athrna.com",
                EncryptedPassword = "admin123"
            };
            await _context.Client.AddAsync(adminClient);
            await _context.SaveChangesAsync();

            // Add administrator role
            var administrator = new Administrator
            {
                ClientId = adminClient.Id
            };
            await _context.Administrator.AddAsync(administrator);
            await _context.SaveChangesAsync();

            // Create regular users
            var regularUsers = new List<Client>
            {
                new Client
                {
                    Username = "user1",
                    Email = "user1@example.com",
                    EncryptedPassword = "user123"
                },
                new Client
                {
                    Username = "user2",
                    Email = "user2@example.com",
                    EncryptedPassword = "user123"
                }
            };
            await _context.Client.AddRangeAsync(regularUsers);
            await _context.SaveChangesAsync();
        }
    }
}