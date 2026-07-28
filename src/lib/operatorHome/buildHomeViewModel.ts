import type {
  FeedbackItem,
  HomeLatestActivityItem,
  LocationItem,
} from "@/types/dashboard"
import { HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  getOperatorFirstName,
  getOperatorInitials,
} from "@/lib/operatorHome/operatorProfile"
import { computeKpiTrendPercent } from "@/lib/operatorHome/performanceOverviewPresentation"
import type {
  OperatorHomeActivityItem,
  OperatorHomeActivityTabId,
  OperatorHomeChecklistAcks,
  OperatorHomeKpi,
  OperatorHomeSetupStep,
  OperatorHomeViewModel,
} from "@/types/operatorHome"

export interface BuildOperatorHomeViewModelInput {
  locations: LocationItem[]
  selectedLocationId: number
  /** Location feedback total from GET /api/feedback; null when not loaded. */
  feedback?: {
    total: number
    recent: FeedbackItem[]
  } | null
  /** Merged Latest activity from GET /api/home/latest-activity; null when not loaded. */
  latestActivity?: HomeLatestActivityItem[] | null
  /**
   * Feedback submitted count for the Home performance date range
   * (GET /api/home/performance); null when not loaded.
   */
  feedbackSubmitted?: number | null
  /** Equal-length previous window count; null when not loaded. */
  feedbackSubmittedPrevious?: number | null
  /** Guests joined count for the Home performance date range; null when not loaded. */
  guestsJoined?: number | null
  /** Equal-length previous window count; null when not loaded. */
  guestsJoinedPrevious?: number | null
  /**
   * QR / Smart Guest Link opens for the Home performance date range
   * (GET /api/home/performance); null when not loaded.
   */
  qrScans?: number | null
  /** Equal-length previous window count; null when not loaded. */
  qrScansPrevious?: number | null
  /** Label for the Performance overview date control. */
  dateRangeLabel?: string
  /** Per–Owned location Finish-setting-up acknowledgements; defaults to none. */
  checklistAcks?: OperatorHomeChecklistAcks | null
}

const ACTIVITY_EMPTY_COPY = "No activity yet"
const ACTIVITY_EMPTY_HELPER =
  "Feedback, guest sign-ups, offer activity and campaign events will appear here."

const GUEST_JOIN_SOURCE_LABEL = "From QR scan" as const

function buildGuestJoinedHeadline(guestName: string): string {
  return `${getOperatorFirstName(guestName)} joined your customer club`
}

function buildGuestConsentLabel(
  offersOptOut: boolean
): "Opted in" | "Opted out" {
  return offersOptOut ? "Opted out" : "Opted in"
}

function mapLatestActivityItems(
  items: HomeLatestActivityItem[]
): {
  feedbackItems: OperatorHomeActivityItem[]
  guestJoinedItems: OperatorHomeActivityItem[]
} {
  const feedbackItems: OperatorHomeActivityItem[] = []
  const guestJoinedItems: OperatorHomeActivityItem[] = []

  for (const item of items) {
    if (item.kind === "guest-joined") {
      guestJoinedItems.push({
        id: `guest-joined-${item.locationGuestId}`,
        kind: "guest-joined",
        locationGuestId: item.locationGuestId,
        guestName: item.guestName,
        initials: getOperatorInitials(item.guestName),
        headline: buildGuestJoinedHeadline(item.guestName),
        joinSourceLabel: GUEST_JOIN_SOURCE_LABEL,
        consentLabel: buildGuestConsentLabel(item.offersOptOut),
        createdAt: item.createdAt,
        canViewGuest: true,
        canSendOffer: false,
      })
      continue
    }

    feedbackItems.push({
      id: `feedback-${item.id}`,
      kind: "feedback",
      feedbackId: item.id,
      locationGuestId: item.locationGuestId,
      comment: item.comment,
      guestName: item.guestName,
      createdAt: item.createdAt,
      sentiment:
        item.classificationStatus === "Succeeded" ? item.sentiment : null,
      canViewFeedback: true,
      canViewGuest: item.locationGuestId != null,
    })
  }

  return { feedbackItems, guestJoinedItems }
}

function sortActivityItems(
  items: OperatorHomeActivityItem[]
): OperatorHomeActivityItem[] {
  return [...items].sort(
    (a, b) =>
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  )
}

function buildKpis(
  feedbackSubmitted: number | null,
  guestsJoined: number | null,
  feedbackSubmittedPrevious: number | null,
  guestsJoinedPrevious: number | null,
  qrScans: number | null,
  qrScansPrevious: number | null
): OperatorHomeKpi[] {
  const hasFeedback = feedbackSubmitted != null
  const hasGuestsJoined = guestsJoined != null
  const hasQrScans = qrScans != null
  const feedbackTrend =
    feedbackSubmitted != null && feedbackSubmittedPrevious != null
      ? computeKpiTrendPercent(feedbackSubmitted, feedbackSubmittedPrevious)
      : null
  const guestsJoinedTrend =
    guestsJoined != null && guestsJoinedPrevious != null
      ? computeKpiTrendPercent(guestsJoined, guestsJoinedPrevious)
      : null
  const qrScansTrend =
    qrScans != null && qrScansPrevious != null
      ? computeKpiTrendPercent(qrScans, qrScansPrevious)
      : null

  return [
    {
      id: "qr-scans",
      label: "QR scans",
      value: qrScans ?? 0,
      trendPercent: qrScansTrend,
      hasRealData: hasQrScans,
    },
    {
      id: "feedback",
      label: "Feedback submitted",
      value: feedbackSubmitted ?? 0,
      trendPercent: feedbackTrend,
      hasRealData: hasFeedback,
    },
    {
      id: "guests-joined",
      label: "Guests joined",
      value: guestsJoined ?? 0,
      trendPercent: guestsJoinedTrend,
      hasRealData: hasGuestsJoined,
    },
    {
      id: "offer-redemptions",
      label: "Offer redemptions",
      value: 0,
      trendPercent: null,
      hasRealData: false,
    },
  ]
}

function buildActivityByTab(
  feedbackItems: OperatorHomeActivityItem[],
  guestJoinedItems: OperatorHomeActivityItem[]
): Record<OperatorHomeActivityTabId, OperatorHomeActivityItem[]> {
  return {
    all: sortActivityItems([...feedbackItems, ...guestJoinedItems]),
    feedback: sortActivityItems(feedbackItems),
    guests: sortActivityItems(guestJoinedItems),
    offers: [],
    campaigns: [],
  }
}

function buildSetupSteps(input: {
  canPreviewGuestForm: boolean
  logoUploaded: boolean
  guestFormPreviewed: boolean
  qrPlacementGuideViewed: boolean
  feedbackTotal: number | null
}): OperatorHomeSetupStep[] {
  const hasFirstResponse = (input.feedbackTotal ?? 0) > 0
  const logoStatus = input.logoUploaded ? "complete" : "partial"
  const guestFormStatus = input.guestFormPreviewed ? "complete" : "partial"
  const firstResponseStatus = hasFirstResponse ? "complete" : "partial"
  const qrPlacementStatus = input.qrPlacementGuideViewed
    ? "complete"
    : "incomplete"

  const previewFormAction = {
    id: "preview-guest-form",
    label: "Preview form",
    available: input.canPreviewGuestForm,
  }

  return [
    {
      id: "account-ready",
      stepNumber: 1,
      title: "Account ready",
      description: "Your account is ready and your trial has started.",
      status: "complete",
      actions: [],
    },
    {
      id: "upload-logo",
      stepNumber: 2,
      title: "Upload restaurant logo",
      description:
        "Add your logo so guests recognise your restaurant when they open the feedback form.",
      status: logoStatus,
      actions: [{ id: "upload-logo", label: "Upload logo", available: false }],
    },
    {
      id: "guest-form",
      stepNumber: 3,
      title: "Review your guest feedback form",
      description:
        "Check the default questions, contact fields, consent wording and thank-you screen before sharing it with guests.",
      status: guestFormStatus,
      actions: [previewFormAction],
    },
    {
      id: "first-response",
      stepNumber: 4,
      title: "Receive your first guest response",
      description:
        "Waiting for the first valid guest submission from your QR code or Smart Guest Link.",
      status: firstResponseStatus,
      actions: [previewFormAction],
    },
    {
      id: "qr-placement",
      stepNumber: 5,
      title: "Place your QR materials",
      description:
        "Place your QR code where guests are likely to see it, such as at the counter, on receipts, in delivery bags or on packaging.",
      status: qrPlacementStatus,
      actions: [
        {
          id: "view-placement-guide",
          label: "View placement guide",
          available: false,
        },
        {
          id: "order-qr-materials",
          label: "Order QR materials",
          available: false,
        },
      ],
    },
    {
      id: "first-offer",
      stepNumber: 6,
      title: "Create your first offer",
      description:
        "Create a simple return-visit offer with an expiry date and redemption controls.",
      status: "incomplete",
      actions: [
        { id: "create-offer", label: "Create offer", available: false },
      ],
    },
    {
      id: "first-campaign",
      stepNumber: 7,
      title: "Send your first campaign",
      description:
        "Available when at least one guest has valid marketing consent and a reachable email address or phone number.",
      status: "incomplete",
      actions: [
        {
          id: "create-campaign",
          label: "Create campaign",
          available: false,
        },
      ],
    },
  ]
}

/** Assemble the Operator Home body view-model for the selected Owned location. */
export function buildOperatorHomeViewModel(
  input: BuildOperatorHomeViewModelInput
): OperatorHomeViewModel | null {
  const selected =
    input.locations.find((location) => location.id === input.selectedLocationId) ??
    input.locations[0]

  if (!selected) {
    return null
  }

  const smartGuestLink = selected.guestUrl.trim() || null
  const canPreviewGuestForm = smartGuestLink != null
  const canCopySmartGuestLink = smartGuestLink != null
  const { feedbackItems, guestJoinedItems } = mapLatestActivityItems(
    input.latestActivity ?? []
  )
  const feedbackTotal =
    input.feedback != null ? input.feedback.total : null
  const feedbackSubmitted =
    input.feedbackSubmitted !== undefined ? input.feedbackSubmitted : null
  const feedbackSubmittedPrevious =
    input.feedbackSubmittedPrevious !== undefined
      ? input.feedbackSubmittedPrevious
      : null
  const guestsJoined =
    input.guestsJoined !== undefined ? input.guestsJoined : null
  const guestsJoinedPrevious =
    input.guestsJoinedPrevious !== undefined
      ? input.guestsJoinedPrevious
      : null
  const qrScans = input.qrScans !== undefined ? input.qrScans : null
  const qrScansPrevious =
    input.qrScansPrevious !== undefined ? input.qrScansPrevious : null
  const checklistAcks = input.checklistAcks ?? {
    guestFormPreviewed: false,
    qrPlacementGuideViewed: false,
    logoUploaded: false,
  }

  return {
    selectedLocationId: selected.id,
    selectedLocationName: selected.locationName,
    smartGuestLink,
    canCopySmartGuestLink,
    canPreviewGuestForm,
    dateRangeLabel: input.dateRangeLabel ?? HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL,
    setupSteps: buildSetupSteps({
      canPreviewGuestForm,
      logoUploaded: checklistAcks.logoUploaded,
      guestFormPreviewed: checklistAcks.guestFormPreviewed,
      qrPlacementGuideViewed: checklistAcks.qrPlacementGuideViewed,
      feedbackTotal,
    }),
    kpis: buildKpis(
      feedbackSubmitted,
      guestsJoined,
      feedbackSubmittedPrevious,
      guestsJoinedPrevious,
      qrScans,
      qrScansPrevious
    ),
    activityByTab: buildActivityByTab(feedbackItems, guestJoinedItems),
    activityEmpty: {
      emptyCopy: ACTIVITY_EMPTY_COPY,
      emptyHelper: ACTIVITY_EMPTY_HELPER,
    },
  }
}

export function resolveInitialLocationId(
  locations: LocationItem[],
  preferredId: number | null | undefined
): number | null {
  if (locations.length === 0) {
    return null
  }

  if (
    preferredId != null &&
    locations.some((location) => location.id === preferredId)
  ) {
    return preferredId
  }

  const sorted = [...locations].sort(
    (a, b) =>
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
  )

  return sorted[0]?.id ?? null
}
