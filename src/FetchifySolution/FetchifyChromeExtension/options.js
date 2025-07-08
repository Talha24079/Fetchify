// Fetchify Extension Options Page
class FetchifyOptions {
    constructor() {
        this.defaultConfig = {
            apiUrl: 'http://localhost:12345/api/download',
            timeout: 10000,
            retryAttempts: 3,
            retryDelay: 1000,
            pollInterval: 2000,
            autoDetect: true,
            autoDetectKeywords: 'thank-you,success,download,complete,finished',
            debugMode: false,
            interceptWindowOpen: true,
            fileExtensions: 'zip,exe,mp4,mp3,pdf,iso,rar,7z,msi,deb,apk,tar.gz,docx,doc,xlsx,xls,pptx,ppt,csv,dmg,pkg,torrent,flv,avi,mov,wmv,mkv,m4v,webm,ogg,wav,flac,aac,wma,epub,mobi,azw3,cbr,cbz,psd,ai,sketch,fig,ipa,xpi,crx'
        };
        this.elements = {
            apiUrl: document.getElementById('apiUrl'),
            timeout: document.getElementById('timeout'),
            retryAttempts: document.getElementById('retryAttempts'),
            fileExtensions: document.getElementById('fileExtensions'),
            autoDetect: document.getElementById('autoDetect'),
            autoDetectKeywords: document.getElementById('autoDetectKeywords'),
            debugMode: document.getElementById('debugMode'),
            pollInterval: document.getElementById('pollInterval'),
            interceptWindowOpen: document.getElementById('interceptWindowOpen'),
            saveSettings: document.getElementById('saveSettings'),
            testConnection: document.getElementById('testConnection'),
            resetSettings: document.getElementById('resetSettings'),
            clearAllData: document.getElementById('clearAllData'),
            statusMessage: document.getElementById('statusMessage'),
            extensionList: document.getElementById('extensionList'),
            totalDownloads: document.getElementById('totalDownloads'),
            todayDownloads: document.getElementById('todayDownloads'),
            weekDownloads: document.getElementById('weekDownloads')
        };
        this.init();
    }

    async init() {
        await this.loadSettings();
        this.setupEventListeners();
        this.updateExtensionList();
        this.loadStatistics();
    }

    async loadSettings() {
        try {
            const result = await chrome.storage.sync.get(['fetchifyConfig']);
            const config = result.fetchifyConfig || this.defaultConfig;
            
            this.elements.apiUrl.value = config.apiUrl || this.defaultConfig.apiUrl;
            this.elements.timeout.value = config.timeout || this.defaultConfig.timeout;
            this.elements.retryAttempts.value = config.retryAttempts || this.defaultConfig.retryAttempts;
            this.elements.fileExtensions.value = config.fileExtensions || this.defaultConfig.fileExtensions;
            this.elements.autoDetect.checked = config.autoDetect !== false;
            this.elements.autoDetectKeywords.value = config.autoDetectKeywords || this.defaultConfig.autoDetectKeywords;
            this.elements.debugMode.checked = config.debugMode || false;
            this.elements.pollInterval.value = config.pollInterval || this.defaultConfig.pollInterval;
            this.elements.interceptWindowOpen.checked = config.interceptWindowOpen !== false;
            
        } catch (error) {
            console.error('Failed to load settings:', error);
            this.showMessage('Failed to load settings', 'error');
        }
    }

    async saveSettings() {
        try {
            const config = {
                apiUrl: this.elements.apiUrl.value.trim(),
                timeout: parseInt(this.elements.timeout.value),
                retryAttempts: parseInt(this.elements.retryAttempts.value),
                retryDelay: this.defaultConfig.retryDelay,
                pollInterval: parseInt(this.elements.pollInterval.value),
                autoDetect: this.elements.autoDetect.checked,
                autoDetectKeywords: this.elements.autoDetectKeywords.value.trim(),
                debugMode: this.elements.debugMode.checked,
                interceptWindowOpen: this.elements.interceptWindowOpen.checked,
                fileExtensions: this.elements.fileExtensions.value.trim()
            };

            // Validate settings
            if (!this.validateSettings(config)) {
                return;
            }

            await chrome.storage.sync.set({ fetchifyConfig: config });
            this.showMessage('Settings saved successfully!', 'success');
            
            // Notify background script of config change
            chrome.runtime.sendMessage({ 
                action: 'configUpdated', 
                config: config 
            });
            
        } catch (error) {
            console.error('Failed to save settings:', error);
            this.showMessage('Failed to save settings', 'error');
        }
    }

    validateSettings(config) {
        // Validate API URL
        if (!config.apiUrl || !this.isValidUrl(config.apiUrl)) {
            this.showMessage('Please enter a valid API URL', 'error');
            return false;
        }

        // Validate timeout
        if (config.timeout < 1000 || config.timeout > 300000) {
            this.showMessage('Timeout must be between 1000ms and 300000ms', 'error');
            return false;
        }

        // Validate retry attempts
        if (config.retryAttempts < 0 || config.retryAttempts > 10) {
            this.showMessage('Retry attempts must be between 0 and 10', 'error');
            return false;
        }

        // Validate poll interval
        if (config.pollInterval < 500 || config.pollInterval > 10000) {
            this.showMessage('Poll interval must be between 500ms and 10000ms', 'error');
            return false;
        }

        return true;
    }

    isValidUrl(string) {
        try {
            new URL(string);
            return true;
        } catch (_) {
            return false;
        }
    }

    setupEventListeners() {
        // Save settings button
        this.elements.saveSettings.addEventListener('click', () => {
            this.saveSettings();
        });

        // Test connection button
        this.elements.testConnection.addEventListener('click', () => {
            this.testApiConnection();
        });

        // Reset settings button
        this.elements.resetSettings.addEventListener('click', () => {
            this.resetToDefault();
        });

        // Clear all data button
        this.elements.clearAllData.addEventListener('click', () => {
            this.clearAllData();
        });

        // Auto-detect toggle
        this.elements.autoDetect.addEventListener('change', (e) => {
            this.elements.autoDetectKeywords.disabled = !e.target.checked;
        });

        // Real-time validation
        this.elements.apiUrl.addEventListener('input', () => {
            this.validateApiUrl();
        });

        this.elements.timeout.addEventListener('input', () => {
            this.validateTimeout();
        });

        this.elements.retryAttempts.addEventListener('input', () => {
            this.validateRetryAttempts();
        });
    }

    validateApiUrl() {
        const url = this.elements.apiUrl.value.trim();
        if (url && !this.isValidUrl(url)) {
            this.elements.apiUrl.setCustomValidity('Please enter a valid URL');
        } else {
            this.elements.apiUrl.setCustomValidity('');
        }
    }

    validateTimeout() {
        const timeout = parseInt(this.elements.timeout.value);
        if (timeout < 1000 || timeout > 300000) {
            this.elements.timeout.setCustomValidity('Timeout must be between 1000ms and 300000ms');
        } else {
            this.elements.timeout.setCustomValidity('');
        }
    }

    validateRetryAttempts() {
        const attempts = parseInt(this.elements.retryAttempts.value);
        if (attempts < 0 || attempts > 10) {
            this.elements.retryAttempts.setCustomValidity('Retry attempts must be between 0 and 10');
        } else {
            this.elements.retryAttempts.setCustomValidity('');
        }
    }

    async testApiConnection() {
        const apiUrl = this.elements.apiUrl.value.trim();
        if (!apiUrl) {
            this.showMessage('Please enter an API URL first', 'error');
            return;
        }

        this.elements.testConnection.disabled = true;
        this.elements.testConnection.textContent = 'Testing...';

        try {
            const response = await fetch(apiUrl + '/health', {
                method: 'GET',
                timeout: 5000
            });

            if (response.ok) {
                this.showMessage('API connection successful!', 'success');
            } else {
                this.showMessage(`API connection failed: ${response.status}`, 'error');
            }
        } catch (error) {
            this.showMessage(`API connection failed: ${error.message}`, 'error');
        } finally {
            this.elements.testConnection.disabled = false;
            this.elements.testConnection.textContent = 'Test Connection';
        }
    }

    async resetToDefault() {
        if (confirm('Are you sure you want to reset all settings to default values?')) {
            try {
                await chrome.storage.sync.remove(['fetchifyConfig']);
                await this.loadSettings();
                this.showMessage('Settings reset to default', 'success');
            } catch (error) {
                console.error('Failed to reset settings:', error);
                this.showMessage('Failed to reset settings', 'error');
            }
        }
    }

    async clearAllData() {
        if (confirm('Are you sure you want to clear all extension data? This action cannot be undone.')) {
            try {
                await chrome.storage.sync.clear();
                await chrome.storage.local.clear();
                this.showMessage('All data cleared successfully', 'success');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } catch (error) {
                console.error('Failed to clear data:', error);
                this.showMessage('Failed to clear data', 'error');
            }
        }
    }

    updateExtensionList() {
        const extensions = this.elements.fileExtensions.value.split(',').map(ext => ext.trim());
        const listContainer = this.elements.extensionList;
        
        listContainer.innerHTML = '';
        extensions.forEach(ext => {
            if (ext) {
                const span = document.createElement('span');
                span.className = 'extension-tag';
                span.textContent = ext;
                listContainer.appendChild(span);
            }
        });
    }

    async loadStatistics() {
        try {
            const result = await chrome.storage.local.get(['downloadStats']);
            const stats = result.downloadStats || {
                total: 0,
                today: 0,
                week: 0,
                lastReset: new Date().toDateString()
            };

            // Reset daily/weekly counters if needed
            const today = new Date().toDateString();
            if (stats.lastReset !== today) {
                stats.today = 0;
                stats.lastReset = today;
            }

            this.elements.totalDownloads.textContent = stats.total;
            this.elements.todayDownloads.textContent = stats.today;
            this.elements.weekDownloads.textContent = stats.week;
        } catch (error) {
            console.error('Failed to load statistics:', error);
        }
    }

    showMessage(message, type = 'info') {
        const messageElement = this.elements.statusMessage;
        messageElement.textContent = message;
        messageElement.className = `status-message ${type}`;
        messageElement.style.display = 'block';
        
        setTimeout(() => {
            messageElement.style.display = 'none';
        }, 5000);
    }
}

// Initialize the options page when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new FetchifyOptions();
});

// Handle extension updates
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.action === 'statsUpdated') {
        const options = new FetchifyOptions();
        options.loadStatistics();
    }
});