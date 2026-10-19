import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"

/** Shared section inset — matches Product / QR materials (20px mobile, 60px desktop). */
export const ABOUT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Figma About hero `5673:24144` — top pad 70px desktop; band sits flush under CTAs. */
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
  title: "Helping restaurants build stronger relationships with their guests.",
  body: "We created Tummly to help independent restaurants and growing groups understand their guests, stay connected beyond the visit and encourage them to return. By bringing together guest Feedback, Offers, Campaigns and performance insights, we make guest engagement easier to manage.",
  primaryLabel: "Get started",
  secondaryLabel: "See how it works",
  imageAlt:
    "Guests toasting over a meal, framed by a green hospitality texture on the About Tummly page.",
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

/** Figma About Industries (`5685:35427`) — card order: cafés → takeaway → casual → groups. */
export const ABOUT_INDUSTRIES = {
  eyebrow: "Who we're building for",
  title: "Independent restaurants. Growing groups. Real operating pressures.",
  paragraphs: [
    "Tummly is built for restaurants that want to understand their guests, improve experiences and create more opportunities for repeat visits.",
    "From independent restaurants to growing groups, Tummly connects guest Feedback, engagement and useful insights with everyday restaurant operations.",
  ],
  items: [
    {
      id: "cafes",
      title: "Cafés & bakeries",
      body: "Make it easy for guests to share their experience after a coffee, quick bite or visit to the counter.",
      imageAlt:
        "Barista handing a latte and croissant to a guest at a café counter with a Tummly feedback stand.",
    },
    {
      id: "takeaway",
      title: "Takeaway & QSR",
      body: "Hear from guests across collection, takeaway and delivery experiences.",
      imageAlt:
        "Green How was your visit QR stand on a takeaway counter with kitchen and menu boards behind.",
    },
    {
      id: "casual",
      title: "Casual dining",
      body: "Understand the dining experience and identify opportunities to improve service.",
      imageAlt:
        "Friends smiling at a casual dining booth with a Tummly QR table tent on the table.",
    },
    {
      id: "groups",
      title: "Growing restaurant groups",
      body: "Understand your guests, strengthen relationships and encourage return visits across all your restaurants.",
      imageAlt:
        "Busy restaurant dining room with guests at wooden tables under woven pendant lights.",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  paragraphs: readonly string[]
  items: readonly AboutIndustry[]
}

/** Figma Principles section pad — 100px top desktop (`5673:24211`). */
export const ABOUT_PRINCIPLES_Y = "pt-12.5 lg:pt-[100px]"

/** Principles section title — *headline* Jakarta 46 / 58 medium, #0e0e0e. */
export const ABOUT_PRINCIPLES_HEADING =
  "font-jakarta text-[34px] font-medium leading-none tracking-normal text-[#0e0e0e] lg:text-[46px] lg:leading-[58px]"

/** Principles intro — *functional* Helvetica Neue 16 / 22, #141414. */
export const ABOUT_PRINCIPLES_INTRO =
  "font-sans text-base font-normal leading-[22px] tracking-normal text-[#141414]"

export type AboutPrinciple = {
  id: "heard" | "listening" | "trust" | "return" | "location"
  title: string
  body: string
  imageAlt: string
}

/** Figma The principles behind Tummly (`5673:24211`). */
export const ABOUT_PRINCIPLES = {
  eyebrow: "What we believe",
  title: "The principles behind Tummly.",
  intro: "The ideas that shape how we build for restaurants and their guests.",
  items: [
    {
      id: "heard",
      title: "Every guest deserves to be heard",
      body: "We make it easier for restaurants to hear directly from their guests.",
      imageAlt: "Illustration of a QR table tent with a heart speech bubble",
    },
    {
      id: "listening",
      title: "Listening leads to improvement",
      body: "Guest Feedback helps restaurants understand experiences and improve their service.",
      imageAlt: "Illustration of a checklist with an upward arrow",
    },
    {
      id: "trust",
      title: "Guest trust comes first",
      body: "We respect guest choice and keep marketing permissions clear and optional.",
      imageAlt: "Illustration of a shield with a heart flanked by guest icons",
    },
    {
      id: "return",
      title: "Give guests a reason to return",
      body: "Relevant Offers and Campaigns help restaurants encourage repeat visits and orders.",
      imageAlt: "Illustration of a gift box with a return arrow",
    },
    {
      id: "location",
      title: "Every Location matters",
      body: "We support individual restaurants and growing groups with a clearer view of performance.",
      imageAlt: "Illustration of restaurant storefronts with location pins",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  intro: string
  items: readonly AboutPrinciple[]
}

/** Figma Restaurant reality section pad — 90px desktop (`5673:24194`). */
export const ABOUT_REALITY_Y = "py-12.5 lg:py-[90px]"

export type AboutRealityItem = {
  id: "return" | "relationships" | "together" | "visibility"
  title: string
  body: string
  imageAlt: string
}

/** Figma Restaurant reality as a design philosophy (`5673:24194`). */
export const ABOUT_REALITY = {
  eyebrow: "We build around restaurant reality",
  title: "Built for the way restaurants connect with guests.",
  intro: "The ideas that shape how we build for restaurants and their guests.",
  items: [
    {
      id: "return",
      title: "Turn everyday visits and orders into reasons to return",
      body: "Connect with guests through simple QR experiences, build stronger relationships and encourage repeat visits and orders.",
      imageAlt: "Illustration of a suggestion box with a green textured side",
    },
    {
      id: "relationships",
      title: "Build relationships beyond the visit",
      body: "Stay connected with guests through relevant Offers and permission-based Campaigns that give them reasons to return.",
      imageAlt:
        "Illustration of a storefront connected to a guest profile by a dashed arc",
    },
    {
      id: "together",
      title: "Keep everything working together",
      body: "Manage your guest list, Offers and Campaigns in one place, with AI helping you understand performance and make informed decisions.",
      imageAlt: "Illustration of a hub plugging into a connected system",
    },
    {
      id: "visibility",
      title: "Grow with greater visibility",
      body: "Understand guest engagement and performance across your restaurants, with the right access and information for each team.",
      imageAlt: "Illustration of stepped blocks with a green flag on top",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  intro: string
  items: readonly AboutRealityItem[]
}

/** Figma Our ambition section — bottom pad 100px desktop (`5673:24231`). */
export const ABOUT_AMBITION_Y = "pb-12.5 lg:pb-[100px]"

export type AboutAmbitionColumn = {
  id: "opportunity" | "ambition"
  title: string
  body: string
}

/** Figma Our ambition (`5673:24231`). */
export const ABOUT_AMBITION = {
  eyebrow: "Our ambition",
  title: "Making every guest relationship count.",
  columns: [
    {
      id: "opportunity",
      title: "The opportunity",
      body: "Every restaurant serves guests who may return, recommend the experience or become regular customers. Each interaction creates an opportunity to understand those guests better, build trust and encourage future visits.",
    },
    {
      id: "ambition",
      title: "Our ambition",
      body: "We are building Tummly to help restaurants make more of those opportunities. By connecting guest engagement, Offers, Campaigns and meaningful insights in one platform, we aim to make lasting guest relationships easier to build and manage.",
    },
  ],
  closing: "Starting in the UK, with a vision for restaurants of every size.",
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
