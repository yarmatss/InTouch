/**
 * InTouch SignalR Chat Client
 * Handles real-time messaging functionality
 */

// Main SignalR connection management
class ChatConnection {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.isTyping = false;
        this.typingTimeout = null;
        this.reconnectAttempts = 0;
    }

    // Initialize connection to SignalR hub
    async initialize() {
        try {
            // Build the connection with robust error handling
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/chathub", {
                    // Include credentials for authentication
                    withCredentials: true
                })
                .withAutomaticReconnect([0, 1000, 5000, 10000, 30000]) // Reconnection intervals
                .configureLogging(signalR.LogLevel.Debug)  // Debug-level logging during development
                .build();

            // Set up message handling
            this.setupMessageHandlers();

            // Start the connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log("SignalR connection established successfully");

            return true;
        } catch (error) {
            console.error("Error establishing SignalR connection:", error);

            // Show detailed connection error
            const errorDiv = document.getElementById('connectionStatus');
            if (errorDiv) {
                errorDiv.textContent = `Connection error: ${error.message || "Could not connect to server"}`;
                errorDiv.style.display = 'block';
                errorDiv.className = 'alert alert-danger';
            }

            return false;
        }
    }

    // Set up all message handler registrations
    setupMessageHandlers() {
        // Handle received messages with detailed logging
        this.connection.on("ReceiveMessage", (messageId, senderId, message, sentAt) => {
            console.log("Message received:", { messageId, senderId, message, sentAt });
            this.handleReceivedMessage(messageId, senderId, message, sentAt);
        });

        // Handle message sent confirmation with logging
        this.connection.on("MessageSent", (messageId, message, sentAt) => {
            console.log("Message sent confirmation:", { messageId, message, sentAt });
            this.handleMessageSent(messageId, message, sentAt);
        });

        // Handle read receipts
        this.connection.on("MessageRead", (messageId) => {
            console.log("Message marked as read:", messageId);
            this.handleMessageRead(messageId);
        });

        // Handle typing indicators
        this.connection.on("UserTyping", (userId, isTyping) => {
            console.log(`User ${userId} typing status: ${isTyping}`);
            this.handleUserTyping(userId, isTyping);
        });

        // Handle user connection state
        this.connection.on("UserConnected", userId => {
            console.log("User connected:", userId);
            this.handleUserConnected(userId);
        });

        this.connection.on("UserDisconnected", userId => {
            console.log("User disconnected:", userId);
            this.handleUserDisconnected(userId);
        });

        // Error handling
        this.connection.on("error", error => {
            console.error("SignalR error:", error);
        });

        // Handle connection lost/reconnecting
        this.connection.onreconnecting(error => {
            console.log("Reconnecting to SignalR hub...", error);
            this.isConnected = false;
            this.reconnectAttempts++;
            this.handleConnectionStatus("reconnecting");
        });

        // Handle connection reestablished
        this.connection.onreconnected(connectionId => {
            console.log("Reconnected to SignalR hub", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.handleConnectionStatus("connected");
        });

        // Handle complete connection closed
        this.connection.onclose(error => {
            console.log("Connection closed", error);
            this.isConnected = false;
            this.handleConnectionStatus("disconnected");
        });
    }

    // Send a message to another user
    async sendMessage(receiverId, message) {
        if (!this.isConnected || !message.trim()) return false;

        try {
            console.log(`Sending message to ${receiverId}: ${message}`);
            await this.connection.invoke("SendMessage", receiverId, message);

            // Stop typing indicator after sending message
            this.sendTypingStop(receiverId);
            return true;
        } catch (error) {
            // Show more detailed error
            console.error("Error sending message:", error);

            // Add useful error details
            if (error.message) {
                const errorDiv = document.getElementById('connectionStatus');
                if (errorDiv) {
                    errorDiv.textContent = `Error: ${error.message}`;
                    errorDiv.style.display = 'block';
                    errorDiv.className = 'alert alert-danger';

                    // Auto-hide after 5 seconds
                    setTimeout(() => {
                        errorDiv.style.display = 'none';
                    }, 5000);
                }
            }

            return false;
        }
    }

    // Mark message as read
    async markMessageAsRead(messageId) {
        if (!this.isConnected || !messageId) return;

        try {
            console.log("Marking message as read:", messageId);
            await this.connection.invoke("MarkAsRead", parseInt(messageId));
        } catch (error) {
            console.error("Error marking message as read:", error);
        }
    }

    // Send typing indicator start
    async sendTypingStart(receiverId) {
        if (!this.isConnected || this.isTyping || !receiverId) return;

        try {
            this.isTyping = true;
            await this.connection.invoke("TypingStart", receiverId);

            // Clear existing timeout
            if (this.typingTimeout) {
                clearTimeout(this.typingTimeout);
            }

            // Set a new timeout to stop the typing indicator after some inactivity
            this.typingTimeout = setTimeout(() => {
                this.sendTypingStop(receiverId);
            }, 3000);
        } catch (error) {
            console.error("Error sending typing indicator:", error);
        }
    }

    // Send typing indicator stop
    async sendTypingStop(receiverId) {
        if (!this.isConnected || !this.isTyping || !receiverId) return;

        try {
            this.isTyping = false;
            await this.connection.invoke("TypingStop", receiverId);

            if (this.typingTimeout) {
                clearTimeout(this.typingTimeout);
                this.typingTimeout = null;
            }
        } catch (error) {
            console.error("Error stopping typing indicator:", error);
        }
    }

    // Event handler functions - override these in the UI layer

    handleReceivedMessage(messageId, senderId, message, sentAt) {
        console.log(`Received message from ${senderId}: ${message}`);
        // Override this in UI layer
    }

    handleMessageSent(messageId, message, sentAt) {
        console.log(`Message sent successfully: ${message} (ID: ${messageId})`);
        // Override this in UI layer
    }

    handleMessageRead(messageId) {
        console.log(`Message ${messageId} was read`);
        // Override this in UI layer
    }

    handleUserTyping(userId, isTyping) {
        console.log(`User ${userId} is ${isTyping ? 'typing' : 'not typing'}`);
        // Override this in UI layer
    }

    handleUserConnected(userId) {
        console.log(`User ${userId} connected`);
        // Override this in UI layer
    }

    handleUserDisconnected(userId) {
        console.log(`User ${userId} disconnected`);
        // Override this in UI layer
    }

    handleConnectionStatus(status) {
        console.log(`Connection status: ${status}`);
        // Override this in UI layer
    }
}

// UI-specific chat functionality
class ChatUI {
    constructor(receiverId) {
        this.chatConnection = new ChatConnection();
        this.receiverId = receiverId;
        this.messageContainer = document.getElementById('messageContainer');
        this.messageForm = document.getElementById('messageForm');
        this.messageInput = document.getElementById('messageInput');
        this.typingIndicator = document.getElementById('typingIndicator');
        this.timestampUpdateInterval = setInterval(this.updateTimestamps.bind(this), 60000); // Update every minute
    }

    async initialize() {
        console.log(`Initializing chat UI for receiver: ${this.receiverId}`);

        // Initialize connection
        const connected = await this.chatConnection.initialize();
        if (!connected) {
            this.showErrorMessage("Failed to connect to chat service. Please refresh the page.");
            return false;
        }

        // Override event handlers
        this.chatConnection.handleReceivedMessage = this.handleReceivedMessage.bind(this);
        this.chatConnection.handleMessageRead = this.handleMessageRead.bind(this);
        this.chatConnection.handleUserTyping = this.handleUserTyping.bind(this);
        this.chatConnection.handleUserConnected = this.handleUserConnected.bind(this);
        this.chatConnection.handleUserDisconnected = this.handleUserDisconnected.bind(this);
        this.chatConnection.handleConnectionStatus = this.handleConnectionStatus.bind(this);
        this.chatConnection.handleMessageSent = this.handleMessageSent.bind(this);

        // Set up UI event listeners
        this.setupEventListeners();

        // Mark all visible messages as read
        this.markVisibleMessagesAsRead();

        // Scroll to bottom of messages
        this.scrollToBottom();

        return true;
    }

    setupEventListeners() {
        // Message form submission
        this.messageForm.addEventListener('submit', this.handleSubmit.bind(this));

        // Typing indicator
        this.messageInput.addEventListener('input', this.handleInput.bind(this));

        // Mark messages as read when they become visible
        this.setupIntersectionObserver();

        // Page unload handler to clean up
        window.addEventListener('beforeunload', () => {
            this.cleanup();
        });
    }

    async handleSubmit(event) {
        event.preventDefault();

        const message = this.messageInput.value.trim();
        if (!message) return;

        // Clear input immediately for better UX
        const messageText = message; // Save a copy before clearing
        this.messageInput.value = '';
        this.messageInput.focus();

        console.log(`Submitting message: ${messageText}`);

        // Send the message but don't add to UI yet - wait for server confirmation
        const sent = await this.chatConnection.sendMessage(this.receiverId, messageText);

        if (!sent) {
            // Only show error if message failed
            this.showErrorMessage("Failed to send message. Please try again.");
            // Optionally restore the input text to let them try again
            this.messageInput.value = messageText;
        }

        // DO NOT add message to UI here - we wait for server confirmation via handleMessageSent
    }

    handleInput() {
        const message = this.messageInput.value.trim();

        if (message) {
            this.chatConnection.sendTypingStart(this.receiverId);
        } else {
            this.chatConnection.sendTypingStop(this.receiverId);
        }
    }

    handleMessageSent(messageId, message, sentAt) {
        // This now adds the message to UI only once, when server confirms it was sent
        console.log(`Message sent confirmation received: ${messageId}`);
        this.addMessageToUI(message, new Date(sentAt), true, messageId);
    }

    formatTimestamp(time) {
        const now = new Date();
        const messageDate = new Date(time);
        const diffMs = now - messageDate;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);

        // For messages from today
        if (messageDate.toDateString() === now.toDateString()) {
            // Very recent messages (less than 1 minute ago)
            if (diffMins < 1) {
                return 'Just now';
            }
            // Recent messages (less than 1 hour ago)
            if (diffHours < 1) {
                return `${diffMins} min${diffMins !== 1 ? 's' : ''} ago`;
            }
            // Messages from today but over an hour ago
            return messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }
        // For messages from yesterday 
        else if (diffDays === 1) {
            return `Yesterday, ${messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        }
        // For messages from this week (less than 7 days ago)
        else if (diffDays < 7) {
            const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
            return `${days[messageDate.getDay()]}, ${messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
        }
        // For older messages
        else {
            return messageDate.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: messageDate.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
            }) + ' ' + messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }
    }

    updateTimestamps() {
        if (!this.messageContainer) return;

        const recentMessages = this.messageContainer.querySelectorAll('.message');

        recentMessages.forEach(message => {
            const timeElement = message.querySelector('.message-time');
            if (!timeElement) return;

            const fullTimestamp = timeElement.getAttribute('title');

            if (fullTimestamp) {
                const messageTime = new Date(fullTimestamp);
                const formattedTime = this.formatTimestamp(messageTime);

                // Update only the text part, not the read receipt icon
                const readReceipt = timeElement.querySelector('span');
                if (readReceipt) {
                    timeElement.innerHTML = formattedTime + ' ';
                    timeElement.appendChild(readReceipt);
                } else {
                    timeElement.textContent = formattedTime;
                }
            }
        });
    }

    cleanup() {
        if (this.timestampUpdateInterval) {
            clearInterval(this.timestampUpdateInterval);
        }

        // Remove the SignalR connection status interval
        if (window.checkSignalRStatus) {
            clearInterval(window.checkSignalRStatus);
        }
    }

    addMessageToUI(content, time, isSent, messageId = null) {
        // Check if we need to add a date header
        const messageDate = new Date(time).toDateString();
        const dateHeaders = Array.from(this.messageContainer.querySelectorAll('.date-header'));
        const needsDateHeader = !dateHeaders.some(header =>
            header.textContent.includes(new Date(time).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }))
        );

        if (needsDateHeader) {
            const fullDate = new Date(time).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
            const dateHeaderElement = document.createElement('div');
            dateHeaderElement.className = 'text-center my-3';
            dateHeaderElement.innerHTML = `<span class="badge bg-light text-dark date-header">${fullDate}</span>`;
            this.messageContainer.appendChild(dateHeaderElement);
        }

        // Format the time
        const formattedTime = this.formatTimestamp(time);

        // Add tooltip with precise timestamp for hover effect
        const fullTimestamp = new Date(time).toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });

        // Create message element
        const messageDiv = document.createElement('div');
        messageDiv.className = `d-flex ${isSent ? 'justify-content-end' : 'justify-content-start'} mb-2`;

        const statusIcon = isSent ? '<i class="bi bi-check2"></i>' : '';

        messageDiv.innerHTML = `
        <div class="message ${isSent ? 'message-sent' : 'message-received'}" ${messageId ? `data-id="${messageId}"` : ''}>
            <div class="message-content">${this.escapeHtml(content)}</div>
            <div class="message-time" title="${fullTimestamp}">
                ${formattedTime}
                ${isSent ? `<span class="ms-1">${statusIcon}</span>` : ''}
            </div>
        </div>
        `;

        this.messageContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }

    scrollToBottom() {
        if (this.messageContainer) {
            this.messageContainer.scrollTop = this.messageContainer.scrollHeight;
        }
    }

    setupIntersectionObserver() {
        if (!('IntersectionObserver' in window)) {
            // Fallback for browsers without IntersectionObserver support
            this.markAllMessagesAsRead();
            return;
        }

        // Use Intersection Observer to detect when messages come into view
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const messageElement = entry.target;
                    const messageId = messageElement.dataset.id;

                    if (messageId && !messageElement.classList.contains('message-sent')) {
                        this.chatConnection.markMessageAsRead(messageId);
                        observer.unobserve(messageElement);
                    }
                }
            });
        }, { threshold: 0.5 });

        // Observe all unread received messages
        document.querySelectorAll('.message-received[data-id]').forEach(message => {
            observer.observe(message);
        });
    }

    markAllMessagesAsRead() {
        // Fallback method to mark all messages as read
        document.querySelectorAll('.message-received[data-id]').forEach(message => {
            const messageId = message.dataset.id;
            if (messageId) {
                this.chatConnection.markMessageAsRead(messageId);
            }
        });
    }

    markVisibleMessagesAsRead() {
        // Mark all currently visible received messages as read
        document.querySelectorAll('.message-received[data-id]').forEach(message => {
            const messageId = message.dataset.id;
            if (messageId) {
                this.chatConnection.markMessageAsRead(messageId);
            }
        });
    }

    // Escape HTML to prevent XSS
    escapeHtml(unsafe) {
        if (!unsafe) return '';

        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    showErrorMessage(message) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger';
        errorDiv.textContent = message;

        // Insert at top of message container
        if (this.messageContainer) {
            this.messageContainer.prepend(errorDiv);

            // Auto remove after 5 seconds
            setTimeout(() => {
                errorDiv.remove();
            }, 5000);
        }
    }

    // Event handler implementations

    handleReceivedMessage(messageId, senderId, message, sentAt) {
        if (senderId === this.receiverId) {
            console.log(`Displaying received message: ${messageId} from ${senderId}`);
            this.addMessageToUI(message, sentAt, false, messageId);
            this.chatConnection.markMessageAsRead(messageId);
        } else {
            // Handle notification for message from someone else
            this.showNotification(senderId, message);
        }
    }

    handleMessageRead(messageId) {
        // Update read status for message
        console.log(`Updating read status for message: ${messageId}`);
        const messageElement = document.querySelector(`.message[data-id="${messageId}"] .message-time i`);
        if (messageElement) {
            messageElement.className = "bi bi-check2-all text-primary";
        }
    }

    handleUserTyping(userId, isTyping) {
        if (userId === this.receiverId) {
            this.typingIndicator.style.display = isTyping ? 'block' : 'none';
        }
    }

    handleUserConnected(userId) {
        if (userId === this.receiverId) {
            const userStatus = document.getElementById('userStatus');
            if (userStatus) {
                userStatus.innerHTML = '<span class="text-success">Online</span>';
            }
        }
    }

    handleUserDisconnected(userId) {
        if (userId === this.receiverId) {
            const userStatus = document.getElementById('userStatus');
            if (userStatus) {
                userStatus.innerHTML = '<span class="text-muted">Offline</span>';
            }
        }
    }

    handleConnectionStatus(status) {
        const statusBar = document.getElementById('connectionStatus');
        if (!statusBar) return;

        if (status === 'reconnecting') {
            statusBar.textContent = 'Reconnecting...';
            statusBar.classList.add('alert', 'alert-warning');
            statusBar.style.display = 'block';
        } else if (status === 'connected') {
            statusBar.textContent = 'Connected';
            statusBar.classList.remove('alert-warning', 'alert-danger');
            statusBar.classList.add('alert-success');

            // Hide after 2 seconds
            setTimeout(() => {
                statusBar.style.display = 'none';
            }, 2000);
        } else if (status === 'disconnected') {
            statusBar.textContent = 'Disconnected. Please refresh the page.';
            statusBar.classList.remove('alert-warning', 'alert-success');
            statusBar.classList.add('alert-danger');
            statusBar.style.display = 'block';
        }
    }

    showNotification(senderId, message) {
        // Only show if browser notifications are supported
        if (!("Notification" in window)) return;

        // Check if permission is already granted
        if (Notification.permission === "granted") {
            new Notification("New message from " + senderId, {
                body: message,
                icon: "/images/logo.png"
            });
        }
    }
}

// Utility functions

// Request notification permission on application startup
function requestNotificationPermission() {
    if (!("Notification" in window)) return;

    if (Notification.permission !== "granted" && Notification.permission !== "denied") {
        Notification.requestPermission();
    }
}

// Initialize chat when document is ready
document.addEventListener('DOMContentLoaded', function () {
    // Request notification permission
    requestNotificationPermission();

    // Initialize chat if we're on a conversation page
    const receiverIdElement = document.getElementById('receiverId');
    if (receiverIdElement) {
        const receiverId = receiverIdElement.value;
        const chatUI = new ChatUI(receiverId);
        chatUI.initialize();
    }
});