/**
 * Gate-specific wait copy for the Assistant turn spinner.
 * Phrases describe real pipeline work (match / load / draft / save), not
 * decorative gerunds. Client classify is best-effort for early UX; the
 * server still owns the real task.
 */

export type AssistantWaitGate =
  | "create-campaign-draft"
  | "create-campaign-with-offer"
  | "offer-path"
  | "recovery-path"
  | "retrieve"
  | "refuse"

export type AssistantWaitStep = "checking" | "retrieving" | "preparing"

export type AssistantWaitRetrieveFocus =
  | "campaigns"
  | "offers"
  | "feedback"
  | "guests"
  | "capture"
  | "attention"
  | "performance"
  | "generic"

export type AssistantWaitPhrasePlan = {
  gate: AssistantWaitGate
  retrieveFocus: AssistantWaitRetrieveFocus
}

export const ASSISTANT_WAIT_PHRASE_INTERVAL_MS = 1250

const CREATE_CAMPAIGN_CHECKING = [
  "Matching the campaign location…",
  "Matching audience and channel…",
  "Matching an attachable offer…",
  "Choosing a campaign goal…",
  "Naming the campaign…",
  "Checking Email vs SMS…",
  "Filling campaign draft fields…",
  "Matching a campaign template…",
] as const

const CREATE_CAMPAIGN_RETRIEVING = [
  "Loading guest audience sizes…",
  "Loading attachable offers…",
  "Loading campaign templates…",
  "Checking eligible guest counts…",
  "Loading venue campaign options…",
] as const

const CREATE_CAMPAIGN_PREPARING = [
  "Drafting the campaign message…",
  "Writing Email or SMS copy…",
  "Saving the campaign draft…",
  "Attaching offer and audience…",
  "Finalising the campaign draft…",
] as const

const CREATE_CAMPAIGN_WITH_OFFER_CHECKING = [
  "Matching campaign and offer together…",
  "Matching audience and channel…",
  "Matching offer type and value…",
  "Filling campaign draft fields…",
  "Filling offer draft fields…",
  "Choosing a campaign goal…",
  "Naming the campaign…",
  "Checking offer attach rules…",
] as const

const CREATE_CAMPAIGN_WITH_OFFER_RETRIEVING = [
  "Loading guest audience sizes…",
  "Loading offer catalog rules…",
  "Loading attachable offers…",
  "Loading campaign templates…",
  "Checking eligible guest counts…",
] as const

const CREATE_CAMPAIGN_WITH_OFFER_PREPARING = [
  "Building the attached offer…",
  "Drafting the campaign message…",
  "Saving the offer draft…",
  "Saving the campaign draft…",
  "Linking offer to campaign…",
  "Finalising campaign with offer…",
] as const

const OFFER_PATH_CHECKING = [
  "Matching offer type and value…",
  "Matching discount or free item…",
  "Filling offer dates…",
  "Matching offer placement…",
  "Checking catalog offer rules…",
  "Naming the offer…",
  "Matching the offer location…",
  "Filling offer draft fields…",
] as const

const OFFER_PATH_RETRIEVING = [
  "Loading offer catalog rules…",
  "Loading venue offer settings…",
  "Checking placement options…",
  "Loading existing offers…",
] as const

const OFFER_PATH_PREPARING = [
  "Building the offer draft…",
  "Saving the offer draft…",
  "Writing offer title and terms…",
  "Finalising the offer draft…",
] as const

const RECOVERY_CHECKING = [
  "Finding the feedback…",
  "Matching recovery intent…",
  "Checking recovery eligibility…",
  "Matching a recovery offer…",
  "Reading the guest reply…",
  "Matching follow-up options…",
] as const

const RECOVERY_RETRIEVING = [
  "Loading guest and offer details…",
  "Loading feedback history…",
  "Loading recovery-ready offers…",
  "Loading venue recovery options…",
] as const

const RECOVERY_PREPARING = [
  "Building the recovery reply…",
  "Drafting the follow-up message…",
  "Saving recovery work…",
  "Finalising the recovery plan…",
] as const

const REFUSE_CHECKING = [
  "Checking what I can help with…",
  "Matching your ask to Assistant limits…",
  "Checking the allow-list…",
] as const

const REFUSE_RETRIEVING = [
  "Skipping venue data for this ask…",
] as const

const REFUSE_PREPARING = [
  "Drafting a reply…",
  "Writing a clear limit message…",
] as const

const RETRIEVE_CHECKING_GENERIC = [
  "Matching your question to venue data…",
  "Choosing which packs to load…",
  "Matching the reporting period…",
  "Matching the owned location…",
  "Checking compare vs single venue…",
] as const

const RETRIEVE_RETRIEVING_GENERIC = [
  "Loading campaigns, feedback, and offers…",
  "Loading venue performance packs…",
  "Loading guest and capture stats…",
  "Gathering facts for this period…",
] as const

const RETRIEVE_PREPARING_GENERIC = [
  "Writing the summary…",
  "Grounding the answer in venue data…",
  "Drafting the reply…",
  "Checking named facts…",
] as const

const RETRIEVE_BY_FOCUS: Record<
  AssistantWaitRetrieveFocus,
  {
    checking: readonly string[]
    retrieving: readonly string[]
    preparing: readonly string[]
  }
> = {
  campaigns: {
    checking: [
      "Matching your campaign question…",
      "Choosing campaign metrics to load…",
      "Matching active vs all campaigns…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading campaign results…",
      "Loading send and open stats…",
      "Loading campaign audiences…",
      "Loading campaign offer attaches…",
    ],
    preparing: [
      "Summarising campaign performance…",
      "Writing the campaign summary…",
      "Grounding campaign facts…",
    ],
  },
  offers: {
    checking: [
      "Matching your offer question…",
      "Choosing claim and redemption packs…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading offer claims and redemptions…",
      "Loading offer catalog activity…",
      "Loading venue offer stats…",
    ],
    preparing: [
      "Summarising offer performance…",
      "Writing the offer summary…",
      "Grounding offer facts…",
    ],
  },
  feedback: {
    checking: [
      "Matching your feedback question…",
      "Choosing feedback score packs…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading feedback scores…",
      "Loading recent guest comments…",
      "Loading feedback themes…",
    ],
    preparing: [
      "Summarising feedback…",
      "Writing the feedback summary…",
      "Grounding feedback facts…",
    ],
  },
  guests: {
    checking: [
      "Matching your guest question…",
      "Choosing guest activity packs…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading guest activity…",
      "Loading new and returning guests…",
      "Loading guest contact eligibility…",
    ],
    preparing: [
      "Summarising guest activity…",
      "Writing the guest summary…",
      "Grounding guest facts…",
    ],
  },
  capture: {
    checking: [
      "Matching your capture question…",
      "Choosing QR and capture packs…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading QR and capture stats…",
      "Loading scan and form activity…",
      "Loading capture placement data…",
    ],
    preparing: [
      "Summarising capture activity…",
      "Writing the capture summary…",
      "Grounding capture facts…",
    ],
  },
  attention: {
    checking: [
      "Matching items that need attention…",
      "Choosing attention surfaces…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading items that need attention…",
      "Loading recommended next steps…",
      "Loading weekly brief signals…",
    ],
    preparing: [
      "Listing what needs attention…",
      "Writing the attention summary…",
      "Grounding attention facts…",
    ],
  },
  performance: {
    checking: [
      "Matching your performance question…",
      "Choosing mixed venue packs…",
      "Matching the reporting period…",
    ],
    retrieving: [
      "Loading venue performance packs…",
      "Loading cross-area metrics…",
      "Gathering facts for this period…",
    ],
    preparing: [
      "Writing the performance summary…",
      "Grounding performance facts…",
      "Drafting the reply…",
    ],
  },
  generic: {
    checking: RETRIEVE_CHECKING_GENERIC,
    retrieving: RETRIEVE_RETRIEVING_GENERIC,
    preparing: RETRIEVE_PREPARING_GENERIC,
  },
}

const PHRASES_BY_GATE: Record<
  Exclude<AssistantWaitGate, "retrieve">,
  {
    checking: readonly string[]
    retrieving: readonly string[]
    preparing: readonly string[]
  }
> = {
  "create-campaign-draft": {
    checking: CREATE_CAMPAIGN_CHECKING,
    retrieving: CREATE_CAMPAIGN_RETRIEVING,
    preparing: CREATE_CAMPAIGN_PREPARING,
  },
  "create-campaign-with-offer": {
    checking: CREATE_CAMPAIGN_WITH_OFFER_CHECKING,
    retrieving: CREATE_CAMPAIGN_WITH_OFFER_RETRIEVING,
    preparing: CREATE_CAMPAIGN_WITH_OFFER_PREPARING,
  },
  "offer-path": {
    checking: OFFER_PATH_CHECKING,
    retrieving: OFFER_PATH_RETRIEVING,
    preparing: OFFER_PATH_PREPARING,
  },
  "recovery-path": {
    checking: RECOVERY_CHECKING,
    retrieving: RECOVERY_RETRIEVING,
    preparing: RECOVERY_PREPARING,
  },
  refuse: {
    checking: REFUSE_CHECKING,
    retrieving: REFUSE_RETRIEVING,
    preparing: REFUSE_PREPARING,
  },
}

function containsAny(lower: string, needles: readonly string[]): boolean {
  return needles.some((needle) => lower.includes(needle))
}

function namesCampaignNoun(lower: string): boolean {
  return containsAny(lower, [
    "campaign",
    "campagin",
    "campaing",
    "email blast",
    "sms blast",
  ])
}

function looksLikeOfferRetrieveOnly(lower: string): boolean {
  return containsAny(lower, [
    "how many offers",
    "offer claims",
    "offer redemptions",
    "redeemed offers",
    "summarise offers",
    "summarize offers",
    "offer performance",
  ])
}

function looksLikeCampaignRetrieveOnly(lower: string): boolean {
  return containsAny(lower, [
    "how many campaigns",
    "campaign performance",
    "active campaigns",
    "summarise campaigns",
    "summarize campaigns",
    "campaign results",
  ])
}

function looksLikeOfferPath(lower: string): boolean {
  if (looksLikeOfferRetrieveOnly(lower)) {
    return false
  }
  return containsAny(lower, [
    "create an offer",
    "create a new offer",
    "create offer",
    "draft an offer",
    "draft a offer",
    "offer draft",
    "offers catalog draft",
    "make an offer",
    "new offer for",
    "build an offer",
  ])
}

function looksLikeRecoveryPath(lower: string): boolean {
  return containsAny(lower, [
    "recover",
    "recovery",
    "follow up on feedback",
    "follow-up on feedback",
    "unhappy guest",
    "negative feedback",
    "make it right",
  ])
}

function looksLikeCreateCampaignDraft(lower: string): boolean {
  if (looksLikeCampaignRetrieveOnly(lower) || !namesCampaignNoun(lower)) {
    return false
  }
  return containsAny(lower, [
    "draft an",
    "draft a ",
    "create a campaign",
    "create campaign",
    "can you create a campaign",
    "start a campaign",
    "help me create a campaign",
    "create an email",
    "create an sms",
    "prepare a campaign",
    "make a campaign",
    "make a draft campaign",
    "write a campaign",
  ])
}

function looksLikeCreateCampaignWithOffer(lower: string): boolean {
  if (looksLikeRecoveryPath(lower)) {
    return false
  }
  if (
    containsAny(lower, [
      "with an offer",
      "with offer",
      "plus an offer",
      "and an offer",
      "attach an offer",
      "include an offer",
      "campaign with offer",
    ])
  ) {
    return looksLikeCreateCampaignDraft(lower) || namesCampaignNoun(lower)
  }
  if (!looksLikeCreateCampaignDraft(lower)) {
    return false
  }
  return (
    looksLikeOfferPath(lower)
    || containsAny(lower, [
      "% off",
      " percent off",
      "free ",
      "discount",
      "£",
      "$",
    ])
  )
}

function looksLikeRefuse(lower: string): boolean {
  return containsAny(lower, [
    "help centre",
    "help center",
    "who are you",
    "what can you do",
    "tell me a joke",
    "write a poem",
  ])
}

export function classifyAssistantWaitRetrieveFocus(
  message: string
): AssistantWaitRetrieveFocus {
  const lower = message.trim().toLowerCase()
  if (
    containsAny(lower, [
      "need attention",
      "needs attention",
      "recommended next",
      "weekly brief",
      "what should i",
    ])
  ) {
    return "attention"
  }
  if (
    containsAny(lower, ["qr", "capture", "scan", "placement guide"])
  ) {
    return "capture"
  }
  if (
    containsAny(lower, [
      "guest",
      "guests",
      "dormant",
      "new guests",
      "returning",
    ])
  ) {
    return "guests"
  }
  if (
    containsAny(lower, [
      "feedback",
      "nps",
      "rating",
      "review",
      "comment",
      "score",
    ])
  ) {
    return "feedback"
  }
  if (
    containsAny(lower, [
      "redeem",
      "redemption",
      "claim",
      "offers",
      "offer ",
    ])
    && !looksLikeOfferPath(lower)
  ) {
    return "offers"
  }
  if (
    containsAny(lower, ["campaign", "email open", "sms sent"])
    && !looksLikeCreateCampaignDraft(lower)
  ) {
    return "campaigns"
  }
  if (
    containsAny(lower, [
      "performance",
      "how are we doing",
      "overview",
      "summarise",
      "summarize",
    ])
  ) {
    return "performance"
  }
  return "generic"
}

export function classifyAssistantWaitGate(message: string): AssistantWaitGate {
  const lower = message.trim().toLowerCase()
  if (looksLikeCreateCampaignWithOffer(lower)) {
    return "create-campaign-with-offer"
  }
  if (looksLikeCreateCampaignDraft(lower)) {
    return "create-campaign-draft"
  }
  if (looksLikeRecoveryPath(lower)) {
    return "recovery-path"
  }
  if (looksLikeOfferPath(lower)) {
    return "offer-path"
  }
  if (looksLikeRefuse(lower)) {
    return "refuse"
  }
  return "retrieve"
}

export function planAssistantWaitPhrases(
  message: string
): AssistantWaitPhrasePlan {
  const gate = classifyAssistantWaitGate(message)
  return {
    gate,
    retrieveFocus:
      gate === "retrieve"
        ? classifyAssistantWaitRetrieveFocus(message)
        : "generic",
  }
}

export function assistantWaitPhrasesFor(
  plan: AssistantWaitPhrasePlan,
  step: AssistantWaitStep
): readonly string[] {
  if (plan.gate === "retrieve") {
    return RETRIEVE_BY_FOCUS[plan.retrieveFocus][step]
  }
  return PHRASES_BY_GATE[plan.gate][step]
}

export function assistantWaitPhraseAt(
  plan: AssistantWaitPhrasePlan,
  step: AssistantWaitStep,
  index: number
): string {
  const phrases = assistantWaitPhrasesFor(plan, step)
  const length = phrases.length
  if (length === 0) {
    return ASSISTANT_WAIT_BODY
  }
  const normalised = ((index % length) + length) % length
  return phrases[normalised] ?? ASSISTANT_WAIT_BODY
}

const ALL_WAIT_PHRASES: readonly string[] = [
  ...Object.values(PHRASES_BY_GATE).flatMap((entry) => [
    ...entry.checking,
    ...entry.retrieving,
    ...entry.preparing,
  ]),
  ...Object.values(RETRIEVE_BY_FOCUS).flatMap((entry) => [
    ...entry.checking,
    ...entry.retrieving,
    ...entry.preparing,
  ]),
]

export function isAssistantWaitPhraseBody(body: string): boolean {
  return ALL_WAIT_PHRASES.includes(body)
}

/** True while the client still rotates the early (checking) phrase set. */
export function isAssistantCheckingWaitBody(body: string): boolean {
  const checking = [
    ...Object.values(PHRASES_BY_GATE).flatMap((entry) => [...entry.checking]),
    ...Object.values(RETRIEVE_BY_FOCUS).flatMap((entry) => [
      ...entry.checking,
    ]),
  ]
  return checking.includes(body)
}

export const ASSISTANT_WAIT_BODY = assistantWaitPhraseAt(
  { gate: "retrieve", retrieveFocus: "generic" },
  "checking",
  0
)

export const ASSISTANT_WAIT_RETRIEVING_BODY = assistantWaitPhraseAt(
  { gate: "retrieve", retrieveFocus: "generic" },
  "retrieving",
  0
)

export const ASSISTANT_WAIT_PREPARING_BODY = assistantWaitPhraseAt(
  { gate: "retrieve", retrieveFocus: "generic" },
  "preparing",
  0
)
