// Context menu for manual right-click
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "send-to-fetchify",
    title: "Download with Fetchify",
    contexts: ["link"]
  });
});

// Right-click context menu handler
chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId === "send-to-fetchify" && info.linkUrl) {
    sendToFetchify(info.linkUrl);
  }
});

// Utility to send download to Fetchify API
function sendToFetchify(url) {
  fetch("http://localhost:12345/api/download", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ url })
  })
  .then(res => {
    if (!res.ok) {
      console.error("Failed to send download to Fetchify:", res.status);
    }
  })
  .catch(err => {
    console.error("Fetchify extension error:", err);
  });
}
