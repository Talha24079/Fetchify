chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "send-to-fetchify",
    title: "Download with Fetchify",
    contexts: ["link"]
  });
});

chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId === "send-to-fetchify" && info.linkUrl) {
    fetch("http://localhost:12345/api/download", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ url: info.linkUrl })
    })
    .then(response => {
      if (response.ok) {
        console.log("Download sent to Fetchify successfully");
      } else {
        console.error("Failed to send download to Fetchify");
      }
    })
    .catch(error => {
      console.error("Fetchify extension error:", error);
    });
  }
});
