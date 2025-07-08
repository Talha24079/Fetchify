// Create right-click menu on extension install
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "send-to-fetchify",
    title: "Download with Fetchify",
    contexts: ["link"]
  });

  console.log("[Fetchify] ✅ Context menu installed.");
});

// Handle context menu click
chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId === "send-to-fetchify" && info.linkUrl) {
    console.log(`[Fetchify] 📤 Context menu clicked for: ${info.linkUrl}`);
    sendToFetchify(info.linkUrl);
  }
});

function extractFilenameFromURL(url) {
  try {
    const parsed = new URL(url);
    const queryName = parsed.searchParams.get("filename");
    if (queryName) return decodeURIComponent(queryName);

    const lastSegment = parsed.pathname.split("/").pop();
    if (/\.[a-z0-9]{2,5}$/i.test(lastSegment)) {
      return decodeURIComponent(lastSegment);
    }

    return null;
  } catch (e) {
    return null;
  }
}

function sendToFetchify(url) {
  const filename = extractFilenameFromURL(url);

  fetch("http://localhost:12345/api/download", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ url, filename })
  })
    .then((res) => {
      if (res.ok) {
        console.log("[Fetchify] ✅ Download sent successfully.");
        showNotification("✅ Sent to Fetchify", "The download was sent successfully.");
      } else {
        console.error("[Fetchify] ❌ Server rejected the download:", res.status);
        showNotification("❌ Fetchify Error", `Server responded with status ${res.status}`);
      }
    })
    .catch((err) => {
      console.error("[Fetchify] ❌ Network error:", err);
      showNotification("❌ Fetchify Error", "Could not reach the Fetchify server.");
    });
}

function showNotification(title, message) {
  chrome.notifications.create({
    type: "basic",
    iconUrl: "icon.png",
    title: title,
    message: message
  });
}
