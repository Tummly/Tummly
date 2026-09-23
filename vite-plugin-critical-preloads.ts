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
  const ctaLaunchBgSrcset = extractAvifSrcset(bundle, "cta-launch-bg");

  if (
    !heroSrcset &&
    !authSignInPanelSrcset &&
    !helpCenterSrcset &&
    !ctaLaunchBgSrcset
  ) {
    return "";
  }

  const authSizes = "(min-width: 1024px) 41.3vw, 0px";
  const panoramicSizes = "(min-width: 1726px) 100vw, 1726px";
  const heroSrcsetJson = heroSrcset ? JSON.stringify(heroSrcset) : "null";
  const helpCenterSrcsetJson = helpCenterSrcset
    ? JSON.stringify(helpCenterSrcset)
    : "null";
  const authSignInPanelJson = authSignInPanelSrcset
    ? JSON.stringify(authSignInPanelSrcset)
    : "null";
  const ctaLaunchBgJson = ctaLaunchBgSrcset
    ? JSON.stringify(ctaLaunchBgSrcset)
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

  function appendPrefetch(attrs) {
    var link = document.createElement("link");
    link.rel = "prefetch";
    for (var key in attrs) {
      if (attrs[key]) link.setAttribute(key, attrs[key]);
    }
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
    var ctaLaunchBgSrcsetHome = ${ctaLaunchBgJson};
    if (ctaLaunchBgSrcsetHome) {
      appendPrefetch({
        as: "image",
        type: "image/avif",
        imagesrcset: ctaLaunchBgSrcsetHome,
        imagesizes: ${JSON.stringify(panoramicSizes)}
      });
    }
  }

  if (
    path === "/faqs" ||
    path === "/faqs/" ||
    path === "/trust-privacy" ||
    path === "/trust-privacy/" ||
    path === "/privacy" ||
    path === "/privacy/" ||
    path === "/terms" ||
    path === "/terms/" ||
    path === "/cookie-policy" ||
    path === "/cookie-policy/" ||
    path === "/accessibility" ||
    path === "/accessibility/" ||
    path === "/acceptable-use" ||
    path === "/acceptable-use/" ||
    path === "/shop-print-terms" ||
    path === "/shop-print-terms/"
  ) {
    var ctaLaunchBgSrcsetMarketing = ${ctaLaunchBgJson};
    if (ctaLaunchBgSrcsetMarketing) {
      appendPreload({
        as: "image",
        type: "image/avif",
        imagesrcset: ctaLaunchBgSrcsetMarketing,
        imagesizes: ${JSON.stringify(panoramicSizes)}
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

  var authPaths = {
    "/login": true,
    "/login/": true,
    "/forgot-password": true,
    "/forgot-password/": true,
    "/reset-password": true,
    "/reset-password/": true,
    "/verify-email": true,
    "/verify-email/": true,
    "/start": true,
    "/start/": true
  };
  if (authPaths[path]) {
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
