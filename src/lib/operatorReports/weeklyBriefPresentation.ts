export const WEEKLY_BRIEF_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbWeeklyBrief: "Weekly Brief",
  pageTitle: "Weekly Brief",
  pageSubtitle:
    "A plain-English summary of what happened, what changed and what to do next.",
  downloadPdf: "Download PDF",
  markAsReviewed: "Mark as reviewed",
  reviewedToast: "Weekly brief marked as reviewed.",
  pdfDownloadedToast: "Weekly brief PDF downloaded.",

  // Empty state copy
  emptyTitle: "No weekly brief yet",
  emptySubtitle:
    "Generate a weekly brief once you have guest feedback, QR activity, offers or campaign results.",
  generateBrief: "Generate brief",

  // Meta card labels
  periodLabel: "Period",
  locationLabel: "Location",
  dataSourcesLabel: "Data sources",
  confidenceLabel: "Confidence",
  generatedLabel: "Generated",

  // Section titles
  executiveSummaryTitle: "Executive summary",
  whatChangedTitle: "What changed",
  feedbackSummaryTitle: "Feedback summary",
  recommendedActionsTitle: "Recommended actions",
  suggestedCampaignTitle: "Suggested campaign",

  // Table headers
  areaHeader: "Area",
  changeHeader: "Change",
  meaningHeader: "Meaning",

  // Feedback summary
  reviewFollowUpQueue: "Review follow-up queue",

  // Suggested campaign
  reviewCampaign: "Review campaign",
} as const

/**
 * Format ready-envelope `generatedAtUtc` for the meta card (operator-local en-GB).
 * Example: "13 July, 08:30".
 */
export function formatWeeklyBriefGeneratedAt(generatedAtUtc: string): string {
  const date = new Date(generatedAtUtc)
  if (Number.isNaN(date.getTime())) {
    return generatedAtUtc
  }

  const datePart = new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
  }).format(date)

  const timePart = new Intl.DateTimeFormat("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date)

  return `${datePart}, ${timePart}`
}

/** Join phase-1 data-source labels for the meta card value. */
export function formatWeeklyBriefDataSources(dataSources: string[]): string {
  return dataSources.join(", ")
}

export type WeeklyBriefChangeRow = {
  id: string
  area: string
  change: string
  meaning: string
}

export type WeeklyBriefRecommendedAction = {
  id: string
  title: string
  subtitle: string
  cta: string
  target: "feedback" | "feedback-inbox" | "campaigns"
}

export type WeeklyBriefSuggestedCampaign = {
  status: string
  title: string
  subtitle: string
  cta: string
}

export type WeeklyBriefData = {
  period: string
  dataSources: string
  location: string
  confidence: string
  generated: string
  executiveSummary: string
  changes: WeeklyBriefChangeRow[]
  feedbackSummary: {
    text: string
    subtitle: string
  }
  recommendedActions: WeeklyBriefRecommendedAction[]
  suggestedCampaign: WeeklyBriefSuggestedCampaign
}

export const mockWeeklyBriefData: WeeklyBriefData = {
  period: "6–12 July",
  dataSources: "QR scans, feedback, guests, offers, campaigns",
  location: "All locations",
  confidence: "Based on enough activity to show useful patterns.",
  generated: "13 July, 08:30",
  executiveSummary:
    "You received more guest activity this week, mainly from delivery inserts and counter cards. Guests submitted 42 private feedback messages, with several comments mentioning delivery packaging and busy-period wait time. Your quiet-day offer produced the most redemptions, while one campaign caused more opt-outs than usual.",
  changes: [
    {
      id: "qr-scans",
      area: "QR scans",
      change: "+12%",
      meaning: "More guests are engaging with your QR placements",
    },
    {
      id: "feedback-received",
      area: "Feedback received",
      change: "+8%",
      meaning: "More guests are sharing private feedback",
    },
    {
      id: "contactable-guests",
      area: "Contactable guests",
      change: "+15%",
      meaning: "Your guest list is growing",
    },
    {
      id: "offer-redemptions",
      area: "Offer redemptions",
      change: "-4%",
      meaning: "Claimed offers may need clearer staff visibility",
    },
    {
      id: "unsubscribes",
      area: "Unsubscribes",
      change: "4 total",
      meaning: "Review message frequency and audience relevance",
    },
  ],
  feedbackSummary: {
    text: "Several guests mentioned delivery packaging and wait time during busy periods. Other comments praised friendly staff and food quality. Six feedback messages may need follow-up because the guest shared contact details and described a specific issue.",
    subtitle: "Based on private feedback submitted between 6–12 July.",
  },
  recommendedActions: [
    {
      id: "pkg-review",
      title: "Review delivery packaging before the weekend",
      subtitle:
        "Several feedback messages mentioned packaging. Check bag sealing, packaging stock and delivery handoff.",
      cta: "View related feedback",
      target: "feedback",
    },
    {
      id: "followup-guests",
      title: "Follow up with 6 guests",
      subtitle:
        "These guests shared contact details and may need a response.",
      cta: "Open follow-up queue",
      target: "feedback-inbox",
    },
    {
      id: "quiet-day-repeat",
      title: "Repeat your quiet-day offer with a tighter audience",
      subtitle:
        "The offer drove redemptions, but send it only to eligible guests who have not redeemed recently.",
      cta: "Review campaign draft",
      target: "campaigns",
    },
  ],
  suggestedCampaign: {
    status: "Draft",
    title: "Quiet-day boost",
    subtitle:
      "Eligible guests who opted in and have not redeemed an offer in the last 30 days.",
    cta: "Review campaign",
  },
}
