// Public trust indicator payloads for `GET /api/public/trust-indicators` (CONTRACT §8).
// Tooltip and status copy is the approved seed text from
// `backend/TummlyBackend/TrustIndicators/TrustIndicatorCatalogSeed.cs`.
// Specs stub the endpoint with these; the real API is never needed.

export type TrustIndicatorFixtureItem = {
  itemNumber: number
  title: string
  status: string
  statusLabel: string
  iconKey: string | null
  imageUrl: string | null
  altText: string
  tooltip: string
  cta: { type: string; label: string; href: string | null } | null
}

/** The stub is cross-origin (dev server to the API origin), so it must allow it. */
export const CORS_HEADERS = { "Access-Control-Allow-Origin": "*" }

export const TRUST_ICO_REGISTRATION_URL = "https://ico.org.uk/ESDWebPages/Entry/ZC270005"
export const TRUST_COMPANY_RECORD_URL =
  "https://find-and-update.company-information.service.gov.uk/company/16236040"

const ICO_REGISTRATION: TrustIndicatorFixtureItem = {
  itemNumber: 1,
  title: "ICO Data Protection Registration",
  status: "Verified",
  statusLabel: "Registered",
  iconKey: "ico-data",
  imageUrl: null,
  altText: "",
  tooltip:
    "Tummly.com Limited is registered with the Information Commissioner’s Office under reference ZC270005. This registration does not certify compliance with UK GDPR.",
  cta: { type: "ExternalProof", label: "View registration", href: TRUST_ICO_REGISTRATION_URL },
}

const CYBER_ESSENTIALS_ROADMAP: TrustIndicatorFixtureItem = {
  itemNumber: 3,
  title: "Cyber Essentials",
  status: "Roadmap",
  statusLabel: "On our roadmap",
  iconKey: "roadmap-cyber-essentials",
  imageUrl: null,
  altText: "",
  tooltip:
    "Cyber Essentials certification is on Tummly’s cybersecurity roadmap. Tummly has not yet been assessed or certified under this scheme.",
  cta: null,
}

const UK_GDPR_UNDER_REVIEW: TrustIndicatorFixtureItem = {
  itemNumber: 9,
  title: "UK GDPR & Data Protection",
  status: "UnderReview",
  statusLabel: "Under review",
  iconKey: "gdpr-uk",
  imageUrl: null,
  altText: "",
  tooltip:
    "Our privacy notice describes how Tummly handles personal information, its relevant responsibilities and how privacy requests are handled. UK GDPR is a legal framework, not a general badge or certification.",
  cta: null,
}

const COOKIE_CHOICES: TrustIndicatorFixtureItem = {
  itemNumber: 10,
  title: "Cookie & Privacy Choices",
  status: "Verified",
  statusLabel: "Verified",
  iconKey: "privacy-choices",
  imageUrl: null,
  altText: "",
  tooltip:
    "Visitors can read about site cookies and change their non-essential cookie choices using the website’s controls. This is not a third-party certification.",
  cta: { type: "CookieSettings", label: "Manage cookie choices", href: null },
}

const UK_REGISTERED_COMPANY: TrustIndicatorFixtureItem = {
  itemNumber: 13,
  title: "UK Registered Company",
  status: "Verified",
  statusLabel: "Verified public record",
  iconKey: "uk-company",
  imageUrl: null,
  altText: "",
  tooltip:
    "Tummly.com Limited is an active private limited company registered in England and Wales, company number 16236040. View the official Companies House record.",
  cta: { type: "ExternalProof", label: "View company record", href: TRUST_COMPANY_RECORD_URL },
}

/** Not a valid status: the client must drop it, so it never renders. */
export const TRUST_BOGUS_TITLE = "Retired certificate that must not show"
const BOGUS_STATUS: TrustIndicatorFixtureItem = {
  itemNumber: 2,
  title: TRUST_BOGUS_TITLE,
  status: "Bogus",
  statusLabel: "Bogus",
  iconKey: null,
  imageUrl: null,
  altText: "",
  tooltip: "This row has a status the public contract does not allow.",
  cta: null,
}

/** Baseline published set: 01, 03, 09, 10, 13, plus one invalid row. */
export const TRUST_INDICATORS_FIXTURE: TrustIndicatorFixtureItem[] = [
  ICO_REGISTRATION,
  BOGUS_STATUS,
  CYBER_ESSENTIALS_ROADMAP,
  UK_GDPR_UNDER_REVIEW,
  COOKIE_CHOICES,
  UK_REGISTERED_COMPANY,
]

/** Later publication: item 01 is unpublished (the API omits it). */
export const TRUST_INDICATORS_WITHOUT_ICO: TrustIndicatorFixtureItem[] =
  TRUST_INDICATORS_FIXTURE.filter((item) => item.itemNumber !== ICO_REGISTRATION.itemNumber)

export const TRUST_TOOLTIPS = {
  ico: ICO_REGISTRATION.tooltip,
  cyberEssentials: CYBER_ESSENTIALS_ROADMAP.tooltip,
  ukGdpr: UK_GDPR_UNDER_REVIEW.tooltip,
  cookieChoices: COOKIE_CHOICES.tooltip,
  companyRecord: UK_REGISTERED_COMPANY.tooltip,
} as const
