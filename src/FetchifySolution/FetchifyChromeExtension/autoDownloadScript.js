(function () {
  'use strict';

  if (window.fetchifySimpleInitialized) return;
  window.fetchifySimpleInitialized = true;

  console.log("[Fetchify] 🚀 Lightweight script active...");

  const downloadExtensions = /\.(zip|exe|mp4|mp3|pdf|iso|rar|7z|msi|deb|apk|tar\.gz|docx?|xlsx?|pptx?|csv)$/i;
  const recent = new Set();

  function isDownloadUrl(url) {
    try {
      const fullUrl = new URL(url, window.location.href).href;
      return downloadExtensions.test(fullUrl);
    } catch {
      return false;
    }
  }

  function sendToFetchify(url) {
    if (recent.has(url)) {
      console.log("[Fetchify] ⚠️ Duplicate suppressed:", url);
      return;
    }

    recent.add(url);
    setTimeout(() => recent.delete(url), 3000);

    console.log(`[Fetchify] 📤 Sending to Fetchify: ${url}`);
    fetch("http://localhost:12345/api/download", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url })
    })
      .then(res => {
        if (res.ok) console.log("✅ Fetchify accepted the request.");
        else console.warn(`⚠️ Fetchify rejected it. Status: ${res.status}`);
      })
      .catch(err => {
        console.error("❌ Network error sending to Fetchify:", err);
      });
  }

  function onClick(e) {
    // Only left button and no modifier keys
    if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

    // Find anchor element up the DOM tree
    let el = e.target;
    while (el && el !== document.body && el.tagName !== 'A') {
      el = el.parentElement;
    }

    if (el && el.tagName === 'A' && el.href && isDownloadUrl(el.href)) {
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      console.log("[Fetchify] ✅ Intercepted left-click on:", el.href);
      sendToFetchify(el.href);
    }
  }

  document.addEventListener("click", onClick, true);
})();