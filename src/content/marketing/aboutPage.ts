import type { MarketingSignUpCtaContent } from "@/components/marketing/MarketingSignUpCta"

/** Shared section inset — matches Product / QR materials (20px mobile, 60px desktop). */
export const ABOUT_SECTION_INSET = "px-5 lg:px-[60px]"

/** Figma About hero `5673:24144` — top pad 70px desktop; band sits flush under CTAs. */
export const ABOUT_HERO_Y = "pt-12.5 lg:pt-[70px]"

/** Figma About hero eyebrow — *headline* Jakarta 14 / 16, #4a4a4a. */
export const ABOUT_HERO_EYEBROW =
  "font-jakarta text-sm font-medium leading-4 tracking-normal text-[#4a4a4a] uppercase"

/** Figma About hero title — *headline* Jakarta 54 / 60 semibold, #141414. */
export const ABOUT_HERO_HEADING =
  "font-jakarta text-[36px] font-semibold leading-none tracking-normal text-[#141414] lg:text-[54px] lg:leading-[60px]"

/** Figma About hero body — *functional* Helvetica Neue 16 / 22, #141414. */
export const ABOUT_HERO_BODY =
  "font-sans text-base font-normal leading-[22px] tracking-normal text-[#141414]"

/** Figma About Tummly hero (`5673:24144`). */
export const ABOUT_HERO = {
  eyebrow: "About Tummly",
  title: "Built for the work that follows a customer’s visit.",
  body: "Running a restaurant means thinking about the next service while learning from the last. Tummly helps you hear what customers experienced, respond with care and create relevant reasons to return.",
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

export type AboutIndustry = {
  id: "takeaway" | "cafes" | "casual" | "groups"
  title: string
  body: string
  imageAlt: string
}

/** Figma About Industries (`5685:35427`) — card order: cafés → takeaways → casual → groups. */
export const ABOUT_INDUSTRIES = {
  eyebrow: "Who we're building for",
  title: "For the people making the next service happen.",
  items: [
    {
      id: "cafes",
      title: "Cafés & bakeries",
      body: "Know what made someone’s morning and what might bring them back another day.",
      imageAlt:
        "Barista handing a latte and croissant to a guest at a café counter with a Tummly feedback stand.",
    },
    {
      id: "takeaway",
      title: "Takeaways & QSRs",
      body: "A late collection. A great meal at home. Hear how the order went and keep the conversation open.",
      imageAlt:
        "Green How was your visit QR stand on a takeaway counter with kitchen and menu boards behind.",
    },
    {
      id: "casual",
      title: "Casual dining",
      body: "Hear about the evening, respond to a concern and recognise what the team got right.",
      imageAlt:
        "Friends smiling at a casual dining booth with a Tummly QR table tent on the table.",
    },
    {
      id: "groups",
      title: "Growing restaurant groups",
      body: "See what customers are saying across your restaurants without losing the local detail.",
      imageAlt:
        "Busy restaurant dining room with guests at wooden tables under woven pendant lights.",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
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
      body: "Start with the person’s own words, shared privately with the restaurant.",
      imageAlt: "Illustration of a QR table tent with a heart speech bubble",
    },
    {
      id: "listening",
      title: "Listening leads to improvement",
      body: "Good feedback gives a team something specific to recognise, discuss or change.",
      imageAlt: "Illustration of a checklist with an upward arrow",
    },
    {
      id: "trust",
      title: "Guest trust comes first",
      body: "Be clear about feedback, follow-up and the separate choice to receive marketing.",
      imageAlt: "Illustration of a shield with a heart flanked by guest icons",
    },
    {
      id: "return",
      title: "Give guests a reason to return",
      body: "A thoughtful invitation can give a customer a reason to choose your restaurant again.",
      imageAlt: "Illustration of a gift box with a return arrow",
    },
    {
      id: "location",
      title: "Every Location matters",
      body: "Each restaurant has its own team, customers and service challenges. The group view should keep that detail.",
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
  id: "visit" | "attention" | "invitation" | "review"
  title: string
  body: string
  imageAlt: string
}

/** Figma Restaurant reality as a design philosophy (`5673:24194`). */
export const ABOUT_REALITY = {
  eyebrow: "We build around restaurant reality",
  title: "Designed around the moments that matter.",
  intro: "The ideas that shape how we build for restaurants and their guests.",
  items: [
    {
      id: "visit",
      title: "After an order or visit",
      body: "Give customers a clear way to tell the restaurant how it went.",
      imageAlt: "Illustration of a suggestion box with a star-rating speech bubble",
    },
    {
      id: "attention",
      title: "When something needs attention",
      body: "Help the team understand the experience and prepare a considered response.",
      imageAlt:
        "Illustration of a speech bubble connected by a dotted arc to a chat badge",
    },
    {
      id: "invitation",
      title: "Before the next invitation",
      body: "Prepare a relevant message for people who have chosen to receive it.",
      imageAlt:
        "Illustration of an envelope connected by a dotted arc to a green check badge",
    },
    {
      id: "review",
      title: "At the management review",
      body: "Bring customer comments and promotional response into the conversation.",
      imageAlt: "Illustration of stepped blocks with a green flag on top",
    },
  ],
} as const satisfies {
  eyebrow: string
  title: string
  intro: string
  items: readonly AboutRealityItem[]
}

/** Figma Our mission section — bottom pad 100px desktop (`7023:13862`). */
export const ABOUT_MISSION_Y = "pb-12.5 lg:pb-[100px]"

/** Figma Our mission (`7023:13862`). */
export const ABOUT_MISSION = {
  eyebrow: "Our mission",
  title: "Make listening part of running a better restaurant.",
  ambitionTitle: "Our ambition",
  ambitionBody:
    "To transform how restaurants grow by making direct, trusted customer relationships a defining strength of the hospitality industry. We envision a future where every restaurant, regardless of size, can turn everyday orders and visits into lasting connections that extend far beyond a single transaction.",
} as const

export type AboutCompanyCard = {
  id: "company" | "office" | "enquiries"
  title: string
  body: string
}

/** Figma The company behind Tummly (`5673:24231`). */
export const ABOUT_COMPANY = {
  title: "The company behind Tummly",
  cards: [
    {
      id: "company",
      title: "Company",
      body: "Tummly.com is the trading name of TUMMLY.COM LIMITED, registered in England and Wales under company number 16236040.",
    },
    {
      id: "office",
      title: "Registered office",
      body: "Registered office: 71–75 Shelton Street, Covent Garden, London, WC2H 9JQ.",
    },
    {
      id: "enquiries",
      title: "Company and product enquiries",
      body: "For company and product enquiries, contact Tummly. Questions about food, service, orders, bookings or restaurant Offer terms belong with the restaurant.",
    },
  ],
  contactLabel: "Contact Tummly",
} as const satisfies {
  title: string
  cards: readonly AboutCompanyCard[]
  contactLabel: string
}

/** Figma About Sign up CTA (`5661:24423`) — editorial blue-green marble, "read" legal note. */
export const ABOUT_SIGN_UP_CTA: MarketingSignUpCtaContent = {
  title: "See what that looks like in practice.",
  body: "Explore the Guest Loop and decide how it could fit the restaurant you run.",
  primaryLabel: "Get started",
  legalNote: "read",
}
