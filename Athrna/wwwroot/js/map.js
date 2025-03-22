// Interactive Map Functionality
document.addEventListener('DOMContentLoaded', function () {
    const mapDisplay = document.getElementById('map');
    const siteInfo = document.getElementById('siteInfo');
    const siteName = document.getElementById('siteName');
    const siteDescription = document.getElementById('siteDescription');
    const siteImage = document.getElementById('siteImage');
    const yearValue = document.getElementById('yearValue');
    const mapButtons = document.querySelectorAll('.map-btn');

    // Historical sites data
    const historicalSites = {
        madinah: [
            {
                name: "Prophet's Mosque (Al-Masjid an-Nabawi)",
                description: "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.",
                position: { top: "40%", left: "50%" },
                image: "/images/sites/masjid nabwi.jpg",
                year: "622 CE"
            },
            {
                name: "Quba Mosque",
                description: "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.",
                position: { top: "65%", left: "30%" },
                image: "/images/sites/Quba.jpg",
                year: "622 CE"
            },
            {
                name: "Battlefield of Uhud",
                description: "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.",
                position: { top: "20%", left: "70%" },
                image: "/images/sites/Uhud.jpg",
                year: "625 CE"
            },
            {
                name: "Al-Baqi Cemetery",
                description: "Ancient cemetery containing the graves of many of Prophet Muhammad's companions and family members.",
                position: { top: "45%", left: "65%" },
                image: "/images/sites/Baqi.jpg",
                year: "622 CE"
            }
        ],
        riyadh: [
            {
                name: "Diriyah",
                description: "UNESCO World Heritage site and the birthplace of the first Saudi state. Features mud-brick structures and is currently being restored as a cultural heritage district.",
                position: { top: "30%", left: "25%" },
                image: "/api/placeholder/300/150",
                year: "1744 CE"
            },
            {
                name: "Masmak Fortress",
                description: "A clay and mud-brick fort that played a crucial role in the history of Saudi Arabia. It was the site of Ibn Saud's recapture of Riyadh in 1902.",
                position: { top: "45%", left: "55%" },
                image: "/api/placeholder/300/150",
                year: "1865 CE"
            },
            {
                name: "National Museum",
                description: "Saudi Arabia's most comprehensive museum, showcasing the country's history from prehistoric times to the modern era across eight galleries.",
                position: { top: "60%", left: "40%" },
                image: "/api/placeholder/300/150",
                year: "1999 CE"
            },
            {
                name: "Murabba Palace",
                description: "Built by King Abdulaziz in 1937, this palace exemplifies traditional Najdi architecture and served as his residence and court.",
                position: { top: "35%", left: "70%" },
                image: "/api/placeholder/300/150",
                year: "1937 CE"
            }
        ],
        alula: [
            {
                name: "Hegra (Mada'in Salih)",
                description: "Saudi Arabia's first UNESCO World Heritage site, featuring over 100 well-preserved tombs with elaborate facades carved into sandstone outcrops.",
                position: { top: "50%", left: "60%" },
                image: "/api/placeholder/300/150",
                year: "1st century BCE"
            },
            {
                name: "Dadan",
                description: "Ancient capital of the Dadanite and Lihyanite kingdoms, featuring remains of settlements and tombs carved into the mountainside.",
                position: { top: "30%", left: "40%" },
                image: "/api/placeholder/300/150",
                year: "9th-8th century BCE"
            },
            {
                name: "Jabal Ikmah",
                description: "An open-air library with thousands of inscriptions in various ancient scripts, offering insights into the history and culture of the region.",
                position: { top: "65%", left: "25%" },
                image: "/api/placeholder/300/150",
                year: "1st millennium BCE"
            },
            {
                name: "Old Town of AlUla",
                description: "A labyrinth of over 900 mud-brick houses built on a hillside, abandoned in the 1980s but now being carefully restored.",
                position: { top: "40%", left: "75%" },
                image: "/api/placeholder/300/150",
                year: "13th century CE"
            }
        ]
    };

    // Set background maps for each city
    const cityMaps = {
        madinah: "linear-gradient(rgba(255, 255, 255, 0.7), rgba(255, 255, 255, 0.7)), url('/images/Maps/Medina.jpg')",
        riyadh: "linear-gradient(rgba(255, 255, 255, 0.7), rgba(255, 255, 255, 0.7)), url('/images/Maps/Riyadh.jpg')",
        alula: "linear-gradient(rgba(255, 255, 255, 0.7), rgba(255, 255, 255, 0.7)), url('/images/Maps/ALULa.jpg')"
    };

    // Function to display sites for a selected city
    function displaySites(city) {
        // Clear existing markers
        mapDisplay.innerHTML = '';

        // Set city map background
        mapDisplay.style.backgroundImage = cityMaps[city];
        mapDisplay.style.backgroundSize = 'cover';
        mapDisplay.style.backgroundPosition = 'center';

        // Add markers for each site
        historicalSites[city].forEach(site => {
            const marker = document.createElement('div');
            marker.className = 'map-marker';
            marker.style.top = site.position.top;
            marker.style.left = site.position.left;

            // Add click event to show site info
            marker.addEventListener('click', () => {
                siteName.textContent = site.name;
                siteDescription.textContent = site.description;
                siteImage.src = site.image;
                siteImage.alt = site.name;
                yearValue.textContent = site.year;
                siteInfo.style.display = 'block';
            });

            mapDisplay.appendChild(marker);
        });
    }

    // Initialize map with Madinah sites if element exists
    if (mapDisplay) {
        displaySites('madinah');

        // Add event listeners to city buttons
        mapButtons.forEach(button => {
            button.addEventListener('click', () => {
                // Remove active class from all buttons
                mapButtons.forEach(btn => btn.classList.remove('active'));

                // Add active class to clicked button
                button.classList.add('active');

                // Display sites for selected city
                const city = button.getAttribute('data-city');
                displaySites(city);

                // Hide site info
                siteInfo.style.display = 'none';
            });
        });

        // Close site info when clicking elsewhere on the map
        mapDisplay.addEventListener('click', (event) => {
            if (event.target === mapDisplay) {
                siteInfo.style.display = 'none';
            }
        });
    }
});