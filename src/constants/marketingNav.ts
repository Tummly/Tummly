import { HELP_CENTRE_CONTACT_URL } from "@/config/support";
import { LEGAL_ROUTES } from "@/constants/legalRoutes";

/** Marketing Pricing page. */
export const MARKETING_PRICING_PATH = "/pricing";

/** Marketing Build your own (custom) plan page. */
export const MARKETING_CUSTOM_PLAN_PATH = "/custom-plan";

/** Marketing Product page — Guest Loop product overview. */
export const MARKETING_PRODUCT_PATH = "/product";

/** Marketing How it works page. */
export const MARKETING_HOW_IT_WORKS_PATH = "/how-it-works";

/** Marketing QR materials page. */
export const MARKETING_QR_MATERIALS_PATH = "/qr-materials";

/** Marketing For Groups page — multi-location restaurant groups. */
export const MARKETING_FOR_GROUPS_PATH = "/for-groups";

/** Marketing 30-day Pilot page. */
export const MARKETING_PILOT_PATH = "/30-day-pilot";

/** Marketing About page. */
export const MARKETING_ABOUT_PATH = "/about";

export type MarketingNavHref =
  | { kind: "route"; to: string }
  | { kind: "hash"; hash: string }
  | { kind: "placeholder" }
  | { kind: "cookie-settings" }
  | { kind: "external"; href: string };

export type MarketingNavItem = {
  id: string;
  label: string;
  href: MarketingNavHref;
};

export const MARKETING_FAQS_HASH = "#faqs";
export const MARKETING_FAQS_PATH = "/faqs";
export const MARKETING_TRUST_PRIVACY_PATH = "/trust-privacy";
export const MARKETING_AI_ASSISTANT_PATH = "/ai-assistant";

/** Public product walkthrough — fixture UI + Figma tour chrome. */
export const MARKETING_DEMO_PATH = "/product-demo";

export const MARKETING_DEMO_HREF: MarketingNavHref = {
  kind: "route",
  to: MARKETING_DEMO_PATH,
};

/**
 * Marketing pages that show the fixed "Scan to try on your phone" demo QR.
 * Legal, Help Centre, auth and 404 pages do not.
 */
export const MARKETING_DEMO_QR_PATHS: ReadonlySet<string> = new Set([
  "/",
  MARKETING_PRODUCT_PATH,
  MARKETING_HOW_IT_WORKS_PATH,
  MARKETING_PRICING_PATH,
  MARKETING_CUSTOM_PLAN_PATH,
  MARKETING_PILOT_PATH,
  MARKETING_FOR_GROUPS_PATH,
  MARKETING_QR_MATERIALS_PATH,
  MARKETING_AI_ASSISTANT_PATH,
  MARKETING_TRUST_PRIVACY_PATH,
  MARKETING_ABOUT_PATH,
  MARKETING_FAQS_PATH,
]);

export function showsMarketingDemoQr(pathname: string): boolean {
  const path = pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname;
  return MARKETING_DEMO_QR_PATHS.has(path);
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
    id: "30-day-pilot",
    label: "30-day Pilot",
    href: { kind: "route", to: MARKETING_PILOT_PATH },
  },
  {
    id: "for-groups",
    label: "For groups",
    href: { kind: "route", to: MARKETING_FOR_GROUPS_PATH },
  },
];

export const MARKETING_RESOURCES_NAV: MarketingNavItem[] = [
  {
    id: "about",
    label: "About",
    href: { kind: "route", to: MARKETING_ABOUT_PATH },
  },
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
];

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
    label: "QR Materials",
    href: { kind: "route", to: MARKETING_QR_MATERIALS_PATH },
  },
  {
    id: "ai-assistant",
    label: "AI Assistant",
    href: { kind: "route", to: MARKETING_AI_ASSISTANT_PATH },
  },
  { id: "try-demo", label: "Try demo", href: MARKETING_DEMO_HREF },
];

export const MARKETING_FOOTER_RESTAURANTS: MarketingNavItem[] = [
  {
    id: "pricing",
    label: "Pricing",
    href: { kind: "route", to: MARKETING_PRICING_PATH },
  },
  {
    id: "custom-plan",
    label: "Build your own plan",
    href: { kind: "route", to: MARKETING_CUSTOM_PLAN_PATH },
  },
  {
    id: "30-day-pilot",
    label: "30-day Pilot",
    href: { kind: "route", to: MARKETING_PILOT_PATH },
  },
  {
    id: "for-groups",
    label: "For groups",
    href: { kind: "route", to: MARKETING_FOR_GROUPS_PATH },
  },
  {
    id: "faqs",
    label: "FAQs",
    href: { kind: "route", to: MARKETING_FAQS_PATH },
  },
];

export const MARKETING_FOOTER_SOCIAL: MarketingNavItem[] = [
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
];

/** Companies House public record for TUMMLY.COM LIMITED (16236040). */
export const MARKETING_COMPANY_RECORD_URL =
  "https://find-and-update.company-information.service.gov.uk/company/16236040";

export const MARKETING_FOOTER_COMPANY: MarketingNavItem[] = [
  {
    id: "about",
    label: "About us",
    href: { kind: "route", to: MARKETING_ABOUT_PATH },
  },
  {
    id: "contact",
    label: "Contact",
    href: { kind: "route", to: HELP_CENTRE_CONTACT_URL },
  },
  {
    id: "company-record",
    label: "View company record",
    href: { kind: "external", href: MARKETING_COMPANY_RECORD_URL },
  },
];

/** Privacy Notice section for guests who use Smart Guest Links. */
export const MARKETING_GUEST_PRIVACY_PATH = `${LEGAL_ROUTES.privacy}#smart-guest-links-guest-feedback-and-restaurant-follow-up`;

/** Guest preference centre — guests ask for a manage link by email. */
export const MARKETING_GUEST_PREFERENCES_PATH = "/preferences";

export const MARKETING_FOOTER_LEGAL: MarketingNavItem[] = [
  {
    id: "trust-privacy",
    label: "Trust and privacy",
    href: { kind: "route", to: MARKETING_TRUST_PRIVACY_PATH },
  },
  {
    id: "privacy",
    label: "Privacy Notice",
    href: { kind: "route", to: LEGAL_ROUTES.privacy },
  },
  {
    id: "guest-privacy",
    label: "Privacy for Guests",
    href: { kind: "route", to: MARKETING_GUEST_PRIVACY_PATH },
  },
  {
    id: "marketing-preferences",
    label: "Manage marketing preferences",
    href: { kind: "route", to: MARKETING_GUEST_PREFERENCES_PATH },
  },
  {
    id: "accessibility",
    label: "Accessibility",
    href: { kind: "route", to: LEGAL_ROUTES.accessibility },
  },
  {
    id: "terms",
    label: "Terms of Service",
    href: { kind: "route", to: LEGAL_ROUTES.terms },
  },
  {
    id: "acceptable-use",
    label: "Acceptable Use",
    href: { kind: "route", to: LEGAL_ROUTES.acceptableUse },
  },
  {
    id: "shop-print-terms",
    label: "Shop and Print Terms",
    href: { kind: "route", to: LEGAL_ROUTES.shopPrintTerms },
  },
  {
    id: "cookie-settings",
    label: "Cookie settings",
    href: { kind: "cookie-settings" },
  },
];

/** Statutory website disclosure (Figma footer `5839:73487`), one entry per line. */
export const MARKETING_FOOTER_COMPANY_DISCLOSURE = [
  "Tummly is operated by TUMMLY.COM LIMITED, registered in England and Wales (Company No. 16236040).",
  "Registered office: 71-75 Shelton Street, Covent Garden, London, WC2H 9JQ.",
] as const;

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
];

export const MARKETING_FOOTER_FHH_URL = "https://fhh.group";
