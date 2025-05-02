class ChatSystem {
    constructor(options = {}) {
        // DOM Elements
        this.messageFormId = options.messageFormId || 'messageForm';
        this.messageInputId = options.messageInputId || 'messageInput';
        this.messageListId = options.messageListId || 'messageList';
        this.guideIdFieldId = options.guideIdFieldId || 'guideId';
        this.messageTemplateId = options.messageTemplateId || 'message-template';

        // Settings
        this.pollingInterval = options.pollingInterval || 5000; // 5 seconds by default
        this.maxRetries = options.maxRetries || 3;
        this.csrfTokenName = options.csrfTokenName || '__RequestVerificationToken';

        // State
        this.isProcessing = false;
        this.retryCount = 0;
        this.pollingTimerId = null;
        this.lastMessageId = 0;

        // Initialize
        this.init();
    }

    init() {
        // Get DOM elements
        this.messageForm = document.getElementById(this.messageFormId);
        this.messageInput = document.getElementById(this.messageInputId);
        this.messageList = document.getElementById(this.messageListId);
        this.guideIdField = document.getElementById(this.guideIdFieldId);
        this.messageTemplate = document.getElementById(this.messageTemplateId);

        // Validate required elements
        if (!this.messageForm || !this.messageInput || !this.messageList || !this.guideIdField) {
            console.error('Required chat elements not found');
            return;
        }

        // Set up events
        this.setupEventListeners();

        // Initial scroll to bottom
        this.scrollToBottom();

        // Start polling for new messages
        this.startMessagePolling();
    }

    setupEventListeners() {
        // Form submission
        this.messageForm.addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleSendMessage();
        });

        // Listen for Enter key (without Shift)
        this.messageInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.handleSendMessage();
            }
        });

        // Auto-resize textarea as user types
        if (this.messageInput.tagName.toLowerCase() === 'textarea') {
            this.messageInput.addEventListener('input', () => {
                this.messageInput.style.height = 'auto';
                this.messageInput.style.height = (this.messageInput.scrollHeight) + 'px';
            });
        }

        // Window visibility change (pause/resume polling)
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                this.startMessagePolling();
            } else {
                this.stopMessagePolling();
            }
        });
    }

    handleSendMessage() {
        const message = this.messageInput.value.trim();
        if (!message || this.isProcessing) return;

        const guideId = this.guideIdField.value;
        if (!guideId) {
            console.error('Guide ID not found');
            return;
        }

        // Add message to UI
        this.addMessageToUI(message, true);

        // Clear input
        this.messageInput.value = '';
        if (this.messageInput.tagName.toLowerCase() === 'textarea') {
            this.messageInput.style.height = 'auto';
        }

        // Focus on input
        this.messageInput.focus();

        // Send to server
        this.sendMessageToServer(guideId, message);
    }

    addMessageToUI(text, isSent = true) {
        // Create message element based on template (if available) or build from scratch
        let messageElement;

        if (this.messageTemplate) {
            // Use the template
            const clone = this.messageTemplate.content.cloneNode(true);
            messageElement = clone.querySelector('.message-item');
        } else {
            // Create element from scratch
            messageElement = document.createElement('div');
            messageElement.className = 'message-item';

            const messageContent = document.createElement('div');
            messageContent.className = 'message-content';

            const messageBubble = document.createElement('div');
            messageBubble.className = 'message-bubble';

            const messageText = document.createElement('div');
            messageText.className = 'message-text';

            const messageTime = document.createElement('div');
            messageTime.className = 'message-time';

            messageBubble.appendChild(messageText);
            messageContent.appendChild(messageBubble);
            messageContent.appendChild(messageTime);
            messageElement.appendChild(messageContent);
        }

        // Set sent/received class
        messageElement.classList.add(isSent ? 'message-sent' : 'message-received');

        // Set message text
        const messageTextEl = messageElement.querySelector('.message-text');
        if (messageTextEl) {
            messageTextEl.textContent = text;
        }

        // Set time
        const messageTimeEl = messageElement.querySelector('.message-time');
        if (messageTimeEl) {
            messageTimeEl.textContent = this.formatTime(new Date());
        }

        // Add avatar for received messages
        if (!isSent && !messageElement.querySelector('.message-avatar')) {
            const avatarText = document.querySelector('.avatar-text');
            if (avatarText) {
                const messageAvatar = document.createElement('div');
                messageAvatar.className = 'message-avatar';

                const avatarCircle = document.createElement('div');
                avatarCircle.className = 'avatar-circle avatar-small';

                const newAvatarText = document.createElement('span');
                newAvatarText.className = 'avatar-text';
                newAvatarText.textContent = avatarText.textContent;

                avatarCircle.appendChild(newAvatarText);
                messageAvatar.appendChild(avatarCircle);

                // Insert at beginning
                messageElement.insertBefore(messageAvatar, messageElement.firstChild);
            }
        }

        // Add to message list
        this.messageList.appendChild(messageElement);

        // Scroll to bottom
        this.scrollToBottom();
    }

    formatTime(date) {
        return date.toLocaleString('en-US', {
            hour: 'numeric',
            minute: 'numeric',
            hour12: true,
            month: 'short',
            day: '2-digit'
        });
    }

    scrollToBottom() {
        if (this.messageList) {
            this.messageList.scrollTop = this.messageList.scrollHeight;
        }
    }

    getCSRFToken() {
        return document.querySelector(`input[name="${this.csrfTokenName}"]`)?.value;
    }

    sendMessageToServer(guideId, content) {
        const token = this.getCSRFToken();
        if (!token) {
            console.error('CSRF token not found');
            this.showError('Security token not found. Please refresh the page.');
            return;
        }

        this.isProcessing = true;

        // Create form data
        const formData = new FormData();
        formData.append('guideId', guideId);
        formData.append('content', content);

        // Send request
        fetch('/Message/SendToGuide', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': token
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Server responded with ${response.status}: ${response.statusText}`);
                }
                return response.json();
            })
            .then(data => {
                this.retryCount = 0;

                if (!data.success) {
                    throw new Error(data.message || 'Failed to send message');
                }
            })
            .catch(error => {
                console.error('Error sending message:', error);

                if (this.retryCount < this.maxRetries) {
                    // Retry sending the message
                    this.retryCount++;
                    setTimeout(() => {
                        this.sendMessageToServer(guideId, content);
                    }, 1000 * this.retryCount); // Increasing backoff
                } else {
                    this.showError(`Failed to send message: ${error.message}`);
                    this.retryCount = 0;
                }
            })
            .finally(() => {
                this.isProcessing = false;
            });
    }

    startMessagePolling() {
        // Clear any existing timer
        this.stopMessagePolling();

        // Set up new timer
        this.pollingTimerId = setInterval(() => {
            this.checkForNewMessages();
        }, this.pollingInterval);

        // Do an immediate check
        this.checkForNewMessages();
    }

    stopMessagePolling() {
        if (this.pollingTimerId) {
            clearInterval(this.pollingTimerId);
            this.pollingTimerId = null;
        }
    }

    checkForNewMessages() {
        const guideId = this.guideIdField.value;
        if (!guideId) return;

        fetch(`/Message/GetNewMessages?guideId=${guideId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`Server responded with ${response.status}: ${response.statusText}`);
                }
                return response.json();
            })
            .then(messages => {
                if (messages && messages.length > 0) {
                    messages.forEach(message => {
                        // Add message to UI if it's new
                        if (message.id > this.lastMessageId) {
                            this.addMessageToUI(message.content, false);
                            this.lastMessageId = Math.max(this.lastMessageId, message.id);
                        }
                    });

                    // Play notification sound if available
                    this.playNotificationSound();
                }
            })
            .catch(error => {
                console.error('Error checking for new messages:', error);
            });
    }

    playNotificationSound() {
        // Check if notification sound element exists
        const sound = document.getElementById('messageNotificationSound');
        if (sound && sound.tagName.toLowerCase() === 'audio') {
            sound.play().catch(e => {
                // Autoplay might be blocked by browser
                console.log('Could not play notification sound:', e);
            });
        }
    }

    showError(message) {
        // Create error element
        const errorElement = document.createElement('div');
        errorElement.className = 'alert alert-danger mt-3';
        errorElement.textContent = message;

        // Add to DOM before the form
        this.messageForm.parentNode.insertBefore(errorElement, this.messageForm);

        // Remove after 5 seconds
        setTimeout(() => {
            errorElement.remove();
        }, 5000);
    }
}

// Initialize chat system when the DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Add notification sound element
    const soundElement = document.createElement('audio');
    soundElement.id = 'messageNotificationSound';
    soundElement.src = '/audio/samples/notification.mp3'; // Adjust path to your notification sound
    soundElement.preload = 'auto';
    document.body.appendChild(soundElement);

    // Initialize the chat system
    const chat = new ChatSystem({
        pollingInterval: 5000 // Check for new messages every 5 seconds
    });
});