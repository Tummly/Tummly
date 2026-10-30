import { describe, expect, it } from "vitest"

import {
  MARKETING_FOOTER_BOTTOM_LINKS,
  MARKETING_FOOTER_COMPANY_DISCLOSURE,
  MARKETING_FOOTER_LEGAL,
  MARKETING_FOOTER_SOCIAL,
} from "@/constants/marketingNav"

describe("marketing footer", () => {
  it("links each social profile to its real Tummly account", () => {
    expect(MARKETING_FOOTER_SOCIAL).toEqual([
      {
        id: "instagram",
        label: "Instagram",
        href: { kind: "external", href: "https://www.instagram.com/tummlycom/" },
      },
      {
        id: "linkedin",
        label: "LinkedIn",
        href: {
          kind: "external",
          href: "https://www.linkedin.com/company/tummlycom",
        },
      },
      {
        id: "facebook",
        label: "Facebook",
        href: { kind: "external", href: "https://www.facebook.com/Tummlycom" },
      },
    ])
  })

  it("offers one Cookie settings item, in the Company & Legal column", () => {
    const allItems = [...MARKETING_FOOTER_LEGAL, ...MARKETING_FOOTER_BOTTOM_LINKS]
    const cookieSettings = allItems.filter(
      (item) => item.href.kind === "cookie-settings"
    )

    expect(cookieSettings).toEqual([
      {
        id: "cookie-settings",
        label: "Cookie settings",
        href: { kind: "cookie-settings" },
      },
    ])
    expect(MARKETING_FOOTER_LEGAL.at(-1)?.id).toBe("cookie-settings")
  })

  // Trust Footer spec (22 Oct 2026): statutory company disclosure.
  it("states the registered company details", () => {
    expect(MARKETING_FOOTER_COMPANY_DISCLOSURE).toEqual([
      "Tummly is operated by TUMMLY.COM LIMITED, registered in England and Wales (Company No. 16236040).",
      "Registered office: 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ.",
    ])
  })
})
