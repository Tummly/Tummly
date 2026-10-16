import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import { buildSignupPath } from "@/lib/signupPlanIntent"

/** Marketing Pricing page — banner “For groups” CTA and related links. */
export const MARKETING_PRICING_PATH = "/pricing"

/** Marketing Product page — Guest Loop product overview. */
export const MARKETING_PRODUCT_PATH = "/product"

/** Marketing How it works page. */
export const MARKETING_HOW_IT_WORKS_PATH = "/how-it-works"

/** Marketing QR materials page. */
export const MARKETING_QR_MATERIALS_PATH = "/qr-materials"

export type MarketingNavHref =
  | { kind: "route"; to: string }
  | { kind: "hash"; hash: string }
  | { kind: "placeholder" }
  | { kind: "cookie-settings" }
  | { kind: "external"; href: string }

export type MarketingNavItem = {
  id: string
  label: string
  href: MarketingNavHref
}

export const MARKETING_FAQS_HASH = "#faqs"
export const MARKETING_FAQS_PATH = "/faqs"
export const MARKETING_TRUST_PRIVACY_PATH = "/trust-privacy"
export const MARKETING_AI_ASSISTANT_PATH = "/ai-assistant"

/** Public product walkthrough — fixture UI + Figma tour chrome. */
export const MARKETING_DEMO_PATH = "/product-demo"

export const MARKETING_DEMO_HREF: MarketingNavHref = {
  kind: "route",
  to: MARKETING_DEMO_PATH,
}

export const MARKETING_PRIMARY_NAV: MarketingNavItem[] = [
  {
    id: "product",
    label: "Product",
    href: { kind: "route", to: MARKETING_PRODUCT_PATH },
  },
  {
    id: "how-it-works",
    label: "How it works",
    href: { kind: "route", to: MARKETING_HOW_IT_WORKS_PATH },
  },
  {
    id: "pricing",
    label: "Pricing",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  {
    id: "qr-materials",
    label: "QR materials",
    href: { kind: "route", to: MARKETING_QR_MATERIALS_PATH },
  },
  {
    id: "for-groups",
    label: "For groups",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
]

export const MARKETING_RESOURCES_NAV: MarketingNavItem[] = [
  {
    id: "ai-assistant",
    label: "AI Assistant",
    href: { kind: "route", to: MARKETING_AI_ASSISTANT_PATH },
  },
  {
    id: "trust-privacy",
    label: "Trust & privacy",
    href: { kind: "route", to: MARKETING_TRUST_PRIVACY_PATH },
  },
  {
    id: "faq",
    label: "FAQ",
    href: { kind: "route", to: MARKETING_FAQS_PATH },
  },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
]

export const MARKETING_FOOTER_PRODUCT: MarketingNavItem[] = [
  {
    id: "guest-loop",
    label: "Guest Loop",
    href: { kind: "route", to: MARKETING_PRODUCT_PATH },
  },
  {
    id: "how-it-works",
    label: "How it works",
    href: { kind: "route", to: MARKETING_HOW_IT_WORKS_PATH },
  },
  {
    id: "qr-materials",
    label: "QR materials",
    href: { kind: "route", to: MARKETING_QR_MATERIALS_PATH },
  },
  {
    id: "pricing",
    label: "Pricing",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  {
    id: "ai-assistant",
    label: "AI Assistant",
    href: { kind: "route", to: MARKETING_AI_ASSISTANT_PATH },
  },
  { id: "try-demo", label: "Try demo", href: MARKETING_DEMO_HREF },
]

export const MARKETING_FOOTER_RESTAURANTS: MarketingNavItem[] = [
  {
    id: "30-day-pilot",
    label: "30-day Pilot",
    href: { kind: "route", to: buildSignupPath({ plan: "Pilot" }) },
  },
  {
    id: "multi-location",
    label: "Multi-Location",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  {
    id: "faq",
    label: "FAQ",
    href: { kind: "route", to: MARKETING_FAQS_PATH },
  },
  { id: "log-in", label: "Log in", href: { kind: "route", to: "/login" } },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
]

export const MARKETING_FOOTER_TRUST: MarketingNavItem[] = [
  {
    id: "trust-privacy",
    label: "Trust & privacy",
    href: { kind: "route", to: MARKETING_TRUST_PRIVACY_PATH },
  },
  {
    id: "privacy",
    label: "Privacy",
    href: { kind: "route", to: LEGAL_ROUTES.privacy },
  },
  {
    id: "cookies",
    label: "Cookies",
    href: { kind: "route", to: LEGAL_ROUTES.cookiePolicy },
  },
  {
    id: "accessibility",
    label: "Accessibility",
    href: { kind: "route", to: LEGAL_ROUTES.accessibility },
  },
  {
    id: "acceptable-use",
    label: "Acceptable Use",
    href: { kind: "route", to: LEGAL_ROUTES.acceptableUse },
  },
]

export const MARKETING_FOOTER_COMPANY: MarketingNavItem[] = [
  { id: "about", label: "About", href: { kind: "placeholder" } },
  {
    id: "terms",
    label: "Terms",
    href: { kind: "route", to: LEGAL_ROUTES.terms },
  },
  {
    id: "shop-print-terms",
    label: "Shop & Print Terms",
    href: { kind: "route", to: LEGAL_ROUTES.shopPrintTerms },
  },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
]

export const MARKETING_FOOTER_SOCIAL: MarketingNavItem[] = [
  { id: "instagram", label: "Instagram", href: { kind: "placeholder" } },
  { id: "linkedin", label: "LinkedIn", href: { kind: "placeholder" } },
  { id: "x", label: "X", href: { kind: "placeholder" } },
  { id: "facebook", label: "Facebook", href: { kind: "placeholder" } },
]

export const MARKETING_FOOTER_COLUMNS = [
  { id: "product", title: "Product", items: MARKETING_FOOTER_PRODUCT },
  {
    id: "restaurants",
    title: "Restaurants",
    items: MARKETING_FOOTER_RESTAURANTS,
  },
  { id: "trust", title: "Trust", items: MARKETING_FOOTER_TRUST },
  {
    id: "company",
    title: "Company & Legal",
    items: MARKETING_FOOTER_COMPANY,
  },
  {
    id: "social",
    title: "Find us on social",
    items: MARKETING_FOOTER_SOCIAL,
  },
] as const

export const MARKETING_FOOTER_BOTTOM_LINKS: MarketingNavItem[] = [
  {
    id: "privacy",
    label: "Privacy",
    href: { kind: "route", to: LEGAL_ROUTES.privacy },
  },
  {
    id: "cookies",
    label: "Cookies",
    href: { kind: "route", to: LEGAL_ROUTES.cookiePolicy },
  },
  {
    id: "terms",
    label: "Terms",
    href: { kind: "route", to: LEGAL_ROUTES.terms },
  },
]

export type MarketingTrustCredential = {
  id: string
  title: string
  linkLabel: string
  href: MarketingNavHref
}

/** “Built with trust in mind” credentials (Figma footer). */
export const MARKETING_FOOTER_TRUST_CREDENTIALS: MarketingTrustCredential[] = [
  {
    id: "ico-registration",
    title: "ICO Data Protection Registration",
    linkLabel: "View registration",
    href: { kind: "placeholder" },
  },
  {
    id: "ico-essentials",
    title: "ICO Data Protection Essentials",
    linkLabel: "View record",
    href: { kind: "placeholder" },
  },
  {
    id: "cyber-essentials",
    title: "Cyber Essentials",
    linkLabel: "View certificate",
    href: { kind: "placeholder" },
  },
  {
    id: "cyber-essentials-plus",
    title: "Cyber Essentials Plus",
    linkLabel: "View certificate",
    href: { kind: "placeholder" },
  },
  {
    id: "iso-27001",
    title: "ISO/IEC 27001",
    linkLabel: "View certification",
    href: { kind: "placeholder" },
  },
]

export const MARKETING_FOOTER_FHH_URL = "https://fhh.group"
