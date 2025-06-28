(function () {
  document.addEventListener("click", function (e) {
    const anchor = e.target.closest("a");
    if (!anchor || !anchor.href) return;

    const url = anchor.href;

    // Check for left-click only (button 0), and avoid Ctrl/Meta (new tab)
    if (e.button !== 0 || e.ctrlKey || e.metaKey) return;

    // Match common downloadable extensions
    const downloadExtensions = /\.(zip|exe|pdf|mp4|mp3|rar|7z|iso|apk|docx?|xlsx?|pptx?|csv)$/i;

    if (downloadExtensions.test(url)) {
      e.preventDefault(); // Stop Chrome's default download behavior

      console.log("[Fetchify Intercept] Sending to Fetchify:", url);

      fetch("http://localhost:12345/api/download", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url })
      })
        .then(res => {
          if (res.ok) {
            console.log("✅ Fetchify received the download.");
          } else {
            console.warn("⚠️ Fetchify rejected the download.");
          }
        })
        .catch(err => {
          console.error("❌ Fetchify is not available or failed:", err);
        });
    }
  }, true); // Use capture to catch early
})();
