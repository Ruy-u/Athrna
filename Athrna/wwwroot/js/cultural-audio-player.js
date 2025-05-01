/**
 * Cultural Info Audio Player Component
 * Provides audio playback for cultural information sections
 */

// Add at the top of the DOMContentLoaded event handler
document.addEventListener('DOMContentLoaded', function () {
    console.log("Audio player script loaded");

    // Find all cultural info sections that need audio players
    const culturalInfoSections = document.querySelectorAll('.cultural-info-section');
    console.log("Found cultural info sections:", culturalInfoSections.length);

    culturalInfoSections.forEach(section => {
        const culturalInfoId = section.getAttribute('data-cultural-info-id');
        const siteName = section.getAttribute('data-site-name');
        console.log("Found section with ID:", culturalInfoId, "Name:", siteName);

        // Rest of the code...
    });
});
class CulturalInfoAudioPlayer {
    constructor(containerId, culturalInfoId, siteName) {
        this.container = document.getElementById(containerId);
        this.culturalInfoId = culturalInfoId;
        this.siteName = siteName;
        this.audioUrl = null;
        this.audioElement = null;
        this.isPlaying = false;
        this.isLoading = false;

        // Initialize the player
        this.init();
    }

    init() {
        if (!this.container) {
            console.error('Audio player container not found');
            return;
        }

        // Create player UI
        this.createPlayerUI();

        // Set up event listeners
        this.setupEventListeners();
    }

    createPlayerUI() {
        // Create audio player container
        const playerContainer = document.createElement('div');
        playerContainer.className = 'cultural-audio-player';

        // Create audio element
        this.audioElement = document.createElement('audio');
        this.audioElement.id = `audio-${this.culturalInfoId}`;
        playerContainer.appendChild(this.audioElement);

        // Create controls
        const controlsContainer = document.createElement('div');
        controlsContainer.className = 'audio-controls';

        // Play/pause button
        this.playPauseButton = document.createElement('button');
        this.playPauseButton.className = 'btn btn-outline-primary btn-sm audio-btn play-btn';
        this.playPauseButton.innerHTML = '<i class="bi bi-play-fill"></i> Listen';
        this.playPauseButton.setAttribute('aria-label', 'Play audio description');
        controlsContainer.appendChild(this.playPauseButton);

        // Progress container
        const progressContainer = document.createElement('div');
        progressContainer.className = 'audio-progress d-none';

        // Progress bar
        const progressWrapper = document.createElement('div');
        progressWrapper.className = 'progress';
        progressWrapper.style.height = '4px';

        this.progressBar = document.createElement('div');
        this.progressBar.className = 'progress-bar';
        this.progressBar.style.width = '0%';
        this.progressBar.setAttribute('role', 'progressbar');
        this.progressBar.setAttribute('aria-valuenow', '0');
        this.progressBar.setAttribute('aria-valuemin', '0');
        this.progressBar.setAttribute('aria-valuemax', '100');

        progressWrapper.appendChild(this.progressBar);
        progressContainer.appendChild(progressWrapper);

        // Time display
        this.timeDisplay = document.createElement('small');
        this.timeDisplay.className = 'text-muted time-display';
        this.timeDisplay.textContent = '0:00 / 0:00';
        progressContainer.appendChild(this.timeDisplay);

        controlsContainer.appendChild(progressContainer);

        // Loading indicator
        this.loadingIndicator = document.createElement('div');
        this.loadingIndicator.className = 'spinner-border spinner-border-sm text-primary d-none';
        this.loadingIndicator.setAttribute('role', 'status');

        const loadingText = document.createElement('span');
        loadingText.className = 'visually-hidden';
        loadingText.textContent = 'Loading...';

        this.loadingIndicator.appendChild(loadingText);
        controlsContainer.appendChild(this.loadingIndicator);

        // Add controls to player
        playerContainer.appendChild(controlsContainer);

        // Add player to container
        this.container.appendChild(playerContainer);
    }

    setupEventListeners() {
        // Play/pause button click
        this.playPauseButton.addEventListener('click', () => {
            if (this.isLoading) return;

            if (!this.audioUrl) {
                this.loadAudio();
                return;
            }

            if (this.isPlaying) {
                this.pause();
            } else {
                this.play();
            }
        });

        // Audio events
        this.audioElement.addEventListener('timeupdate', () => this.updateProgress());
        this.audioElement.addEventListener('ended', () => this.onEnded());
        this.audioElement.addEventListener('canplaythrough', () => this.onCanPlay());
        this.audioElement.addEventListener('error', (e) => this.onError(e));
    }

    loadAudio() {
        this.isLoading = true;
        this.showLoading(true);

        // Construct request URL
        const url = `/CulturalInfo/GetAudio?siteId=${this.culturalInfoId}&siteName=${encodeURIComponent(this.siteName)}`;

        // Make API call to get audio URL
        fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to load audio');
                }
                return response.json();
            })
            .then(data => {
                this.audioUrl = data.audioUrl;
                this.audioElement.src = this.audioUrl;
                this.audioElement.load();

                // Auto-play once loaded
                this.audioElement.play()
                    .then(() => {
                        this.isPlaying = true;
                        this.updatePlayPauseButton();
                    })
                    .catch(err => {
                        console.error('Error playing audio:', err);
                        this.showLoading(false);
                    });
            })
            .catch(error => {
                console.error('Error loading audio:', error);
                this.showError();
            });
    }

    play() {
        if (!this.audioElement || !this.audioUrl) return;

        this.audioElement.play()
            .then(() => {
                this.isPlaying = true;
                this.updatePlayPauseButton();

                // Show progress container
                const progressContainer = this.container.querySelector('.audio-progress');
                progressContainer.classList.remove('d-none');
            })
            .catch(err => {
                console.error('Error playing audio:', err);
            });
    }

    pause() {
        if (!this.audioElement) return;

        this.audioElement.pause();
        this.isPlaying = false;
        this.updatePlayPauseButton();
    }

    updatePlayPauseButton() {
        if (this.isPlaying) {
            this.playPauseButton.innerHTML = '<i class="bi bi-pause-fill"></i> Pause';
            this.playPauseButton.classList.remove('play-btn');
            this.playPauseButton.classList.add('pause-btn');
        } else {
            this.playPauseButton.innerHTML = '<i class="bi bi-play-fill"></i> Listen';
            this.playPauseButton.classList.remove('pause-btn');
            this.playPauseButton.classList.add('play-btn');
        }
    }

    updateProgress() {
        if (!this.audioElement) return;

        const currentTime = this.audioElement.currentTime;
        const duration = this.audioElement.duration || 0;

        // Update progress bar
        const progress = (currentTime / duration) * 100;
        this.progressBar.style.width = `${progress}%`;
        this.progressBar.setAttribute('aria-valuenow', progress);

        // Update time display
        this.timeDisplay.textContent = `${this.formatTime(currentTime)} / ${this.formatTime(duration)}`;
    }

    formatTime(seconds) {
        const minutes = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${minutes}:${secs.toString().padStart(2, '0')}`;
    }

    onEnded() {
        this.isPlaying = false;
        this.updatePlayPauseButton();
    }

    onCanPlay() {
        this.isLoading = false;
        this.showLoading(false);

        // Show progress container
        const progressContainer = this.container.querySelector('.audio-progress');
        progressContainer.classList.remove('d-none');
    }

    onError(e) {
        console.error('Audio error:', e);
        this.isLoading = false;
        this.showLoading(false);
        this.showError();
    }

    showLoading(show) {
        if (show) {
            this.loadingIndicator.classList.remove('d-none');
            this.playPauseButton.classList.add('d-none');
        } else {
            this.loadingIndicator.classList.add('d-none');
            this.playPauseButton.classList.remove('d-none');
        }
    }

    showError() {
        this.playPauseButton.innerHTML = '<i class="bi bi-exclamation-triangle"></i> Error';
        this.playPauseButton.classList.remove('btn-outline-primary');
        this.playPauseButton.classList.add('btn-outline-danger');
        this.playPauseButton.disabled = true;
    }
}

// Initialize audio players on the page
document.addEventListener('DOMContentLoaded', function () {
    // Find all cultural info sections that need audio players
    const culturalInfoSections = document.querySelectorAll('.cultural-info-section');

    culturalInfoSections.forEach(section => {
        const culturalInfoId = section.getAttribute('data-cultural-info-id');
        const siteName = section.getAttribute('data-site-name');
        const containerId = `cultural-audio-${culturalInfoId}`;

        // Create audio player container if it doesn't exist
        if (!document.getElementById(containerId)) {
            const audioContainer = document.createElement('div');
            audioContainer.id = containerId;
            audioContainer.className = 'cultural-audio-container mt-3';
            section.appendChild(audioContainer);

            // Initialize the audio player
            new CulturalInfoAudioPlayer(containerId, culturalInfoId, siteName);
        }
    });
});