import type { Plugin } from "vite";
import type { OutputBundle } from "rollup";

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

function extractAssetPath(bundle: OutputBundle, assetKey: string): string | null {
  const pattern = new RegExp(`(/assets/${assetKey}[A-Za-z0-9_.-]+\\.(?:png|webp|avif|jpg|jpeg))`);

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
  const auth267Srcset = extractAvifSrcset(bundle, "auth-hero-frame-267");
  const auth268Srcset = extractAvifSrcset(bundle, "auth-hero-frame-268");
  const authLogoHref = extractAssetPath(bundle, "auth-hero-logo-");

  if (!heroSrcset && !(auth267Srcset && auth268Srcset)) {
    return "";
  }

  const authSizes = "(min-width: 1024px) 45.38vw, 0px";
  const heroSrcsetJson = heroSrcset ? JSON.stringify(heroSrcset) : "null";
  const auth267Json = auth267Srcset ? JSON.stringify(auth267Srcset) : "null";
  const auth268Json = auth268Srcset ? JSON.stringify(auth268Srcset) : "null";
  const authLogoJson = authLogoHref ? JSON.stringify(authLogoHref) : "null";

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

  if (path === "/login" || path === "/login/") {
    var authSizes = ${JSON.stringify(authSizes)};
    var frames = [
      { srcset: ${auth267Json}, type: "image/avif" },
      { srcset: ${auth268Json}, type: "image/avif" }
    ];

    for (var i = 0; i < frames.length; i++) {
      if (!frames[i].srcset) continue;
      appendPreload({
        as: "image",
        type: frames[i].type,
        imagesrcset: frames[i].srcset,
        imagesizes: authSizes
      });
    }

    var logoHref = ${authLogoJson};
    if (logoHref) {
      appendPreload({ as: "image", href: logoHref });
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
