import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import { LEGAL_ROUTES } from "@/constants/legalRoutes"

/** Future Pricing page — banner “For groups” CTA and related links. */
export const MARKETING_PRICING_PATH = "/pricing"

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

export const MARKETING_PRIMARY_NAV: MarketingNavItem[] = [
  { id: "product", label: "Product", href: { kind: "placeholder" } },
  { id: "how-it-works", label: "How it works", href: { kind: "placeholder" } },
  {
    id: "pricing",
    label: "Pricing",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  { id: "qr-materials", label: "QR materials", href: { kind: "placeholder" } },
  {
    id: "for-groups",
    label: "For groups",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
]

export const MARKETING_RESOURCES_NAV: MarketingNavItem[] = [
  { id: "ai-assistant", label: "AI Assistant", href: { kind: "placeholder" } },
  {
    id: "trust-privacy",
    label: "Trust & privacy",
    href: { kind: "placeholder" },
  },
  {
    id: "faq",
    label: "FAQ",
    href: { kind: "hash", hash: MARKETING_FAQS_HASH },
  },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
]

export const MARKETING_FOOTER_PRODUCT: MarketingNavItem[] = [
  { id: "guest-loop", label: "Guest Loop", href: { kind: "placeholder" } },
  { id: "how-it-works", label: "How it works", href: { kind: "placeholder" } },
  {
    id: "pricing",
    label: "Pricing",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  { id: "ai-assistant", label: "AI Assistant", href: { kind: "placeholder" } },
]

export const MARKETING_FOOTER_RESTAURANTS: MarketingNavItem[] = [
  {
    id: "30-day-pilot",
    label: "30-day Pilot",
    href: { kind: "route", to: "/signup" },
  },
  {
    id: "multi-location",
    label: "Multi-Location",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  {
    id: "faq",
    label: "FAQ",
    href: { kind: "hash", hash: MARKETING_FAQS_HASH },
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
    href: { kind: "placeholder" },
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
    href: { kind: "placeholder" },
  },
  {
    id: "acceptable-use",
    label: "Acceptable Use",
    href: { kind: "placeholder" },
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
    href: { kind: "placeholder" },
  },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
]

export const MARKETING_FOOTER_SOCIAL: MarketingNavItem[] = [
  { id: "youtube", label: "YouTube", href: { kind: "placeholder" } },
  { id: "instagram", label: "Instagram", href: { kind: "placeholder" } },
  { id: "tiktok", label: "TikTok", href: { kind: "placeholder" } },
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
