import type { Plugin } from "vite";
import type { OutputBundle } from "rollup";

import { HELP_CENTRE_URL } from "./src/config/support";

function extractAvifSrcset(bundle: OutputBundle, assetKey: string): string | null {
  const pattern = new RegExp(`avif:\\\`([^\\\`]*${assetKey}[^\\\`]*)\\\``);

  for (const item of Object.values(bundle)) {
    if (item.type !== "chunk") {
      continue;
    }

    const match = item.code.match(pattern);
    if (match) {
      return match[1];
    }
  }

  return null;
}

function buildPreloadTags(bundle: OutputBundle): string {
  const heroSrcset = extractAvifSrcset(bundle, "hero-bg");
  const helpCenterSrcset = extractAvifSrcset(bundle, "help-center-bg");
  const authSignInPanelSrcset = extractAvifSrcset(bundle, "auth-sign-in-panel");

  if (!heroSrcset && !authSignInPanelSrcset && !helpCenterSrcset) {
    return "";
  }

  const authSizes = "(min-width: 1024px) 41.3vw, 0px";
  const heroSrcsetJson = heroSrcset ? JSON.stringify(heroSrcset) : "null";
  const helpCenterSrcsetJson = helpCenterSrcset
    ? JSON.stringify(helpCenterSrcset)
    : "null";
  const authSignInPanelJson = authSignInPanelSrcset
    ? JSON.stringify(authSignInPanelSrcset)
    : "null";

  const loginPreloadScript = `<script>
(function () {
  var path = location.pathname;

  function appendPreload(attrs) {
    var link = document.createElement("link");
    link.rel = "preload";
    for (var key in attrs) {
      if (attrs[key]) link.setAttribute(key, attrs[key]);
    }
    link.fetchPriority = "high";
    document.head.appendChild(link);
  }

  if (path === "/" || path === "") {
    var heroSrcset = ${heroSrcsetJson};
    if (heroSrcset) {
      appendPreload({
        as: "image",
        type: "image/avif",
        imagesrcset: heroSrcset,
        imagesizes: "100vw"
      });
    }
  }

  if (path === ${JSON.stringify(HELP_CENTRE_URL)} || path === ${JSON.stringify(`${HELP_CENTRE_URL}/`)}) {
    var helpCenterSrcset = ${helpCenterSrcsetJson};
    if (helpCenterSrcset) {
      appendPreload({
        as: "image",
        type: "image/avif",
        imagesrcset: helpCenterSrcset,
        imagesizes: "100vw"
      });
    }
  }

  if (path === "/login" || path === "/login/") {
    var authSizes = ${JSON.stringify(authSizes)};
    var authSignInPanelSrcset = ${authSignInPanelJson};
    if (authSignInPanelSrcset) {
      appendPreload({
        as: "image",
        type: "image/avif",
        imagesrcset: authSignInPanelSrcset,
        imagesizes: authSizes
      });
    }
  }
})();
</script>`;

  return loginPreloadScript;
}

export function criticalPreloads(): Plugin {
  return {
    name: "critical-preloads",
    apply: "build",
    transformIndexHtml: {
      order: "post",
      handler(html, ctx) {
        if (!ctx.bundle) {
          return html;
        }

        const preloadTags = buildPreloadTags(ctx.bundle);
        if (!preloadTags) {
          return html;
        }

        return html.replace("<!-- CRITICAL_PRELOADS -->", preloadTags);
      },
    },
  };
}
