import { readFileSync } from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

import { describe, expect, it } from "vitest"

const here = path.dirname(fileURLToPath(import.meta.url))
const srcRoot = path.resolve(here, "../..")

function readSrc(relativeFromSrc: string): string {
  return readFileSync(path.join(srcRoot, relativeFromSrc), "utf8")
}

/**
 * Regression: Home hero phone must thread restaurant brandLogoPublicUrl into
 * GuestFeedbackForm. Without it, BrandLogoMark falls back to Building2.
 */
describe("Home guest-form phone brand logo wiring", () => {
  it("HomeGuestFormPhone passes brandLogoPublicUrl to GuestFeedbackForm", () => {
    const source = readSrc(
      "components/dashboard/operator/Home/HomeGuestFormPhone.tsx"
    )
    expect(source).toMatch(
      /<GuestFeedbackForm[\s\S]*?brandLogoPublicUrl=\{brandLogoPublicUrl\}/
    )
  })

  it("HomeHero passes brandLogoPublicUrl to HomeGuestFormPhone", () => {
    const source = readSrc(
      "components/dashboard/operator/Home/HomeHero.tsx"
    )
    expect(source).toMatch(
      /<HomeGuestFormPhone[\s\S]*?brandLogoPublicUrl=\{/
    )
  })

  it("HomePage reads brandLogoPublicUrl from the dashboard outlet", () => {
    const source = readSrc(
      "components/dashboard/operator/Home/HomePage.tsx"
    )
    expect(source).toMatch(/brandLogoPublicUrl/)
    expect(source).toMatch(
      /guestFormPreviewBrandLogoPublicUrl=\{brandLogoPublicUrl\}/
    )
  })
})
