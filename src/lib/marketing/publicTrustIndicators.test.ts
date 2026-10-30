import { describe, expect, it } from "vitest"

import { normalizePublicTrustIndicators } from "./publicTrustIndicators"

function row(overrides: Record<string, unknown> = {}) {
  return {
    itemNumber: 1,
    title: "ICO Data Protection Registration",
    status: "Verified",
    statusLabel: "Registered",
    iconKey: "ico-data",
    imageUrl: null,
    altText: "ICO logo",
    tooltip: "Registered with the ICO",
    cta: null,
    ...overrides,
  }
}

const externalCta = {
  type: "ExternalProof",
  label: "View ICO record",
  href: "https://ico.org.uk/ESDWEB/Pages/x.aspx",
}

function one(overrides: Record<string, unknown>) {
  return normalizePublicTrustIndicators([row(overrides)])
}

describe("normalizePublicTrustIndicators", () => {
  it("returns an empty list for a non-array payload", () => {
    expect(normalizePublicTrustIndicators(null)).toEqual([])
    expect(normalizePublicTrustIndicators({})).toEqual([])
    expect(normalizePublicTrustIndicators("trust")).toEqual([])
  })

  it("keeps a valid verified item unchanged", () => {
    expect(normalizePublicTrustIndicators([row({ cta: externalCta })])).toEqual([
      {
        itemNumber: 1,
        title: "ICO Data Protection Registration",
        status: "Verified",
        statusLabel: "Registered",
        iconKey: "ico-data",
        imageUrl: null,
        altText: "ICO logo",
        tooltip: "Registered with the ICO",
        cta: {
          type: "ExternalProof",
          label: "View ICO record",
          href: "https://ico.org.uk/ESDWEB/Pages/x.aspx",
        },
      },
    ])
  })

  it("omits an item with a missing or invalid itemNumber", () => {
    const invalid: unknown[] = [undefined, 0, -1, 1.5, "3", Number.NaN]
    for (const itemNumber of invalid) {
      expect(one({ itemNumber })).toEqual([])
    }
  })

  it("omits an item with a missing or blank title", () => {
    expect(one({ title: "   " })).toEqual([])
    expect(one({ title: undefined })).toEqual([])
  })

  it("omits an item whose status is unknown, missing or internal", () => {
    expect(one({ status: "Certified" })).toEqual([])
    expect(one({ status: undefined })).toEqual([])
    expect(one({ status: "QaPending" })).toEqual([])
    expect(one({ status: "VerifiedPublicRecord" })).toEqual([])
  })

  it("omits a Verified or Roadmap item with a blank statusLabel", () => {
    expect(one({ statusLabel: "  " })).toEqual([])
    expect(one({ status: "Roadmap", statusLabel: "" })).toEqual([])
  })

  it("forces the UnderReview label to Under review", () => {
    const [item] = one({ status: "UnderReview", statusLabel: "Withdrawn", cta: externalCta })
    expect(item.status).toBe("UnderReview")
    expect(item.statusLabel).toBe("Under review")
    expect(item.cta).toBeNull()
  })

  it("drops a CTA on an item that is not Verified", () => {
    expect(one({ status: "Roadmap", cta: externalCta })[0].cta).toBeNull()
  })

  it("drops an ExternalProof CTA whose href is not https", () => {
    const cta = { ...externalCta, href: "http://ico.org.uk/x" }
    expect(one({ cta })[0].cta).toBeNull()
    expect(one({ cta: { ...externalCta, href: "javascript:alert(1)" } })[0].cta).toBeNull()
  })

  it("accepts an InternalPage CTA only with a single leading slash", () => {
    const page = (href: string) => one({ cta: { type: "InternalPage", label: "Read more", href } })[0].cta
    expect(page("/privacy")).toEqual({ type: "InternalPage", label: "Read more", href: "/privacy" })
    expect(page("privacy")).toBeNull()
    expect(page("//evil.example")).toBeNull()
    expect(page("/a\\b")).toBeNull()
    expect(page("/a b")).toBeNull()
    expect(page("/a\nb")).toBeNull()
  })

  it("drops a CookieSettings CTA that carries an href", () => {
    const withHref = one({ cta: { type: "CookieSettings", label: "Cookie settings", href: "/x" } })
    expect(withHref[0].cta).toBeNull()
    const withoutHref = one({ cta: { type: "CookieSettings", label: "Cookie settings", href: null } })
    expect(withoutHref[0].cta).toEqual({ type: "CookieSettings", label: "Cookie settings", href: null })
  })

  it("drops a CTA with a blank or over-long label", () => {
    expect(one({ cta: { ...externalCta, label: "  " } })[0].cta).toBeNull()
    expect(one({ cta: { ...externalCta, label: "x".repeat(61) } })[0].cta).toBeNull()
    expect(one({ cta: { ...externalCta, label: "x".repeat(60) } })[0].cta).not.toBeNull()
  })

  it("forces imageUrl and cta to null for Roadmap items", () => {
    const [item] = one({
      status: "Roadmap",
      statusLabel: "Planned",
      imageUrl: "https://cdn.example/logo.svg",
      cta: externalCta,
    })
    expect(item.imageUrl).toBeNull()
    expect(item.cta).toBeNull()
  })

  it("sorts by itemNumber", () => {
    const items = normalizePublicTrustIndicators([
      row({ itemNumber: 9, title: "Nine" }),
      row({ itemNumber: 2, title: "Two" }),
      row({ itemNumber: 5, title: "Five" }),
    ])
    expect(items.map((item) => item.itemNumber)).toEqual([2, 5, 9])
  })

  it("keeps the first occurrence of a duplicate itemNumber", () => {
    const items = normalizePublicTrustIndicators([
      row({ itemNumber: 4, title: "First" }),
      row({ itemNumber: 4, title: "Second" }),
    ])
    expect(items).toHaveLength(1)
    expect(items[0].title).toBe("First")
  })

  it("trims strings and defaults altText and tooltip to empty", () => {
    const [item] = normalizePublicTrustIndicators([
      row({ title: "  Title  ", statusLabel: " Label ", altText: undefined, tooltip: undefined }),
    ])
    expect(item.title).toBe("Title")
    expect(item.statusLabel).toBe("Label")
    expect(item.altText).toBe("")
    expect(item.tooltip).toBe("")
  })

  it("skips a null or array element without dropping the rest", () => {
    const items = normalizePublicTrustIndicators([null, [], row({ itemNumber: 3 })])
    expect(items.map((item) => item.itemNumber)).toEqual([3])
  })
})
