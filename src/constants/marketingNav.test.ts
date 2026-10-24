import { describe, expect, it } from "vitest"

import {
  MARKETING_FOOTER_COMPANY_DISCLOSURE,
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

  // Trust Footer spec (22 Oct 2026): statutory company disclosure.
  it("states the registered company details", () => {
    expect(MARKETING_FOOTER_COMPANY_DISCLOSURE).toBe(
      "TUMMLY.COM LIMITED (trading as Tummly.com). Registered in England and Wales, company no. 16236040. Registered office: 71-75 Shelton Street, Covent Garden, London WC2H 9JQ."
    )
  })
})
