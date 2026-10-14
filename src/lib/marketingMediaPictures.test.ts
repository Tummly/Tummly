import { describe, expect, it } from "vitest"

import {
  remotePicture,
  remoteSimpleImg,
  remoteSimpleUrl,
} from "@/lib/marketingMediaPictures"

describe("remotePicture", () => {
  it("builds AVIF/WebP/fallback srcsets for the hero", () => {
    const picture = remotePicture("hero/hero-bg")
    expect(picture.sources.avif).toContain("hero-bg-1920.avif")
    expect(picture.sources.avif).toContain(" 1920w")
    expect(picture.sources.webp).toContain("hero-bg-1024.webp")
    expect(picture.img.src).toContain("hero-bg-1920.png")
    expect(picture.img.w).toBeGreaterThan(0)
    expect(picture.img.h).toBeGreaterThan(0)
  })
})

describe("remoteSimpleUrl", () => {
  it("returns the primary webp for pricing peeks", () => {
    expect(remoteSimpleUrl("pricing/qr-peek-01")).toContain(
      "qr-peek-01-1024.webp",
    )
  })
})

describe("remoteSimpleImg", () => {
  it("returns src and multi-width srcset for pricing peeks", () => {
    const img = remoteSimpleImg("pricing/qr-peek-01")
    expect(img.src).toContain("qr-peek-01-1024.webp")
    expect(img.srcSet).toContain("640w")
    expect(img.srcSet).toContain("1024w")
    expect(img.width).toBeGreaterThan(0)
    expect(img.height).toBeGreaterThan(0)
  })
})
