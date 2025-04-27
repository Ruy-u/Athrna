// Add this script directly to your EditSite.cshtml file in the @section Scripts block

document.addEventListener('DOMContentLoaded', function () {
    // Get the date input - could be either ID depending on the form
    const dateInput = document.getElementById('CulturalInfo_EstablishedDate') ||
        document.getElementById('EstablishedDate');

    if (!dateInput) {
        console.error('Date input element not found on page');
        return;
    }

    // Get the era selector that should be next to the date input
    const eraSelector = document.getElementById('eraSelector');

    if (!eraSelector) {
        console.error('Era selector not found on page');
        return;
    }

    // Initial setup - if date is negative, it's BCE
    const initialValue = parseInt(dateInput.value) || 0;
    if (initialValue < 0) {
        // It's BCE - display as positive
        dateInput.value = Math.abs(initialValue);
        eraSelector.value = 'BCE';
    } else {
        eraSelector.value = 'CE';
    }

    // Add handler for form submission
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            // If date value exists and we've selected BCE, make it negative
            if (dateInput.value) {
                const numericValue = parseInt(dateInput.value) || 0;

                if (eraSelector.value === 'BCE') {
                    // Ensure it's negative for BCE
                    dateInput.value = -Math.abs(numericValue);
                } else {
                    // Ensure it's positive for CE
                    dateInput.value = Math.abs(numericValue);
                }

                console.log(`Converted date to ${dateInput.value} (${eraSelector.value})`);
            }
        });
    }

    console.log('BCE/CE date handling initialized');
});