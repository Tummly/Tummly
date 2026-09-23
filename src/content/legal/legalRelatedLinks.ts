import { LEGAL_ROUTES } from "@/constants/legalRoutes"
import type { MarketingNavHref } from "@/constants/marketingNav"

export type LegalRelatedLink = {
  id: string
  title: string
  description: string
  linkLabel: string
  href: MarketingNavHref
}

/** Shared related-legal rows (Figma Privacy Notice `4974:26980`). */
export const LEGAL_RELATED_LINKS: LegalRelatedLink[] = [
  {
    id: "privacy",
    title: "Privacy",
    description: "How Tummly handles personal information.",
    linkLabel: "Read Privacy Notice",
    href: { kind: "route", to: LEGAL_ROUTES.privacy },
  },
  {
    id: "cookies",
    title: "Cookies",
    description:
      "How cookies and similar technologies are used on Tummly websites and services.",
    linkLabel: "Read Cookie Notice",
    href: { kind: "route", to: LEGAL_ROUTES.cookiePolicy },
  },
  {
    id: "accessibility",
    title: "Accessibility",
    description: "How we're working to make Tummly usable by more people.",
    linkLabel: "Read Accessibility statement",
    href: { kind: "route", to: LEGAL_ROUTES.accessibility },
  },
  {
    id: "acceptable-use",
    title: "Acceptable Use",
    description:
      "The rules that help keep Tummly safe, permission-aware and useful.",
    linkLabel: "Read Acceptable Use Policy",
    href: { kind: "route", to: LEGAL_ROUTES.acceptableUse },
  },
]
