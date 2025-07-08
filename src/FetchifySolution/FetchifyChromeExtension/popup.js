// Fetchify Extension Popup Script
class FetchifyPopup {
    constructor() {
        this.elements = {
            statusDot: document.getElementById('statusDot'),
            statusText: document.getElementById('statusText'),
            statusDetail: document.getElementById('statusDetail'),
            extensionToggle: document.getElementById('extensionToggle'),
            totalDownloads: document.getElementById('totalDownloads'),
            todayDownloads: document.getElementById('todayDownloads'),
            testConnection: document.getElementById('testConnection'),
            testButtonText: document.getElementById('testButtonText'),
            testLoading: document.getElementById('testLoading'),
            openOptions: document.getElementById('openOptions'),
            clearStats: document.getElementById('clearStats')
        };

        this.state = {
            isEnabled: true,
            isOnline: false,
            stats: {
                total: 0,
                today: 0
            }
        };

        this.init();
    }

    async init() {
        await this.loadState();
        this.setupEventListeners();
        this.updateUI();
        this.checkBackendStatus();
    }

    async loadState() {
        try {
            // Load extension state
            const response = await chrome.runtime.sendMessage({ action: 'getConfig' });
            if (response) {
                this.state.isEnabled = response.enabled !== false;
            }

            // Load statistics
            const result = await chrome.storage.local.get(['downloadStats']);
            if (result.downloadStats) {
                this.state.stats = result.downloadStats;
            }
        } catch (error) {
            console.error('Failed to load popup state:', error);
        }
    }

    setupEventListeners() {
        // Extension toggle
        this.elements.extensionToggle.addEventListener('change', async (e) => {
            await this.toggleExtension(e.target.checked);
        });

        // Test connection button
        this.elements.testConnection.addEventListener('click', async () => {
            await this.testConnection();
        });

        // Open options page
        this.elements.openOptions.addEventListener('click', () => {
            chrome.runtime.openOptionsPage();
        });

        // Clear statistics
        this.elements.clearStats.addEventListener('click', async () => {
            await this.clearStatistics();
        });
    }

    async toggleExtension(enabled) {
        try {
            this.state.isEnabled = enabled;
            await chrome.storage.sync.set({ fetchifyEnabled: enabled });
            
            this.updateUI();
            this.showNotification(
                enabled ? 'Extension enabled' : 'Extension disabled', 
                enabled ? 'success' : 'info'
            );
        } catch (error) {
            console.error('Failed to toggle extension:', error);
            this.showNotification('Failed to update settings', 'error');
        }
    }

    async testConnection() {
        this.setTestButtonLoading(true);
        
        try {
            const response = await chrome.runtime.sendMessage({ action: 'ping' });
            this.state.isOnline = response && response.online;
            
            this.updateStatusUI();
            this.showNotification(
                this.state.isOnline ? 'Connection successful!' : 'Connection failed',
                this.state.isOnline ? 'success' : 'error'
            );
        } catch (error) {
            console.error('Connection test failed:', error);
            this.state.isOnline = false;
            this.updateStatusUI();
            this.showNotification('Connection test failed', 'error');
        } finally {
            this.setTestButtonLoading(false);
        }
    }

    async checkBackendStatus() {
        try {
            const response = await chrome.runtime.sendMessage({ action: 'ping' });
            this.state.isOnline = response && response.online;
            this.updateStatusUI();
        } catch (error) {
            this.state.isOnline = false;
            this.updateStatusUI();
        }
    }

    async clearStatistics() {
        try {
            await chrome.storage.local.set({ 
                downloadStats: { total: 0, today: 0 } 
            });
            
            this.state.stats = { total: 0, today: 0 };
            this.updateStatsUI();
            this.showNotification('Statistics cleared', 'success');
        } catch (error) {
            console.error('Failed to clear statistics:', error);
            this.showNotification('Failed to clear statistics', 'error');
        }
    }

    updateUI() {
        this.updateStatusUI();
        this.updateToggleUI();
        this.updateStatsUI();
    }

    updateStatusUI() {
        const { statusDot, statusText, statusDetail } = this.elements;
        
        if (!this.state.isEnabled) {
            statusDot.className = 'status-dot disabled';
            statusText.textContent = 'Extension Disabled';
            statusDetail.textContent = 'Enable the extension to start intercepting downloads';
        } else if (this.state.isOnline) {
            statusDot.className = 'status-dot online';
            statusText.textContent = 'Connected';
            statusDetail.textContent = 'Fetchify backend is running and ready';
        } else {
            statusDot.className = 'status-dot offline';
            statusText.textContent = 'Disconnected';
            statusDetail.textContent = 'Unable to connect to Fetchify backend';
        }
    }

    updateToggleUI() {
        this.elements.extensionToggle.checked = this.state.isEnabled;
    }

    updateStatsUI() {
        this.elements.totalDownloads.textContent = this.state.stats.total || 0;
        this.elements.todayDownloads.textContent = this.state.stats.today || 0;
    }

    setTestButtonLoading(loading) {
        const { testButtonText, testLoading } = this.elements;
        
        if (loading) {
            testButtonText.textContent = 'Testing...';
            testLoading.classList.remove('hidden');
        } else {
            testButtonText.textContent = 'Test Connection';
            testLoading.classList.add('hidden');
        }
    }

    showNotification(message, type = 'success') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.notification');
        existingNotifications.forEach(notification => notification.remove());

        // Create new notification
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 3 seconds
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // Listen for background script messages
    setupMessageListener() {
        chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
            if (request.action === 'updateStats') {
                this.state.stats = request.stats;
                this.updateStatsUI();
            }
        });
    }
}

// Initialize popup when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new FetchifyPopup();
});

// Update statistics when popup is opened
chrome.storage.onChanged.addListener((changes, namespace) => {
    if (changes.downloadStats) {
        const popup = window.fetchifyPopup;
        if (popup) {
            popup.state.stats = changes.downloadStats.newValue;
            popup.updateStatsUI();
        }
    }
});