(function () {
  console.log("[Fetchify] 🚀 Script injected and running...");

  const downloadExtensions = /\.(zip|exe|mp4|mp3|pdf|iso|rar|7z|msi|deb|apk|tar\.gz|docx?|xlsx?|pptx?|csv)(\?|$)/i;

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
    console.log(`[Fetchify] 📤 Sending to Fetchify API: ${url} ${filename ? `(filename: ${filename})` : ''}`);

    fetch("http://localhost:12345/api/download", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url, filename })
    })
      .then(res => {
        if (res.ok) console.log("✅ Fetchify accepted the request.");
        else console.warn(`⚠️ Fetchify rejected it. Status: ${res.status}`);
      })
      .catch(err => {
        console.error("❌ Network error sending to Fetchify:", err);
      });
  }

  function overrideMatchingLinks() {
    console.log("[Fetchify] 🔄 Overriding all matching <a> links on page...");
    const links = document.querySelectorAll("a[href]");
    for (const link of links) {
      const url = link.href;
      if (downloadExtensions.test(url)) {
        console.log(`[Fetchify] 🛠️ Overriding link: ${url}`);
        link.setAttribute("target", "_self");
        link.addEventListener("click", (e) => {
          sendToFetchify(url);

          const isDirectLink = downloadExtensions.test(url);
          if (isDirectLink) {
            e.preventDefault();
            e.stopImmediatePropagation();
            return false;
          }
        }, true);
      }
    }
  }

  function tryAutoDetectDownloadLinks() {
    const currentUrl = window.location.href.toLowerCase();
    console.log(`[Fetchify] 🌐 Current page URL: ${currentUrl}`);
    if (!currentUrl.includes("thank-you") && !currentUrl.includes("success")) {
      console.log("[Fetchify] ⏭️ Page does not trigger auto-detection.");
      return;
    }

    const links = Array.from(document.querySelectorAll("a[href]"));
    for (const a of links) {
      if (downloadExtensions.test(a.href)) {
        console.log(`[Fetchify] 🔍 Auto-detected thank-you link: ${a.href}`);
        sendToFetchify(a.href);
        break;
      }
    }
  }

  // Global click listener
  console.log("[Fetchify] 📌 Registering click event listener.");
  window.addEventListener("click", function (e) {
    const anchor = e.target.closest("a[href]");
    if (!anchor) return;

    const url = anchor.href;
    if (!downloadExtensions.test(url)) return;

    sendToFetchify(url);

    const isDirectLink = downloadExtensions.test(url);
    if (isDirectLink) {
      e.preventDefault();
      e.stopImmediatePropagation();
    }
  }, true);

  // DOM events
  window.addEventListener("DOMContentLoaded", () => {
    console.log("[Fetchify] ✅ DOM content loaded.");
    overrideMatchingLinks();
  });

  window.addEventListener("load", () => {
    console.log("[Fetchify] ✅ Full page load.");
    tryAutoDetectDownloadLinks();
  });

  // Redirect interception
  const originalAssign = window.location.assign.bind(window.location);
  const originalReplace = window.location.replace.bind(window.location);

  window.location.assign = function (url) {
    if (downloadExtensions.test(url)) {
      console.log(`[Fetchify] 🔁 Intercepted location.assign to: ${url}`);
      sendToFetchify(url);
    } else {
      originalAssign(url);
    }
  };

  window.location.replace = function (url) {
    if (downloadExtensions.test(url)) {
      console.log(`[Fetchify] 🔁 Intercepted location.replace to: ${url}`);
      sendToFetchify(url);
    } else {
      originalReplace(url);
    }
  };

  // Intercept window.open
  const originalOpen = window.open;
  window.open = function (url, ...args) {
    if (typeof url === "string" && downloadExtensions.test(url)) {
      console.log(`[Fetchify] 🚫 Intercepted window.open to: ${url}`);
      sendToFetchify(url);
      return null;
    }
    return originalOpen.call(window, url, ...args);
  };

  // Intercept anchor click() calls
  const originalAnchorClick = HTMLAnchorElement.prototype.click;
  HTMLAnchorElement.prototype.click = function () {
    const url = this.href;
    if (typeof url === "string" && downloadExtensions.test(url)) {
      console.log(`[Fetchify] 🪝 Intercepted anchor.click() to: ${url}`);
      sendToFetchify(url);
      return;
    }

    return originalAnchorClick.call(this);
  };

})();
