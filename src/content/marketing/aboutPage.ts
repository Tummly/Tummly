import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"

/** Shared section inset — matches Product / QR materials (20px mobile, 60px desktop). */
export const ABOUT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Figma About hero `5673:24144` — top pad 70px desktop; no bottom pad under image. */
export const ABOUT_HERO_Y = "pt-12.5 lg:pt-[70px]"

/** Figma About hero eyebrow — *headline* Jakarta 14 / 16, #4a4a4a. */
export const ABOUT_HERO_EYEBROW =
  "font-jakarta text-sm font-medium leading-4 tracking-normal text-[#4a4a4a] uppercase"

/** Figma About hero title — *headline* Jakarta 54 / 60 semibold, #0e0e0e. */
export const ABOUT_HERO_HEADING =
  "font-jakarta text-[36px] font-semibold leading-none tracking-normal text-[#0e0e0e] lg:text-[54px] lg:leading-[60px]"

/** Figma About hero body — *functional* Helvetica Neue 16 / 22, #4a4a4a. */
export const ABOUT_HERO_BODY =
  "font-sans text-base font-normal leading-[22px] tracking-normal text-[#4a4a4a]"

/** Figma About Tummly hero (`5673:24144`). */
export const ABOUT_HERO = {
  eyebrow: "About Tummly",
  title: "Built for restaurants that want stronger guest relationships.",
  body: "Tummly helps restaurant teams hear directly from guests, build their own guest relationships and use every visit to improve the experience and bring more guests back.",
  imageAlt:
    "Abstract green and lime hospitality editorial texture for the About Tummly page.",
} as const

/** Figma About Industries section padding — 100px top desktop (`5685:35427`). */
export const ABOUT_INDUSTRIES_Y = "pt-12.5 lg:pt-[100px]"

/** Section title — *headline* Jakarta 46 / 52 medium, #141414. */
export const ABOUT_SECTION_HEADING =
  "font-jakarta text-[34px] font-medium leading-none tracking-normal text-[#141414] lg:text-[46px] lg:leading-[52px]"

/** Section body — *functional* Helvetica Neue 16 / 22, #141414. */
export const ABOUT_SECTION_BODY =
  "font-sans text-base font-normal leading-[22px] tracking-normal text-[#141414]"

export type AboutIndustry = {
  id: "takeaway" | "cafes" | "casual" | "groups"
  title: string
  body: string
  imageAlt: string
}

/** Figma About Industries (`5685:35427`). */
export const ABOUT_INDUSTRIES = {
  eyebrow: "Who we're building for",
  title: "Independent restaurants. Growing groups. Real operating pressures.",
  paragraphs: [
    "Tummly is built for independent restaurants and growing restaurant groups that want practical ways to understand guests, build stronger relationships and bring more of them back.",
    "These businesses do not always have large marketing teams, complex CRM systems or time to manage complicated software. Tummly is designed to fit around the way restaurants already operate.",
  ],
  items: [
    {
      id: "takeaway",
      title: "Takeaway & QSR",
      body: "Counters, collection points, packaging and delivery touchpoints.",
      imageAlt: "Staff using a counter POS in a takeaway restaurant",
    },
    {
      id: "cafes",
      title: "Cafés & bakeries",
      body: "Counters, tables, receipts and everyday guest interactions.",
      imageAlt: "Busy café counter with guests and pastry display",
    },
    {
      id: "casual",
      title: "Casual dining",
      body: "Tables, receipts and post-visit Feedback.",
      imageAlt: "Guests dining together at a casual restaurant table",
    },
    {
      id: "groups",
      title: "Growing restaurant groups",
      body: "Location-level Guest Loop activity with wider group visibility and appropriate access controls.",
      imageAlt: "Server speaking with guests in a busy dining room",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  paragraphs: readonly string[]
  items: readonly AboutIndustry[]
}

/** Figma Restaurant reality section pad — 90px desktop (`5673:24194`). */
export const ABOUT_REALITY_Y = "py-12.5 lg:py-[90px]"

export type AboutRealityItem = {
  id: "time" | "physical" | "systems" | "growth"
  title: string
  body: string
  imageAlt: string
}

/** Figma Restaurant reality as a design philosophy (`5673:24194`). */
export const ABOUT_REALITY = {
  eyebrow: "We build around restaurant reality",
  title: "Built for the way restaurants actually work.",
  items: [
    {
      id: "time",
      title: "Time is limited",
      body: "Tummly is easy to understand and use without a dedicated marketing or technology team.",
      imageAlt: "Isometric illustration of a stopwatch on a platform",
    },
    {
      id: "physical",
      title: "Physical still matters",
      body: "Restaurant relationships start in real places, so QR materials and in-venue touchpoints are part of the product, not an afterthought.",
      imageAlt: "Isometric illustration of a phone scanning a QR stand",
    },
    {
      id: "systems",
      title: "Existing systems matter",
      body: "Tummly is designed to add value alongside the tools and processes restaurants already rely on.",
      imageAlt: "Isometric illustration of systems connecting to a central hub",
    },
    {
      id: "growth",
      title: "Growth should stay manageable",
      body: "The Guest Loop works across one Location or many, without adding unnecessary complexity.",
      imageAlt: "Isometric illustration of stacked blocks with a growth arrow",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  items: readonly AboutRealityItem[]
}

/** Figma Our ambition section — bottom pad 100px desktop (`5673:24231`). */
export const ABOUT_AMBITION_Y = "pb-12.5 lg:pb-[100px]"

export type AboutAmbitionColumn = {
  id: "challenge" | "ambition"
  title: string
  body: string
}

/** Figma Our ambition (`5673:24231`). */
export const ABOUT_AMBITION = {
  eyebrow: "Our ambition",
  title: "Make stronger guest relationships practical for more restaurants.",
  columns: [
    {
      id: "challenge",
      title: "The challenge",
      body: "The tools used to understand guests and encourage repeat visits have traditionally been easier for larger businesses with dedicated teams, established digital channels and more complex systems.",
    },
    {
      id: "ambition",
      title: "Our ambition",
      body: "Tummly is built to make those capabilities practical for independent restaurants and growing groups, in a way that fits the pace and reality of everyday restaurant operations.",
    },
  ],
  closing:
    "Starting in the UK, we’re focused on building Tummly well for the restaurants we are here to serve.",
} as const satisfies {
  eyebrow: string
  title: string
  columns: readonly AboutAmbitionColumn[]
  closing: string
}

/** Figma About Sign up CTA (`5661:24424`) — editorial green marble variant. */
export const ABOUT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "Bring Tummly to your restaurant.",
  body: "Hear directly from guests, build your own guest list and give guests more reasons to return.",
  primaryLabel: "Get started",
}
