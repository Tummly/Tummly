import type { ReactNode } from "react"

import type { LegalSection } from "./types"

/** Subtitle shared across legal pages in Figma. */
export const LEGAL_PAGE_SUBTITLE =
  "Use QR prompts to collect private feedback, grow your guest list, send return offers and see what's working each week."

/** Table of contents labels from Figma (Privacy Policy frame). */
export const FIGMA_LEGAL_SECTION_TITLES = [
  "1. Who this policy applies to",
  "2. Who we are",
  "3. Our role: controller or processor",
  "4. Personal information we collect",
  "5. How we collect information",
  "6. How we use personal information",
  "7. Lawful bases for processing",
  "8. Guest feedback, QR forms and offers",
  "9. Marketing messages and consent",
  "10. Cookies and similar technologies",
  "11. AI-assisted features",
  "12. Sharing personal information",
  "13. International transfers",
  "14. How long we keep personal information",
  "15. Security",
  "16. Your rights",
  "17. Restaurant customer responsibilities",
  "18. Children",
  "19. Changes to this policy",
  "20. Contact us",
] as const

export function legalSectionIdFromTitle(title: string): string {
  return title
    .replace(/^\d+\.\s*/, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "")
}

/** Section 1 body — extracted from Figma. */
export function figmaWhoThisPolicyAppliesToContent(): ReactNode {
  return (
    <>
      <p>This Privacy Policy applies to:</p>
      <ul>
        <li>visitors to the Tummly website;</li>
        <li>
          restaurants, hospitality operators and their team members using the
          Tummly dashboard;
        </li>
        <li>
          guests who scan a Tummly-powered QR code, submit feedback, join a
          restaurant customer club, claim an offer or receive a message;
        </li>
        <li>
          people who contact us for sales, support, billing or privacy enquiries.
        </li>
      </ul>
      <p>
        This policy does not apply to websites, services or privacy practices
        operated by restaurants, delivery marketplaces, payment providers, email
        providers, SMS providers or other third parties.
      </p>
    </>
  )
}

/** Section 2 body — extracted from Figma. */
export function figmaWhoWeAreContent(): ReactNode {
  return (
    <>
      <p>
        Tummly provides software for restaurants and hospitality operators. Our
        launch product, Tummly Guest Loop, helps operators turn orders, visits
        and deliveries into direct guest relationships using Smart Guest Links,
        QR codes, private feedback, offers, campaigns and weekly insights.
      </p>
      <p>
        Company: Tummly Ltd.
        <br />
        Address: [Insert registered address]
        <br />
        Email: [Insert privacy email, e.g. privacy@tummly.com]
        <br />
        Company number: [Insert if applicable]
      </p>
    </>
  )
}

const FIGMA_SECTION_BODY_BY_INDEX: Record<number, () => ReactNode> = {
  0: figmaWhoThisPolicyAppliesToContent,
  1: figmaWhoWeAreContent,
}

/** Builds the Figma section list: full TOC, body copy where Figma provides it. */
export function buildFigmaLegalSections(): LegalSection[] {
  return FIGMA_LEGAL_SECTION_TITLES.map((title, index) => {
    const body = FIGMA_SECTION_BODY_BY_INDEX[index]?.()

    return {
      id: legalSectionIdFromTitle(title),
      title,
      ...(body ? { content: body } : {}),
    }
  })
}
