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

  it("returns the Pilot hero blob URL", () => {
    expect(remoteSimpleUrl("pilot/hero")).toContain(
      "images/pilot/hero-1478.webp",
    )
  })

  it("returns Why Tummly illustration blob URLs", () => {
    expect(remoteSimpleUrl("pilot/why-hear")).toContain(
      "images/pilot/why-hear-600.webp",
    )
    expect(remoteSimpleUrl("pilot/why-relationships")).toContain(
      "images/pilot/why-relationships-651.webp",
    )
    expect(remoteSimpleUrl("pilot/why-return")).toContain(
      "images/pilot/why-return-532.webp",
    )
  })

  it("returns See Tummly in Action mockup blob URLs", () => {
    expect(remoteSimpleUrl("pilot/in-action-hear")).toContain(
      "images/pilot/in-action-hear-1500.webp",
    )
    expect(remoteSimpleUrl("pilot/in-action-connected")).toContain(
      "images/pilot/in-action-connected-1500.webp",
    )
    expect(remoteSimpleUrl("pilot/in-action-opportunity")).toContain(
      "images/pilot/in-action-opportunity-1500.webp",
    )
    expect(remoteSimpleUrl("pilot/in-action-working")).toContain(
      "images/pilot/in-action-working-1500.webp",
    )
  })

  it("returns Getting Started stairs blob URL", () => {
    expect(remoteSimpleUrl("pilot/getting-started-steps")).toContain(
      "images/pilot/getting-started-steps-1506.webp",
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
