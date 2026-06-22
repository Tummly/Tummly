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

function buildPreloadTags(bundle: OutputBundle): string {
  const heroSrcset = extractAvifSrcset(bundle, "hero-bg");
  const auth267Srcset = extractAvifSrcset(bundle, "auth-hero-frame-267");
  const auth268Srcset = extractAvifSrcset(bundle, "auth-hero-frame-268");

  const tags: string[] = [];

  if (heroSrcset) {
    tags.push(
      `<link rel="preload" as="image" type="image/avif" imagesrcset="${heroSrcset}" imagesizes="100vw" fetchpriority="high" />`,
    );
  }

  if (auth267Srcset && auth268Srcset) {
    const authSizes = "(min-width: 1024px) 45.38vw, 0px";
    const loginPreloadScript = `<script>
(function () {
  var path = location.pathname;
  if (path !== "/login" && path !== "/login/") return;
  var authSizes = "${authSizes}";
  var frames = [
    { srcset: "${auth267Srcset}", type: "image/avif" },
    { srcset: "${auth268Srcset}", type: "image/avif" }
  ];
  for (var i = 0; i < frames.length; i++) {
    var link = document.createElement("link");
    link.rel = "preload";
    link.as = "image";
    link.type = frames[i].type;
    link.imageSrcset = frames[i].srcset;
    link.imageSizes = authSizes;
    link.fetchPriority = "high";
    document.head.appendChild(link);
  }
})();
</script>`;
    tags.push(loginPreloadScript);
  }

  return tags.join("\n    ");
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
