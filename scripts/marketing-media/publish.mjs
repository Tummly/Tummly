#!/usr/bin/env node
/**
 * Generate responsive marketing image variants and upload to Azure Blob.
 *
 * Usage (from repo root):
 *   node scripts/marketing-media/publish.mjs
 *   node scripts/marketing-media/publish.mjs --skip-upload   # generate only
 *   node scripts/marketing-media/publish.mjs --skip-generate # upload existing out/
 *
 * Requires: sharp (devDep), az CLI (Docker-wrapped — files must live under repo).
 * Account: sttummlyqavfavue / container: marketing
 */

import { spawnSync } from "node:child_process"
import { createHash } from "node:crypto"
import {
  existsSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

import sharp from "sharp"

import { PICTURE_ASSETS, SIMPLE_ASSETS } from "./manifest.mjs"

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const ROOT = path.resolve(__dirname, "../..")
const OUT_DIR = path.join(ROOT, ".scratch/marketing-media-out")
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

const args = new Set(process.argv.slice(2))
const skipUpload = args.has("--skip-upload")
const skipGenerate = args.has("--skip-generate")

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

  const maxWidth = Math.max(...asset.widths)
  const aspect = srcH / srcW
  meta[asset.id] = {
    w: Math.min(maxWidth, srcW),
    h: Math.round(Math.min(maxWidth, srcW) * aspect),
    blobDir: asset.blobDir,
    baseName: asset.baseName,
    widths: asset.widths,
    formats: asset.formats,
  }

  for (const width of asset.widths) {
    const targetW = Math.min(width, srcW)
    for (const format of asset.formats) {
      const fileName = variantFileName(asset.baseName, width, format)
      const outPath = path.join(outDir, fileName)
      const pipeline = sharp(absSource, { failOn: "none" }).resize({
        width: targetW,
        withoutEnlargement: true,
      })
      await encode(pipeline, format, asset.quality).toFile(outPath)
      process.stdout.write(`  wrote ${blobPath(asset.blobDir, fileName)}\n`)
    }
  }

  if ("primaryFormat" in asset && asset.primaryFormat) {
    const primaryWidth = Math.max(...asset.widths)
    meta[asset.id].primary = variantFileName(
      asset.baseName,
      primaryWidth,
      asset.primaryFormat,
    )
  }
}

async function generateAll() {
  if (existsSync(OUT_DIR)) {
    rmSync(OUT_DIR, { recursive: true, force: true })
  }
  ensureDir(OUT_DIR)

  /** @type {Record<string, object>} */
  const meta = {}

  process.stdout.write("Generating picture assets…\n")
  for (const asset of PICTURE_ASSETS) {
    process.stdout.write(`- ${asset.id}\n`)
    await generateAsset(asset, meta)
    meta[asset.id].kind = "picture"
    meta[asset.id].fallbackFormat =
      /** @type {import("./manifest.mjs").PictureAsset} */ (asset).fallbackFormat
  }

  process.stdout.write("Generating simple assets…\n")
  for (const asset of SIMPLE_ASSETS) {
    process.stdout.write(`- ${asset.id}\n`)
    await generateAsset(asset, meta)
    meta[asset.id].kind = "simple"
    meta[asset.id].primaryFormat = asset.primaryFormat
  }

  writeFileSync(META_PATH, `${JSON.stringify(meta, null, 2)}\n`, "utf8")
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
  process.stdout.write(
    `Uploading batch from ${sourceRel}/ → ${CONTAINER}/ (cache: ${CACHE_CONTROL})…\n`,
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
  if (!skipGenerate) {
    await generateAll()
  } else if (!existsSync(OUT_DIR)) {
    throw new Error("OUT_DIR missing; cannot --skip-generate")
  }

  if (!skipUpload) {
    uploadAll()
  }

  // Fingerprint sources so CI/docs can detect drift.
  const hash = createHash("sha256")
  for (const asset of [...PICTURE_ASSETS, ...SIMPLE_ASSETS]) {
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
