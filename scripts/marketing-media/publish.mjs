#!/usr/bin/env node
/**
 * Generate responsive marketing image variants and upload to Azure Blob.
 *
 * Usage (from repo root):
 *   node scripts/marketing-media/publish.mjs
 *   node scripts/marketing-media/publish.mjs --skip-upload   # generate only
 *   node scripts/marketing-media/publish.mjs --skip-generate # upload existing out/
 *   node scripts/marketing-media/publish.mjs --new           # only ids missing from meta
 *   node scripts/marketing-media/publish.mjs --only product  # id or id-prefix (repeatable)
 *
 * Requires: sharp (devDep), az CLI (Docker-wrapped — files must live under repo).
 * Account: sttummlyqavfavue / container: marketing
 */

import { spawnSync } from "node:child_process"
import { createHash } from "node:crypto"
import {
  copyFileSync,
  existsSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  rmSync,
  unlinkSync,
  writeFileSync,
} from "node:fs"
import { tmpdir } from "node:os"
import path from "node:path"
import { fileURLToPath } from "node:url"

import sharp from "sharp"

import { PICTURE_ASSETS, SIMPLE_ASSETS } from "./manifest.mjs"

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const ROOT = path.resolve(__dirname, "../..")
/** Override with MARKETING_MEDIA_OUT when running parallel --only publishes. */
const OUT_DIR = process.env.MARKETING_MEDIA_OUT
  ? path.resolve(process.env.MARKETING_MEDIA_OUT)
  : path.join(ROOT, ".scratch/marketing-media-out")
const META_PATH = path.join(ROOT, "src/lib/marketingMediaMeta.json")
const ACCOUNT = "sttummlyqavfavue"
const CONTAINER = "marketing"
const CACHE_CONTROL = "public,max-age=31536000,immutable"

/** @type {Record<string, string>} */
const CONTENT_TYPE_BY_EXT = {
  ".avif": "image/avif",
  ".webp": "image/webp",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".mp4": "video/mp4",
}

/**
 * @param {string[]} argv
 * @returns {{
 *   skipUpload: boolean,
 *   skipGenerate: boolean,
 *   newOnly: boolean,
 *   onlyFilters: string[],
 * }}
 */
function parseArgs(argv) {
  let skipUpload = false
  let skipGenerate = false
  let newOnly = false
  /** @type {string[]} */
  const onlyFilters = []

  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i]
    if (arg === "--skip-upload") {
      skipUpload = true
      continue
    }
    if (arg === "--skip-generate") {
      skipGenerate = true
      continue
    }
    if (arg === "--new") {
      newOnly = true
      continue
    }
    if (arg === "--only") {
      const value = argv[i + 1]
      if (value == null || value.startsWith("-")) {
        throw new Error("--only requires an asset id or prefix (e.g. product)")
      }
      onlyFilters.push(value)
      i += 1
      continue
    }
    if (arg.startsWith("--only=")) {
      const value = arg.slice("--only=".length).trim()
      if (value === "") {
        throw new Error("--only= requires an asset id or prefix")
      }
      onlyFilters.push(value)
      continue
    }
    throw new Error(`Unknown argument: ${arg}`)
  }

  return { skipUpload, skipGenerate, newOnly, onlyFilters }
}

const { skipUpload, skipGenerate, newOnly, onlyFilters } = parseArgs(
  process.argv.slice(2),
)

function ensureDir(dir) {
  mkdirSync(dir, { recursive: true })
}

/**
 * @param {string} dir
 * @returns {string[]} relative paths under OUT_DIR (posix-style)
 */
function listOutFiles(dir = OUT_DIR, prefix = "") {
  /** @type {string[]} */
  const files = []
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const rel = prefix ? `${prefix}/${entry.name}` : entry.name
    if (entry.isDirectory()) {
      files.push(...listOutFiles(path.join(dir, entry.name), rel))
    } else {
      files.push(rel)
    }
  }
  return files
}

function variantFileName(baseName, width, format) {
  return `${baseName}-${width}.${format}`
}

function blobPath(blobDir, fileName) {
  return `${blobDir}/${fileName}`
}

/** @returns {Record<string, object>} */
function readExistingMeta() {
  if (!existsSync(META_PATH)) {
    return {}
  }
  return /** @type {Record<string, object>} */ (
    JSON.parse(readFileSync(META_PATH, "utf8"))
  )
}

/**
 * @param {{ id: string }} asset
 * @param {string[]} filters
 */
function matchesOnlyFilter(asset, filters) {
  if (filters.length === 0) {
    return true
  }
  return filters.some(
    (filter) => asset.id === filter || asset.id.startsWith(filter),
  )
}

/**
 * @param {typeof PICTURE_ASSETS | typeof SIMPLE_ASSETS} assets
 * @param {Record<string, object>} existingMeta
 */
function selectAssets(assets, existingMeta) {
  return assets.filter((asset) => {
    if (!matchesOnlyFilter(asset, onlyFilters)) {
      return false
    }
    if (newOnly && existingMeta[asset.id] != null) {
      return false
    }
    return true
  })
}

/**
 * @param {import("sharp").Sharp} pipeline
 * @param {string} format
 * @param {number} quality
 */
function encode(pipeline, format, quality) {
  switch (format) {
    case "avif":
      return pipeline.avif({ quality, effort: 4 })
    case "webp":
      return pipeline.webp({ quality })
    case "png":
      return pipeline.png({ compressionLevel: 9 })
    case "jpg":
      return pipeline.jpeg({ quality, mozjpeg: true })
    default:
      throw new Error(`Unknown format: ${format}`)
  }
}

/**
 * @param {{ source: string, blobDir: string, baseName: string, widths: number[], formats: string[], quality: number }} asset
 * @param {Record<string, { w: number, h: number, primary?: string }>} meta
 */
async function generateAsset(asset, meta) {
  const absSource = path.join(ROOT, asset.source)
  if (!existsSync(absSource)) {
    throw new Error(`Missing source: ${asset.source}`)
  }

  const image = sharp(absSource, { failOn: "none" })
  const { width: srcW = 0, height: srcH = 0 } = await image.metadata()
  if (srcW < 1 || srcH < 1) {
    throw new Error(`Invalid dimensions for ${asset.source}`)
  }

  const outDir = path.join(OUT_DIR, asset.blobDir)
  ensureDir(outDir)

  // Never emit a variant wider than the master (avoids `*-1532.webp` that is
  // actually 766px). Drop requested widths above srcW and warn.
  const usableWidths = [
    ...new Set(
      asset.widths
        .map((width) => Math.min(width, srcW))
        .filter((width) => width >= 1),
    ),
  ].sort((a, b) => a - b)
  const skipped = asset.widths.filter((width) => width > srcW)
  if (skipped.length > 0) {
    process.stdout.write(
      `  warn ${asset.id}: source ${srcW}px — skip widths ${skipped.join(", ")} (need larger master)\n`,
    )
  }

  const maxWidth = Math.max(...usableWidths)
  const aspect = srcH / srcW
  meta[asset.id] = {
    w: maxWidth,
    h: Math.round(maxWidth * aspect),
    blobDir: asset.blobDir,
    baseName: asset.baseName,
    widths: usableWidths,
    formats: asset.formats,
  }

  for (const width of usableWidths) {
    for (const format of asset.formats) {
      const fileName = variantFileName(asset.baseName, width, format)
      const outPath = path.join(outDir, fileName)
      const pipeline = sharp(absSource, { failOn: "none" }).resize({
        width,
        withoutEnlargement: true,
      })
      // Encode to OS temp, then copy into OUT_DIR. Direct HEIF writes on some
      // external mounts fail mid-encode; also re-ensure the out dir each file.
      const bytes = await encode(pipeline, format, asset.quality).toBuffer()
      const tmpPath = path.join(
        tmpdir(),
        `marketing-media-${process.pid}-${fileName}`,
      )
      writeFileSync(tmpPath, bytes)
      ensureDir(outDir)
      copyFileSync(tmpPath, outPath)
      unlinkSync(tmpPath)
      process.stdout.write(`  wrote ${blobPath(asset.blobDir, fileName)}\n`)
    }
  }

  if ("primaryFormat" in asset && asset.primaryFormat) {
    const primaryWidth = Math.max(...usableWidths)
    meta[asset.id].primary = variantFileName(
      asset.baseName,
      primaryWidth,
      asset.primaryFormat,
    )
  }
}

/**
 * @param {boolean} filtered
 */
async function generateSelected(filtered) {
  const existingMeta = readExistingMeta()
  const pictureAssets = selectAssets(PICTURE_ASSETS, existingMeta)
  const simpleAssets = selectAssets(SIMPLE_ASSETS, existingMeta)

  if (pictureAssets.length === 0 && simpleAssets.length === 0) {
    const hint = newOnly
      ? "No missing assets (meta already has every manifest id)."
      : onlyFilters.length > 0
        ? `No assets matched --only ${onlyFilters.join(", ")}.`
        : "No assets selected."
    throw new Error(hint)
  }

  if (existsSync(OUT_DIR)) {
    rmSync(OUT_DIR, { recursive: true, force: true })
  }
  ensureDir(OUT_DIR)

  /** @type {Record<string, object>} */
  const generatedMeta = {}

  if (pictureAssets.length > 0) {
    process.stdout.write(
      filtered
        ? `Generating ${pictureAssets.length} picture asset(s)…\n`
        : "Generating picture assets…\n",
    )
    for (const asset of pictureAssets) {
      process.stdout.write(`- ${asset.id}\n`)
      await generateAsset(asset, generatedMeta)
      generatedMeta[asset.id].kind = "picture"
      generatedMeta[asset.id].fallbackFormat =
        /** @type {import("./manifest.mjs").PictureAsset} */ (asset).fallbackFormat
    }
  }

  if (simpleAssets.length > 0) {
    process.stdout.write(
      filtered
        ? `Generating ${simpleAssets.length} simple asset(s)…\n`
        : "Generating simple assets…\n",
    )
    for (const asset of simpleAssets) {
      process.stdout.write(`- ${asset.id}\n`)
      await generateAsset(asset, generatedMeta)
      generatedMeta[asset.id].kind = "simple"
      generatedMeta[asset.id].primaryFormat = asset.primaryFormat
    }
  }

  const nextMeta = filtered
    ? { ...existingMeta, ...generatedMeta }
    : generatedMeta

  writeFileSync(META_PATH, `${JSON.stringify(nextMeta, null, 2)}\n`, "utf8")
  process.stdout.write(`Wrote meta → ${path.relative(ROOT, META_PATH)}\n`)
}

function azKey() {
  const result = spawnSync(
    "az",
    [
      "storage",
      "account",
      "keys",
      "list",
      "-n",
      ACCOUNT,
      "--query",
      "[0].value",
      "-o",
      "tsv",
    ],
    { encoding: "utf8" },
  )
  if (result.status !== 0) {
    throw new Error(`az keys list failed: ${result.stderr || result.stdout}`)
  }
  return result.stdout.trim()
}

function uploadAll() {
  if (!existsSync(OUT_DIR)) {
    throw new Error(`No files in ${OUT_DIR}. Run without --skip-generate first.`)
  }

  const accountKey = azKey()
  const sourceRel = path.relative(ROOT, OUT_DIR)
  const fileCount = listOutFiles().length
  process.stdout.write(
    `Uploading ${fileCount} file(s) from ${sourceRel}/ → ${CONTAINER}/ (cache: ${CACHE_CONTROL})…\n`,
  )

  const result = spawnSync(
    "az",
    [
      "storage",
      "blob",
      "upload-batch",
      "--account-name",
      ACCOUNT,
      "--destination",
      CONTAINER,
      "--source",
      sourceRel,
      "--auth-mode",
      "key",
      "--account-key",
      accountKey,
      "--overwrite",
      "true",
      "--content-cache-control",
      CACHE_CONTROL,
    ],
    { encoding: "utf8", cwd: ROOT },
  )
  if (result.status !== 0) {
    throw new Error(`upload-batch failed: ${result.stderr || result.stdout}`)
  }
  process.stdout.write(result.stdout || "")
  process.stdout.write("Batch upload complete.\n")

  // upload-batch often leaves WebP/AVIF as application/octet-stream — fix MIME.
  repairContentTypes(accountKey)
}

/**
 * Set correct Content-Type + Cache-Control on every uploaded variant.
 * Host `az` is Docker-wrapped (one container per call) — run all updates in
 * a single azure-cli container instead.
 * @param {string} accountKey
 */
function repairContentTypes(accountKey) {
  const files = listOutFiles()
  const listPath = path.join(ROOT, ".scratch/marketing-media-content-types.tsv")
  ensureDir(path.dirname(listPath))

  const lines = []
  for (const rel of files) {
    const ext = path.extname(rel).toLowerCase()
    const contentType = CONTENT_TYPE_BY_EXT[ext]
    if (contentType == null) {
      process.stdout.write(`  skip (unknown ext): ${rel}\n`)
      continue
    }
    lines.push(`${rel}\t${contentType}`)
  }
  writeFileSync(listPath, `${lines.join("\n")}\n`, "utf8")
  process.stdout.write(
    `Repairing content-types for ${lines.length} blob(s) in one az container…\n`,
  )

  const listRel = path.relative(ROOT, listPath)
  const script = [
    "set -euo pipefail",
    `KEY=${JSON.stringify(accountKey)}`,
    `CACHE=${JSON.stringify(CACHE_CONTROL)}`,
    `ACCOUNT=${JSON.stringify(ACCOUNT)}`,
    `CONTAINER=${JSON.stringify(CONTAINER)}`,
    "n=0",
    `while IFS=$'\\t' read -r name ctype; do`,
    '  [ -n "$name" ] || continue',
    "  az storage blob update \\",
    '    --account-name "$ACCOUNT" \\',
    '    --container-name "$CONTAINER" \\',
    '    --name "$name" \\',
    '    --content-type "$ctype" \\',
    '    --content-cache-control "$CACHE" \\',
    '    --auth-mode key \\',
    '    --account-key "$KEY" \\',
    "    -o none",
    '  n=$((n+1))',
    '  if (( n % 25 == 0 )); then echo "  updated $n"; fi',
    `done < ${JSON.stringify(listRel)}`,
    'echo "Content-types repaired ($n)."',
  ].join("\n")

  const result = spawnSync(
    "docker",
    [
      "run",
      "--rm",
      "-v",
      `${process.env.HOME}/.azure:/root/.azure`,
      "-v",
      `${ROOT}:/work`,
      "-w",
      "/work",
      "-e",
      "AZURE_CONFIG_DIR=/root/.azure",
      "mcr.microsoft.com/azure-cli:latest",
      "bash",
      "-lc",
      script,
    ],
    { encoding: "utf8", cwd: ROOT },
  )
  if (result.status !== 0) {
    throw new Error(
      `content-type repair failed: ${result.stderr || result.stdout}`,
    )
  }
  process.stdout.write(result.stdout || "")
}

async function main() {
  const filtered = newOnly || onlyFilters.length > 0

  if (!skipGenerate) {
    await generateSelected(filtered)
  } else if (!existsSync(OUT_DIR)) {
    throw new Error("OUT_DIR missing; cannot --skip-generate")
  }

  if (!skipUpload) {
    uploadAll()
  }

  // Fingerprint sources in scope so CI/docs can detect drift.
  const inScope = [...PICTURE_ASSETS, ...SIMPLE_ASSETS].filter((asset) =>
    matchesOnlyFilter(asset, onlyFilters),
  )
  const hash = createHash("sha256")
  for (const asset of inScope) {
    hash.update(asset.source)
    hash.update(readFileSync(path.join(ROOT, asset.source)))
  }
  process.stdout.write(`Source fingerprint: ${hash.digest("hex").slice(0, 16)}\n`)
  process.stdout.write("Done.\n")
}

main().catch((error) => {
  console.error(error)
  process.exit(1)
})
