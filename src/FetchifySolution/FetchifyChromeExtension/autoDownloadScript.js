(function () {
  const downloadExtensions = /\.(zip|exe|mp4|mp3|pdf|iso|rar|7z|msi|deb|apk|tar\.gz|docx?|xlsx?|pptx?)$/i;

  function sendToFetchify(url) {
    console.log("[Fetchify] Auto-detected download:", url);

    fetch("http://localhost:12345/api/download", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url })
    })
      .then(res => {
        if (res.ok) console.log("✅ Sent to Fetchify.");
        else console.warn("⚠️ Rejected by Fetchify.");
      })
      .catch(err => console.error("❌ Fetchify error:", err));
  }

  function interceptLinkClick(e) {
    const anchor = e.target.closest("a");
    if (!anchor || !anchor.href) return;

    const url = anchor.href;

    if (downloadExtensions.test(url)) {
      e.preventDefault(); // Stop Chrome default download
      e.stopPropagation();

      console.log("[Fetchify] Intercepted link click:", url);
      sendToFetchify(url);
    }
  }

  function preventDefaultOnMatchingLinks() {
    const links = document.querySelectorAll("a[href]");
    for (const link of links) {
      if (downloadExtensions.test(link.href)) {
        link.addEventListener("click", e => {
          e.preventDefault();
          e.stopPropagation();
          sendToFetchify(link.href);
        });
      }
    }
  }

  function tryAutoDetectDownloadLinks() {
    const links = Array.from(document.querySelectorAll("a[href]"));
    for (const a of links) {
      if (downloadExtensions.test(a.href)) {
        console.log("[Fetchify] Auto-detected static link:", a.href);
        sendToFetchify(a.href);
        break; // Only handle one
      }
    }
  }

  // Intercept user clicks early (for dynamic DOM)
  document.addEventListener("click", interceptLinkClick, true);

  // Prevent static links from triggering download
  window.addEventListener("DOMContentLoaded", preventDefaultOnMatchingLinks);

  // Auto-detect on thank-you/success pages
  window.addEventListener("load", () => {
    const url = window.location.href.toLowerCase();
    if (url.includes("thank-you") || url.includes("success")) {
      setTimeout(tryAutoDetectDownloadLinks, 1500);
    }
  });
})();
