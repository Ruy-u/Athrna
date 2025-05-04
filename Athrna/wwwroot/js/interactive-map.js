// interactive-map.js

/**
 * Interactive Map Component for Athrna Project
 * This script integrates Google Maps to display cities and historical sites
 */

// Map configuration and state
let map;
let markers = [];
let infoWindow;
let activeCity = null;
let isInitialized = false;

// Historical sites data with coordinates
const historicalSites = {
    madinah: [
        {
            id: 1,
            name: "Prophet's Mosque (Al-Masjid an-Nabawi)",
            description: "The second holiest site in Islam, built by Prophet Muhammad in 622 CE.",
            position: { lat: 24.4672, lng: 39.6111 },
            image: "/images/sites/masjid_nabwi.jpg",
            year: "622 CE",
            dbId: 21  // Updated DB ID
        },
        {
            id: 2,
            name: "Quba Mosque",
            description: "The first mosque built in Islam.",
            position: { lat: 24.4395, lng: 39.6168 },
            image: "/images/sites/quba.jpg",
            year: "622 CE",
            dbId: 22  // Updated DB ID
        },
        {
            id: 3,
            name: "Battlefield of Uhud",
            description: "Site of the Battle of Uhud in 625 CE.",
            position: { lat: 24.4854, lng: 39.6169 },
            image: "/images/sites/uhud.jpg",
            year: "625 CE",
            dbId: 24  // Keep original ID
        },
        {
            id: 4,
            name: "Al-Baqi Cemetery (Jannat al-Baqi)",
            description: "Ancient cemetery containing the graves of many of Prophet Muhammad's companions.",
            position: { lat: 24.4664, lng: 39.6161 },
            image: "/images/sites/baqi.jpg",
            year: "622 CE",
            dbId: 23  // Updated DB ID
        }
    ],
    riyadh: [
        {
            id: 5,
            name: "Diriyah",
            description: "UNESCO World Heritage site and the birthplace of the first Saudi state.",
            position: { lat: 24.7363, lng: 46.5763 },
            image: "/images/sites/diriyah.jpg",
            year: "1744 CE",
            dbId: 4  // Updated DB ID
        },
        {
            id: 6,
            name: "Masmak Fortress",
            description: "A clay and mud-brick fort that played a crucial role in the history of Saudi Arabia.",
            position: { lat: 24.6309, lng: 46.7127 },
            image: "/images/sites/masmak.jpg",
            year: "1865 CE",
            dbId: 5  // Updated DB ID
        },
        {
            id: 7,
            name: "National Museum",
            description: "Saudi Arabia's most comprehensive museum.",
            position: { lat: 24.6818, lng: 46.7160 },
            image: "/images/sites/national_museum.jpg",
            year: "1999 CE",
            dbId: 7  // Keep original ID
        },
        {
            id: 8,
            name: "Murabba Palace",
            description: "Built by King Abdulaziz in 1937, this palace exemplifies traditional Najdi architecture.",
            position: { lat: 24.6510, lng: 46.7126 },
            image: "/images/sites/murabba_palace.jpg",
            year: "1937 CE",
            dbId: 8  // Keep original ID
        }
    ],
    alula: [
        {
            id: 9,
            name: "Hegra (Mada'in Salih)",
            description: "Saudi Arabia's first UNESCO World Heritage site.",
            position: { lat: 26.7891, lng: 37.9444 },
            image: "/images/sites/hegra.jpg",
            year: "1st century BCE",
            dbId: 6  // Updated DB ID
        },
        {
            id: 10,
            name: "Dadan",
            description: "Ancient capital of the Dadanite and Lihyanite kingdoms.",
            position: { lat: 26.6058, lng: 37.9157 },
            image: "/images/sites/dadan.jpg",
            year: "9th-8th century BCE",
            dbId: 7  // Updated DB ID
        },
        {
            id: 11,
            name: "Jabal Ikmah",
            description: "An open-air library with thousands of inscriptions in various ancient scripts.",
            position: { lat: 26.6211, lng: 37.9259 },
            image: "/images/sites/Japal ikmah.jpg",
            year: "1st millennium BCE",
            dbId: 18  // Updated DB ID
        },
        {
            id: 12,
            name: "Old Town of AlUla",
            description: "A labyrinth of over 900 mud-brick houses built on a hillside.",
            position: { lat: 26.6180, lng: 37.9131 },
            image: "/images/sites/alula_old_town.jpg",
            year: "13th century CE",
            dbId: 12  // Keep original ID
        }
    ]
};

// City information with coordinates
const cities = {
    madinah: {
        name: "Madinah",
        position: { lat: 24.5246, lng: 39.5692 },
        zoom: 11,
        description: "The second holiest city in Islam, home to the Prophet's Mosque."
    },
    riyadh: {
        name: "Riyadh",
        position: { lat: 24.7136, lng: 46.6753 },
        zoom: 11,
        description: "The capital city of Saudi Arabia, blending modernity with history."
    },
    alula: {
        name: "AlUla",
        position: { lat: 26.6175, lng: 37.9158 },
        zoom: 10,
        description: "An archaeological wonder with Hegra, Saudi Arabia's first UNESCO World Heritage site."
    }
};

/**
 * Loads the Google Maps API and initializes the map
 */
function loadGoogleMapsApi() {
    // Check if map container exists
    const mapContainer = document.getElementById('googleMap');
    if (!mapContainer) {
        console.log("Map container not found, skipping map initialization");
        return;
    }

    // Prevent double initialization
    if (isInitialized) {
        console.log("Map already initialized, skipping");
        return;
    }

    // Use a static API key for now to avoid AJAX issues
    // In production, you should retrieve this from the server
    const apiKey = "AIzaSyCfmD2cv4d6ML3shp0YER9gpHT5VHdsvpQ"; // Replace with your actual API key

    // Create script element
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&callback=initMap`;
    script.async = true;
    script.defer = true;

    // Add error handling
    script.onerror = function () {
        console.error('Failed to load Google Maps API');
        const mapContainer = document.getElementById('googleMap');
        if (mapContainer) {
            mapContainer.innerHTML = '<div class="map-error">Failed to load Google Maps. Please try again later.</div>';
        }
    };

    // Append script to body
    document.body.appendChild(script);
}

/**
 * Initializes the Google Map
 */
function initMap() {
    // Check if map container exists
    const mapContainer = document.getElementById('googleMap');
    if (!mapContainer) {
        console.log("Map container not found during initialization");
        return;
    }

    try {
        console.log("Initializing map");

        // Create map centered on Saudi Arabia
        map = new google.maps.Map(document.getElementById("googleMap"), {
            center: { lat: 24.7136, lng: 42.3528 },
            zoom: 6,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            mapTypeControl: true,
            mapTypeControlOptions: {
                style: google.maps.MapTypeControlStyle.DROPDOWN_MENU
            },
            fullscreenControl: true,
            streetViewControl: false,
            styles: [
                {
                    "featureType": "administrative",
                    "elementType": "geometry",
                    "stylers": [
                        {
                            "visibility": "off"
                        }
                    ]
                },
                {
                    "featureType": "poi",
                    "stylers": [
                        {
                            "visibility": "off"
                        }
                    ]
                },
                {
                    "featureType": "transit",
                    "stylers": [
                        {
                            "visibility": "off"
                        }
                    ]
                }
            ]
        });

        // Create an info window to share between markers
        infoWindow = new google.maps.InfoWindow();

        // Add city markers
        addCityMarkers();

        // Add event listeners for city buttons if they exist
        const cityButtons = document.querySelectorAll('.city-button');
        cityButtons.forEach(button => {
            button.addEventListener('click', function () {
                const city = this.getAttribute('data-city');
                focusCity(city);
            });
        });

        // If there's a default city parameter, use it
        const defaultCity = mapContainer.getAttribute('data-default-city');
        if (defaultCity && cities[defaultCity]) {
            setTimeout(() => focusCity(defaultCity), 500);
        }

        // Mark as initialized
        isInitialized = true;
        console.log("Map initialization complete");

    } catch (error) {
        console.error("Error initializing map:", error);
        mapContainer.innerHTML = '<div class="map-error">Error initializing map: ' + error.message + '</div>';
    }
}

/**
 * Adds city markers to the map
 */
function addCityMarkers() {
    for (const [cityId, city] of Object.entries(cities)) {
        try {
            const marker = new google.maps.Marker({
                position: city.position,
                map: map,
                title: city.name,
                icon: {
                    path: google.maps.SymbolPath.CIRCLE,
                    scale: 10,
                    fillColor: "#1a3b29",
                    fillOpacity: 0.8,
                    strokeWeight: 2,
                    strokeColor: "#ffffff"
                },
                cityId: cityId
            });

            // Add click event to city marker
            marker.addListener("click", () => {
                // Navigate to city page - converted to lowercase for correct URL
                const cityIdLower = cityId.toLowerCase();
                window.location.href = `/City/Explore/${cityIdLower}`;
            });

            // Add mouseover event to show city info
            marker.addListener("mouseover", () => {
                const content = `
                    <div class="map-info-window">
                        <h3>${city.name}</h3>
                        <p>${city.description}</p>
                        <a href="/City/Explore/${cityId.toLowerCase()}" class="btn btn-primary btn-sm">Explore ${city.name}</a>
                    </div>
                `;
                infoWindow.setContent(content);
                infoWindow.open(map, marker);
            });

            marker.addListener("mouseout", () => {
                infoWindow.close();
            });

            markers.push(marker);
        } catch (error) {
            console.error(`Error adding marker for city ${cityId}:`, error);
        }
    }
}

/**
 * Focuses the map on a specific city and displays its historical sites
 * @param {string} cityId - The ID of the city to focus on
 */
function focusCity(cityId) {
    if (!cities[cityId]) {
        console.error(`City not found: ${cityId}`);
        return;
    }

    try {
        console.log(`Focusing on city: ${cityId}`);

        // Clear existing site markers
        clearSiteMarkers();

        // Set active city
        activeCity = cityId;

        // Update city buttons active state
        const cityButtons = document.querySelectorAll('.city-button');
        cityButtons.forEach(button => {
            if (button.getAttribute('data-city') === cityId) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });

        // Pan to city
        map.panTo(cities[cityId].position);
        map.setZoom(cities[cityId].zoom);

        // Add site markers for the selected city
        addSiteMarkers(cityId);

    } catch (error) {
        console.error(`Error focusing city ${cityId}:`, error);
    }
}

/**
 * Clears all historical site markers from the map
 */
function clearSiteMarkers() {
    try {
        markers.forEach(marker => {
            if (marker.siteId) {
                marker.setMap(null);
            }
        });

        // Filter out site markers
        markers = markers.filter(marker => !marker.siteId);
    } catch (error) {
        console.error("Error clearing site markers:", error);
    }
}

/**
 * Adds historical site markers for a specific city
 * @param {string} cityId - The ID of the city
 */
function addSiteMarkers(cityId) {
    const sites = historicalSites[cityId];
    if (!sites) {
        console.error(`No sites found for city: ${cityId}`);
        return;
    }

    sites.forEach(site => {
        try {
            const marker = new google.maps.Marker({
                position: site.position,
                map: map,
                title: site.name,
                icon: {
                    path: google.maps.SymbolPath.BACKWARD_CLOSED_ARROW,
                    scale: 5,
                    fillColor: "#f8c15c",
                    fillOpacity: 1,
                    strokeWeight: 2,
                    strokeColor: "#1a3b29"
                },
                animation: google.maps.Animation.DROP,
                siteId: site.id
            });

            // Add click event to navigate to site page
            marker.addListener("click", () => {
                // Use the dbId property for correct navigation
                window.location.href = `/City/Site/${site.dbId}`;
            });

            // Add hover event to show site info
            marker.addListener("mouseover", () => {
                const content = `
                    <div class="map-info-window">
                        <div class="site-image">
                            <img src="${site.image}" alt="${site.name}" onerror="this.src='/api/placeholder/150/100'">
                        </div>
                        <div class="site-info">
                            <h3>${site.name}</h3>
                            <p>${site.description}</p>
                            <p><strong>Established:</strong> ${site.year}</p>
                            <a href="/City/Site/${site.dbId}" class="btn btn-primary btn-sm">View Details</a>
                        </div>
                    </div>
                `;
                infoWindow.setContent(content);
                infoWindow.open(map, marker);
            });

            markers.push(marker);
        } catch (error) {
            console.error(`Error adding marker for site ${site.name}:`, error);
        }
    });
}

// Initialize maps when the page loads
document.addEventListener('DOMContentLoaded', function () {
    console.log("DOM loaded, loading Google Maps API");

    // Add error handling for the entire script
    try {
        loadGoogleMapsApi();
    } catch (error) {
        console.error("Critical error in map initialization:", error);
        const mapContainer = document.getElementById('googleMap');
        if (mapContainer) {
            mapContainer.innerHTML = '<div class="map-error">Failed to initialize map. Please refresh the page or try again later.</div>';
        }
    }
});

// Make initMap globally available for Google Maps callback
window.initMap = initMap;