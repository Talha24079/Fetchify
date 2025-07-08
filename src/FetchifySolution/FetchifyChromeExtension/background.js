// Fetchify Extension Background Script
// Fixed version to prevent duplicate script injection

class FetchifyBackgroundService {
  constructor() {
    this.config = {
      apiUrl: 'http://localhost:12345/api/download',
      timeout: 10000,
      retryAttempts: 3,
      retryDelay: 1000
    };
    this.isEnabled = true;
    this.injectedTabs = new Set(); // Track injected tabs
    this.loadSettings();
    this.setupEventListeners();
  }

  async loadSettings() {
    try {
      const result = await chrome.storage.sync.get(['fetchifyConfig', 'fetchifyEnabled']);
      if (result.fetchifyConfig) {
        this.config = { ...this.config, ...result.fetchifyConfig };
      }
      this.isEnabled = result.fetchifyEnabled !== false;
    } catch (error) {
      console.warn('[Fetchify] Failed to load settings:', error);
    }
  }

  setupEventListeners() {
    // Context menu setup
    chrome.runtime.onInstalled.addListener(() => {
      this.createContextMenu();
    });

    // Context menu click handler
    chrome.contextMenus.onClicked.addListener((info, tab) => {
      this.handleContextMenuClick(info, tab);
    });

    // Tab update listener - MODIFIED to prevent duplicate injection
    chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
      this.handleTabUpdate(tabId, changeInfo, tab);
    });

    // Tab removal listener to clean up tracking
    chrome.tabs.onRemoved.addListener((tabId) => {
      this.injectedTabs.delete(tabId);
    });

    // Message handling from content scripts
    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
      this.handleMessage(request, sender, sendResponse);
      return true; // Keep message channel open for async response
    });

    // Storage change listener
    chrome.storage.onChanged.addListener((changes) => {
      this.handleStorageChange(changes);
    });
  }

  createContextMenu() {
    chrome.contextMenus.removeAll(() => {
      chrome.contextMenus.create({
        id: 'send-to-fetchify',
        title: 'Download with Fetchify',
        contexts: ['link'],
        enabled: this.isEnabled
      });
      
      chrome.contextMenus.create({
        id: 'fetchify-separator',
        type: 'separator',
        contexts: ['link']
      });
      
      chrome.contextMenus.create({
        id: 'fetchify-toggle',
        title: this.isEnabled ? 'Disable Fetchify' : 'Enable Fetchify',
        contexts: ['link']
      });
    });
    
    console.log('[Fetchify] Context menu created');
  }

  async handleContextMenuClick(info, tab) {
    if (info.menuItemId === 'send-to-fetchify' && info.linkUrl) {
      console.log(`[Fetchify] Context menu clicked for: ${info.linkUrl}`);
      await this.sendToFetchify(info.linkUrl, { source: 'context_menu' });
    } else if (info.menuItemId === 'fetchify-toggle') {
      await this.toggleExtension();
    }
  }

  async handleTabUpdate(tabId, changeInfo, tab) {
    // Only inject if:
    // 1. Tab is complete
    // 2. It's a valid HTTP/HTTPS URL
    // 3. Extension is enabled
    // 4. Not a chrome:// or extension:// URL
    // 5. We haven't already injected into this tab
    if (changeInfo.status === 'complete' && 
        /^https?:/.test(tab.url) && 
        this.isEnabled &&
        !tab.url.includes('chrome://') &&
        !tab.url.includes('chrome-extension://') &&
        !this.injectedTabs.has(tabId)) {
      
      try {
        // Check if content script is already loaded
        const results = await chrome.scripting.executeScript({
          target: { tabId },
          func: () => window.fetchifyScriptLoaded || false
        });
        
        // If script is not loaded, inject it
        if (!results[0]?.result) {
          await chrome.scripting.executeScript({
            target: { tabId },
            files: ['autoDownloadScript.js']
          });
          
          this.injectedTabs.add(tabId);
          console.log(`[Fetchify] Content script injected: ${tab.url}`);
        } else {
          console.log(`[Fetchify] Content script already loaded: ${tab.url}`);
          this.injectedTabs.add(tabId);
        }
      } catch (error) {
        console.warn('[Fetchify] Script injection failed:', error.message);
      }
    }
  }

  async handleMessage(request, sender, sendResponse) {
    try {
      switch (request.action) {
        case 'download':
          if (!request.url) {
            sendResponse({ success: false, error: 'No URL provided' });
            break;
          }
          try {
            const result = await this.sendToFetchify(request.url, request.metadata || {});
            sendResponse({ success: true, result });
          } catch (error) {
            console.error('[Fetchify] Failed to send URL:', request.url, error);
            sendResponse({ success: false, error: error.message });
          }
          break;
          
        case 'getConfig':
          sendResponse({ config: this.config, enabled: this.isEnabled });
          break;
          
        case 'updateConfig':
          await this.updateConfig(request.config);
          sendResponse({ success: true });
          break;
          
        case 'ping':
          const isOnline = await this.checkBackendStatus();
          sendResponse({ online: isOnline });
          break;
          
        default:
          sendResponse({ error: 'Unknown action' });
      }
    } catch (error) {
      console.error('[Fetchify] Message handling error:', error);
      sendResponse({ error: error.message });
    }
  }

  handleStorageChange(changes) {
    if (changes.fetchifyEnabled) {
      this.isEnabled = changes.fetchifyEnabled.newValue;
      this.createContextMenu();
    }
    
    if (changes.fetchifyConfig) {
      this.config = { ...this.config, ...changes.fetchifyConfig.newValue };
    }
  }

  async sendToFetchify(url, metadata = {}) {
    if (!this.isEnabled) {
      throw new Error('Fetchify extension is disabled');
    }

    const requestData = {
      url,
      timestamp: Date.now(),
      userAgent: navigator.userAgent,
      ...metadata
    };

    let lastError;
    
    for (let attempt = 0; attempt < this.config.retryAttempts; attempt++) {
      try {
        const response = await this.makeApiRequest(requestData);
        
        if (response.ok) {
          const responseData = await response.json();
          console.log('[Fetchify] Successfully sent to backend:', responseData);
          
          this.showNotification('✅ Download Queued', 
            `File queued for download: ${this.getFileNameFromUrl(url)}`);
          
          return responseData;
        } else {
          throw new Error(`Server responded with ${response.status}: ${response.statusText}`);
        }
      } catch (error) {
        lastError = error;
        console.warn(`[Fetchify] Attempt ${attempt + 1} failed:`, error.message);
        
        if (attempt < this.config.retryAttempts - 1) {
          await this.delay(this.config.retryDelay * Math.pow(2, attempt));
        }
      }
    }
    
    console.error('[Fetchify] All retry attempts failed:', lastError);
    this.showNotification('❌ Download Failed', 
      `Failed to connect to Fetchify backend after ${this.config.retryAttempts} attempts`);
    
    throw lastError;
  }

  async makeApiRequest(data) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.config.timeout);
    
    try {
      const response = await fetch(this.config.apiUrl, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'X-Fetchify-Extension': 'true'
        },
        body: JSON.stringify(data),
        signal: controller.signal
      });
      
      clearTimeout(timeoutId);
      return response;
    } catch (error) {
      clearTimeout(timeoutId);
      throw error;
    }
  }

  async checkBackendStatus() {
    try {
      const response = await fetch(`${this.config.apiUrl}/status`, {
        method: 'GET',
        headers: { 'X-Fetchify-Extension': 'true' }
      });
      return response.ok;
    } catch (error) {
      return false;
    }
  }

  async toggleExtension() {
    this.isEnabled = !this.isEnabled;
    await chrome.storage.sync.set({ fetchifyEnabled: this.isEnabled });
    
    this.createContextMenu();
    this.showNotification(
      this.isEnabled ? '✅ Fetchify Enabled' : '❌ Fetchify Disabled',
      `Extension is now ${this.isEnabled ? 'active' : 'inactive'}`
    );
  }

  async updateConfig(newConfig) {
    this.config = { ...this.config, ...newConfig };
    await chrome.storage.sync.set({ fetchifyConfig: this.config });
  }

  showNotification(title, message) {
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.png',
      title: title,
      message: message,
      requireInteraction: false
    });
  }

  getFileNameFromUrl(url) {
    try {
      const urlObj = new URL(url);
      const pathname = urlObj.pathname;
      const filename = pathname.split('/').pop();
      return filename || 'Unknown file';
    } catch (error) {
      return 'Unknown file';
    }
  }

  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Initialize the background service
const fetchifyService = new FetchifyBackgroundService();

// Keep service worker alive
chrome.runtime.onStartup.addListener(() => {
  console.log('[Fetchify] Service worker started');
});

// Export for testing purposes
if (typeof module !== 'undefined' && module.exports) {
  module.exports = FetchifyBackgroundService;
}