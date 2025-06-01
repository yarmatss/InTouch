/**
 * InTouch SignalR Chat Client
 * Handles real-time messaging functionality
 */

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

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

            // Set up message handling (will be overridden by consumers)
            this.setupMessageHandlers();

            // IMPORTANT: Make sure all handlers are registered BEFORE starting connection
            if (this.connection) {
                // Register global handlers explicitly
                this.connection.on("UpdateConversationList",
                    (otherUserId, messageContent, sentAt, isSenderCurrentUser, newUnreadCount) => {
                        console.log("UpdateConversationList received globally");
                        if (this.handleUpdateConversationList) {
                            this.handleUpdateConversationList(otherUserId, messageContent,
                                new Date(sentAt), isSenderCurrentUser, newUnreadCount);
                        }
                    });

                this.connection.on("ClearUnreadCountForConversation", (conversationPartnerId) => {
                    console.log("ClearUnreadCountForConversation received globally");
                    if (this.handleClearUnreadCount) {
                        this.handleClearUnreadCount(conversationPartnerId);
                    }
                });
            }

            // Start the connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log("SignalR connection established successfully");

            return true;
        } catch (error) {
            console.error("Error establishing SignalR connection:", error);
            // Rest of error handling...
        }
    }

    // Method to stop the connection
    async stop() {
        if (this.connection && this.isConnected) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                console.log("SignalR connection stopped.");
            } catch (err) {
                console.error("Error stopping SignalR connection: ", err);
            }
        }
    }

    async updateLastActiveStatus() {
        if (!this.isConnected) return;
        try {
            await this.connection.invoke("UpdateLastActive");
        } catch (error) {
            console.error("Error updating last active status:", error);
        }
    }


    // Set up all message handler registrations
    // These are placeholders and should be overridden by ChatUI or ConversationsListUI
    setupMessageHandlers() {
        this.connection.on("ReceiveMessage", (messageId, senderId, message, sentAt) => {
            if (this.handleReceivedMessage) this.handleReceivedMessage(messageId, senderId, message, sentAt);
        });
        this.connection.on("MessageSent", (messageId, message, sentAt) => {
            if (this.handleMessageSent) this.handleMessageSent(messageId, message, sentAt);
        });
        this.connection.on("MessageRead", (messageId) => {
            if (this.handleMessageRead) this.handleMessageRead(messageId);
        });
        this.connection.on("UserTyping", (userId, isTyping) => {
            if (this.handleUserTyping) this.handleUserTyping(userId, isTyping);
        });
        this.connection.on("UserConnected", userId => {
            if (this.handleUserConnected) this.handleUserConnected(userId);
        });
        this.connection.on("UserDisconnected", userId => {
            if (this.handleUserDisconnected) this.handleUserDisconnected(userId);
        });
        this.connection.on("error", error => console.error("SignalR error:", error));
        this.connection.onreconnecting(error => {
            console.log("Reconnecting to SignalR hub...", error);
            this.isConnected = false;
            this.reconnectAttempts++;
            if (this.handleConnectionStatus) this.handleConnectionStatus("reconnecting");
        });
        this.connection.onreconnected(connectionId => {
            console.log("Reconnected to SignalR hub", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
            if (this.handleConnectionStatus) this.handleConnectionStatus("connected");
        });
        this.connection.onclose(error => {
            console.log("Connection closed", error);
            this.isConnected = false;
            if (this.handleConnectionStatus) this.handleConnectionStatus("disconnected");
        });
    }

    // Send a message to another user
    async sendMessage(receiverId, message) {
        if (!this.isConnected || !message.trim()) return false;
        try {
            await this.connection.invoke("SendMessage", receiverId, message);
            this.sendTypingStop(receiverId); // Assuming this method exists or is added
            return true;
        } catch (error) {
            console.error("Error sending message:", error);
            return false;
        }
    }

    // Mark message as read
    async markMessageAsRead(messageId) {
        if (!this.isConnected || !messageId) return;
        try {
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
            if (this.typingTimeout) clearTimeout(this.typingTimeout);
            this.typingTimeout = setTimeout(() => this.sendTypingStop(receiverId), 3000);
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

    // Default event handlers (can be overridden)
    handleReceivedMessage(messageId, senderId, message, sentAt) { console.log("Default: Message received", { messageId, senderId, message, sentAt }); }
    handleMessageSent(messageId, message, sentAt) { console.log("Default: Message sent", { messageId, message, sentAt }); }
    handleMessageRead(messageId) { console.log("Default: Message read", messageId); }
    handleUserTyping(userId, isTyping) { console.log("Default: User typing", { userId, isTyping }); }
    handleUserConnected(userId) { console.log("Default: User connected", userId); }
    handleUserDisconnected(userId) { console.log("Default: User disconnected", userId); }
    handleConnectionStatus(status) { console.log("Default: Connection status", status); }
}

// UI-specific chat functionality (for individual conversation page)
class ChatUI {
    constructor(receiverId) {
        this.chatConnection = new ChatConnection();
        this.receiverId = receiverId; // ID of the other user in the chat
        this.messageContainer = document.getElementById('messageContainer');
        this.messageForm = document.getElementById('messageForm');
        this.messageInput = document.getElementById('messageInput');
        this.typingIndicator = document.getElementById('typingIndicator');
        this.timestampUpdateInterval = setInterval(this.updateTimestamps.bind(this), 60000);
        this.checkUnreadInterval = null;
    }

    async initialize() {
        console.log(`Initializing chat UI for receiver: ${this.receiverId}`);
        const connected = await this.chatConnection.initialize();
        if (!connected) {
            this.showErrorMessage("Failed to connect to chat service. Please refresh the page.");
            return false;
        }

        this.chatConnection.handleReceivedMessage = this.handleReceivedMessage.bind(this);
        this.chatConnection.handleMessageRead = this.handleMessageRead.bind(this);
        this.chatConnection.handleUserTyping = this.handleUserTyping.bind(this);
        this.chatConnection.handleUserConnected = this.handleUserConnected.bind(this);
        this.chatConnection.handleUserDisconnected = this.handleUserDisconnected.bind(this);
        this.chatConnection.handleConnectionStatus = this.handleConnectionStatus.bind(this);
        this.chatConnection.handleMessageSent = this.handleMessageSent.bind(this); // For sent messages by current user

        this.setupEventListeners();

        // Start periodic last active status updates
        this.lastActiveInterval = setInterval(() => {
            this.chatConnection.updateLastActiveStatus();
        }, 30000); // Update every 30 seconds

        setTimeout(() => {
            this.markVisibleMessagesAsRead();
            this.checkUnreadInterval = setInterval(() => this.markVisibleMessagesAsRead(), 5000);
        }, 1000);
        this.scrollToBottom();
        return true;
    }

    setupEventListeners() {
        if (this.messageForm) this.messageForm.addEventListener('submit', this.handleSubmit.bind(this));
        if (this.messageInput) this.messageInput.addEventListener('input', this.handleInput.bind(this));
        this.setupIntersectionObserver();
        window.addEventListener('beforeunload', () => this.cleanup());
    }

    async handleSubmit(event) {
        event.preventDefault();
        const message = this.messageInput.value.trim();
        if (!message) return;
        const messageText = message;
        this.messageInput.value = '';
        this.messageInput.focus();
        const sent = await this.chatConnection.sendMessage(this.receiverId, messageText);
        if (!sent) {
            this.showErrorMessage("Failed to send message. Please try again.");
            this.messageInput.value = messageText;
        }
    }

    handleInput() {
        const message = this.messageInput.value.trim();
        if (message) this.chatConnection.sendTypingStart(this.receiverId);
        else this.chatConnection.sendTypingStop(this.receiverId);
    }

    // Called when the current user's message is confirmed sent by the server
    handleMessageSent(messageId, message, sentAt) {
        console.log(`Message sent confirmation received by ChatUI: ${messageId}`);
        this.addMessageToUI(message, new Date(sentAt), true, messageId);
    }

    formatTimestamp(time) {
        const userTimezone = getCookie('user_timezone') || 'UTC'; // Get user timezone from cookie
        const now = new Date();
        const messageDate = new Date(time);
        const diffMs = now - messageDate;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);

        // Use the timezone from cookie
        const timeOptions = {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
            timeZone: userTimezone
        };

        if (messageDate.toDateString() === now.toDateString()) {
            if (diffMins < 1) return 'Just now';
            if (diffHours < 1) return `${diffMins} min${diffMins !== 1 ? 's' : ''} ago`;
            return messageDate.toLocaleTimeString('en-US', timeOptions);
        }
        if (diffDays === 1) return `Yesterday, ${messageDate.toLocaleTimeString('en-US', timeOptions)}`;
        if (diffDays < 7) {
            const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
            return `${days[messageDate.getDay()]}, ${messageDate.toLocaleTimeString('en-US', timeOptions)}`;
        }

        const dateTimeOptions = {
            month: 'short',
            day: 'numeric',
            year: messageDate.getFullYear() !== now.getFullYear() ? 'numeric' : undefined,
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
            timeZone: userTimezone
        };

        return messageDate.toLocaleString('en-US', dateTimeOptions);
    }

    updateTimestamps() {
        if (!this.messageContainer) return;
        this.messageContainer.querySelectorAll('.message .message-time').forEach(timeElement => {
            const fullTimestamp = timeElement.getAttribute('title');
            if (fullTimestamp) {
                const messageTime = new Date(fullTimestamp);
                const formattedTime = this.formatTimestamp(messageTime);
                const readReceiptSpan = timeElement.querySelector('span.ms-1');
                timeElement.textContent = formattedTime + ' '; // Add space before icon
                if (readReceiptSpan) timeElement.appendChild(readReceiptSpan);
            }
        });
    }

    cleanup() {
        if (this.timestampUpdateInterval) clearInterval(this.timestampUpdateInterval);
        if (this.checkUnreadInterval) clearInterval(this.checkUnreadInterval);
        if (this.lastActiveInterval) clearInterval(this.lastActiveInterval);
        if (window.checkSignalRStatus) clearInterval(window.checkSignalRStatus);
        if (this.chatConnection) {
            this.chatConnection.stop(); // Stop the connection
        }
        console.log("ChatUI cleaned up.");
    }

    addMessageToUI(content, time, isSentByCurrentUser, messageId = null) {
        if (!this.messageContainer) return;
        const userTimezone = getCookie('user_timezone') || 'UTC';
        const messageDate = new Date(time);
        const dateOptions = { month: 'long', day: 'numeric', year: 'numeric', timeZone: userTimezone };

        const dateHeaders = Array.from(this.messageContainer.querySelectorAll('.date-header'));
        const needsDateHeader = !dateHeaders.some(header =>
            header.textContent.includes(messageDate.toLocaleDateString('en-US', dateOptions))
        );

        if (needsDateHeader) {
            const fullDate = messageDate.toLocaleDateString('en-US', dateOptions);
            const dateHeaderElement = document.createElement('div');
            dateHeaderElement.className = 'text-center my-3';
            dateHeaderElement.innerHTML = `<span class="badge bg-light text-dark date-header">${fullDate}</span>`;
            this.messageContainer.appendChild(dateHeaderElement);
        }

        const formattedTime = this.formatTimestamp(time);
        const fullTimestampOptions = {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false,
            timeZone: userTimezone
        };

        const fullTimestamp = messageDate.toLocaleString('en-US', fullTimestampOptions);

        // Rest of the method remains the same
        const messageDiv = document.createElement('div');
        messageDiv.className = `d-flex ${isSentByCurrentUser ? 'justify-content-end' : 'justify-content-start'} mb-2`;
        const statusIcon = isSentByCurrentUser ? '<i class="bi bi-check2" style="color: white;"></i>' : '';

        messageDiv.innerHTML = `
        <div class="message ${isSentByCurrentUser ? 'message-sent' : 'message-received'}" ${messageId ? `data-id="${messageId}"` : ''}>
            <div class="message-content">${this.escapeHtml(content)}</div>
            <div class="message-time" title="${fullTimestamp}">
                ${formattedTime}
                ${isSentByCurrentUser ? `<span class="ms-1">${statusIcon}</span>` : ''}
            </div>
        </div>`;
        this.messageContainer.appendChild(messageDiv);
        this.scrollToBottom();
    }


    scrollToBottom() {
        if (this.messageContainer) this.messageContainer.scrollTop = this.messageContainer.scrollHeight;
    }

    setupIntersectionObserver() {
        if (!('IntersectionObserver' in window) || !this.messageContainer) {
            this.markAllMessagesAsReadFallback(); return;
        }
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const messageElement = entry.target;
                    const messageId = messageElement.dataset.id;
                    if (messageId && messageElement.classList.contains('message-received')) { // Only mark received messages
                        this.chatConnection.markMessageAsRead(messageId);
                        observer.unobserve(messageElement);
                    }
                }
            });
        }, { threshold: 0.5 });
        this.messageContainer.querySelectorAll('.message-received[data-id]').forEach(msg => observer.observe(msg));
    }

    markAllMessagesAsReadFallback() { // Fallback if IntersectionObserver is not supported
        document.querySelectorAll('.message-received[data-id]').forEach(message => {
            const messageId = message.dataset.id;
            if (messageId) this.chatConnection.markMessageAsRead(messageId);
        });
    }

    markVisibleMessagesAsRead() { // Periodically check for visible unread messages
        if (!this.messageContainer) return;
        this.messageContainer.querySelectorAll('.message-received[data-id]').forEach(messageElement => {
            const messageId = messageElement.dataset.id;
            // Basic visibility check (can be improved)
            const rect = messageElement.getBoundingClientRect();
            const isVisible = rect.top < window.innerHeight && rect.bottom >= 0;
            if (isVisible && messageId) {
                // Check if not already marked or being observed to avoid redundant calls
                if (!messageElement.dataset.observedByIntersection) {
                    this.chatConnection.markMessageAsRead(messageId);
                }
            }
        });
    }

    escapeHtml(unsafe) {
        if (unsafe === null || typeof unsafe === 'undefined') return '';
        return unsafe.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
    }

    showErrorMessage(message) {
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger mt-2'; errorDiv.textContent = message;
        if (this.messageForm) {
            this.messageForm.prepend(errorDiv);
            setTimeout(() => errorDiv.remove(), 5000);
        } else if (this.messageContainer) {
            this.messageContainer.prepend(errorDiv);
            setTimeout(() => errorDiv.remove(), 5000);
        }
    }

    handleReceivedMessage(messageId, senderId, message, sentAt) {
        if (senderId === this.receiverId) { // Message is for the currently open chat
            this.addMessageToUI(message, new Date(sentAt), false, messageId);
            this.chatConnection.markMessageAsRead(messageId); // Mark as read since it's displayed
        } else {
            // This case should ideally be handled by ConversationsListUI for notifications if not on active chat page
            // Or show a general notification if desired
            this.showNotification("New message from " + senderId, message);
        }
    }

    handleMessageRead(messageId) { // Updates the checkmark for a sent message
        const messageElements = document.querySelectorAll(`.message-sent[data-id="${messageId}"] .message-time span.ms-1`);
        messageElements.forEach(span => {
            span.innerHTML = '<i class="bi bi-check2-all" style="color: white;"></i>';
        });
    }

    handleUserTyping(userId, isTyping) {
        if (userId === this.receiverId && this.typingIndicator) {
            this.typingIndicator.style.display = isTyping ? 'block' : 'none';
        }
    }
    handleUserConnected(userId) { /* Update UI if needed */ }
    handleUserDisconnected(userId) { /* Update UI if needed */ }
    handleConnectionStatus(status) { /* Update UI for connection status */ }
    showNotification(title, body) {
        if (!("Notification" in window) || Notification.permission !== "granted") return;
        new Notification(title, { body: body, icon: "/images/logo.png" });
    }
}

// Manages real-time updates for the conversations list on Messages/Index.cshtml
class ConversationsListUI {
    constructor(currentUserId) {
        this.currentUserId = currentUserId;
        this.conversationsListElement = document.getElementById('conversationsList');
        this.chatConnection = new ChatConnection();
    }

    async initialize() {
        if (!this.conversationsListElement || !this.currentUserId) {
            console.log("ConversationsListUI: Missing list element or currentUserId. Not initializing.");
            return;
        }
        console.log("ConversationsListUI: Initializing...");

        const connected = await this.chatConnection.initialize();
        if (!connected) {
            console.error("ConversationsListUI: Failed to connect SignalR.");
            return;
        }

        // Listen for custom events from the hub
        this.chatConnection.connection.on("UpdateConversationList",
            (otherUserId, messageContent, sentAt, isSenderCurrentUser, newUnreadCount) => {
                this.handleUpdateConversationList(otherUserId, messageContent, new Date(sentAt), isSenderCurrentUser, newUnreadCount);
            });

        this.chatConnection.connection.on("ClearUnreadCountForConversation", (conversationPartnerId) => {
            this.handleClearUnreadCount(conversationPartnerId);
        });

        window.addEventListener('beforeunload', () => this.cleanup()); // Add cleanup on page unload

        console.log("ConversationsListUI: Event listeners set up.");
    }

    cleanup() {
        if (this.chatConnection) {
            this.chatConnection.stop();
        }
        console.log("ConversationsListUI cleaned up.");
    }

    formatTimeOnly(date) {
        const userTimezone = getCookie('user_timezone') || 'UTC';
        return date.toLocaleTimeString('en-US', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
            timeZone: userTimezone
        });
    }

    handleUpdateConversationList(otherUserId, messageContent, sentAt, isSenderCurrentUser, newUnreadCount) {
        console.log("ConversationsListUI: Received UpdateConversationList", { otherUserId, messageContent, sentAt, isSenderCurrentUser, newUnreadCount });
        if (!this.conversationsListElement) return;

        let conversationItem = this.conversationsListElement.querySelector(`a.list-group-item[data-userid='${otherUserId}']`);

        if (!conversationItem) {
            console.warn(`Conversation item for user ${otherUserId} not found. Cannot update.`);
            return;
        }

        const lastMessageContentElement = conversationItem.querySelector('.last-message-content');
        const lastMessageTimeElement = conversationItem.querySelector('.last-message-time');
        const unreadBadgeElement = conversationItem.querySelector('.unread-badge');

        if (lastMessageContentElement) {
            lastMessageContentElement.textContent = (isSenderCurrentUser ? "You: " : "") + this.escapeHtml(messageContent);
            if (!isSenderCurrentUser && newUnreadCount > 0) {
                lastMessageContentElement.classList.add('fw-bold');
                lastMessageContentElement.classList.remove('text-muted');
            } else {
                lastMessageContentElement.classList.remove('fw-bold');
                lastMessageContentElement.classList.add('text-muted');
            }
        }

        if (lastMessageTimeElement) {
            lastMessageTimeElement.textContent = this.formatTimeOnly(sentAt);
        }

        if (unreadBadgeElement) {
            if (newUnreadCount > 0) {
                unreadBadgeElement.textContent = newUnreadCount;
                unreadBadgeElement.style.display = '';
            } else {
                unreadBadgeElement.textContent = '0';
                unreadBadgeElement.style.display = 'none';
            }
        }

        if (this.conversationsListElement.firstChild !== conversationItem) {
            this.conversationsListElement.prepend(conversationItem);
        }
    }

    handleClearUnreadCount(conversationPartnerId) {
        console.log("ConversationsListUI: Received ClearUnreadCountForConversation", { conversationPartnerId });
        if (!this.conversationsListElement) return;

        const conversationItem = this.conversationsListElement.querySelector(`a.list-group-item[data-userid='${conversationPartnerId}']`);
        if (conversationItem) {
            const unreadBadgeElement = conversationItem.querySelector('.unread-badge');
            const lastMessageContentElement = conversationItem.querySelector('.last-message-content');

            if (unreadBadgeElement) {
                unreadBadgeElement.textContent = '0';
                unreadBadgeElement.style.display = 'none';
            }
            if (lastMessageContentElement && !lastMessageContentElement.textContent.startsWith("You: ")) {
                lastMessageContentElement.classList.remove('fw-bold');
                lastMessageContentElement.classList.add('text-muted');
            }
        }
    }

    escapeHtml(unsafe) {
        if (unsafe === null || typeof unsafe === 'undefined') return '';
        return unsafe.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
    }
}


// Utility functions
function requestNotificationPermission() {
    if (!("Notification" in window)) return;
    if (Notification.permission !== "granted" && Notification.permission !== "denied") {
        Notification.requestPermission();
    }
}

// Initialize chat systems when document is ready
document.addEventListener('DOMContentLoaded', function () {
    requestNotificationPermission();

    const receiverIdElement = document.getElementById('receiverId');
    const conversationsListElement = document.getElementById('conversationsList');
    const currentUserIdElement = document.getElementById('currentUserId');

    // Ensure previous managers are cleaned up if they exist on the window object
    // This is more of a safeguard, 'beforeunload' is the primary cleanup mechanism for page navigation
    if (window.currentChatUI && typeof window.currentChatUI.cleanup === 'function' && !receiverIdElement) {
        window.currentChatUI.cleanup();
        window.currentChatUI = null;
    }
    if (window.conversationsListManager && typeof window.conversationsListManager.cleanup === 'function' && !conversationsListElement) {
        window.conversationsListManager.cleanup();
        window.conversationsListManager = null;
    }


    if (receiverIdElement) {
        const receiverId = receiverIdElement.value;
        if (window.conversationsListManager && typeof window.conversationsListManager.cleanup === 'function') {
            window.conversationsListManager.cleanup(); // Clean up list manager if navigating to a chat
            window.conversationsListManager = null;
        }
        if (!window.currentChatUI) {
            window.currentChatUI = new ChatUI(receiverId);
            window.currentChatUI.initialize();
        }
    } else if (conversationsListElement && currentUserIdElement) {
        const currentUserId = currentUserIdElement.value;
        if (window.currentChatUI && typeof window.currentChatUI.cleanup === 'function') {
            window.currentChatUI.cleanup(); // Clean up chat UI if navigating to the list
            window.currentChatUI = null;
        }
        if (!window.conversationsListManager) {
            window.conversationsListManager = new ConversationsListUI(currentUserId);
            window.conversationsListManager.initialize();
        }
    }
});