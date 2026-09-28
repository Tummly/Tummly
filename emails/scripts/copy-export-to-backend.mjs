import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..")
const outDir = path.join(root, "out")
const backendTemplates = path.resolve(
  root,
  "../backend/TummlyBackend/Assets/emails/templates"
)
const backendAssets = path.resolve(root, "../backend/TummlyBackend/Assets/emails")
const publicEmail = path.resolve(root, "../public/email")

if (!existsSync(outDir)) {
  console.error("Missing emails/out — run `npm run export` first.")
  process.exit(1)
}

mkdirSync(backendTemplates, { recursive: true })
mkdirSync(publicEmail, { recursive: true })

const htmlFiles = readdirSync(outDir).filter((f) => f.endsWith(".html"))
if (htmlFiles.length === 0) {
  console.error("No HTML files in emails/out.")
  process.exit(1)
}

for (const file of htmlFiles) {
  const src = path.join(outDir, file)
  const dest = path.join(backendTemplates, file)
  let html = readFileSync(src, "utf8")

  // Exported preview uses /static/...; C# injects absolute URLs via tokens.
  html = html.replaceAll("/static/tummly-logo-dark.png", "{{logo_url}}")
  html = html.replaceAll(
    "/static/brand-logo-placeholder.png",
    "{{brand_logo_url}}"
  )
  html = html.replaceAll("/static/top-decoration.png", "{{top_decoration_url}}")
  html = html.replaceAll("/static/tummly-wordmark.png", "{{powered_by_logo_url}}")
  html = html.replaceAll("/static/bottom-strip.png", "{{bottom_strip_url}}")

  writeFileSync(dest, html, "utf8")
  console.log(`Wrote ${path.relative(root, dest)}`)
}

const staticSrc = path.join(outDir, "static")
if (existsSync(staticSrc)) {
  for (const file of readdirSync(staticSrc)) {
    const from = path.join(staticSrc, file)
    cpSync(from, path.join(backendAssets, file))
    console.log(`Copied static/${file} → Assets/emails/${file}`)

    // Frontend public CDN path used when Frontend:BaseUrl hosts /email/*
    cpSync(from, path.join(publicEmail, file))
    console.log(`Copied static/${file} → public/email/${file}`)
  }

  // Guest "powered by" uses logo.png; keep a stable public alias.
  const wordmark = path.join(staticSrc, "tummly-wordmark.png")
  const logoPublic = path.join(publicEmail, "logo.png")
  if (existsSync(wordmark) && !existsSync(logoPublic)) {
    cpSync(wordmark, logoPublic)
    console.log("Copied tummly-wordmark.png → public/email/logo.png")
  }
}
