import type { FeedbackItem, LocationItem } from "@/types/dashboard"
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
  /** Location feedback total + recent from GET /api/feedback; null when not loaded. */
  feedback?: {
    total: number
    recent: FeedbackItem[]
  } | null
  /** Per–Owned location Finish-setting-up acknowledgements; defaults to none. */
  checklistAcks?: OperatorHomeChecklistAcks | null
}

const ACTIVITY_EMPTY_COPY = "No activity yet"
const ACTIVITY_EMPTY_HELPER =
  "Feedback, guest sign-ups, offer activity and campaign events will appear here."

function mapFeedbackActivity(
  recent: FeedbackItem[]
): OperatorHomeActivityItem[] {
  return [...recent]
    .sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )
    .map((item) => ({
      id: `feedback-${item.id}`,
      kind: "feedback" as const,
      feedbackId: item.id,
      comment: item.comment,
      guestName: item.guestName,
      createdAt: item.createdAt,
      sentiment:
        item.classificationStatus === "Succeeded" ? item.sentiment : null,
      canViewFeedback: true,
      canViewGuest: false,
    }))
}

function buildKpis(feedbackTotal: number | null): OperatorHomeKpi[] {
  const hasFeedback = feedbackTotal != null

  return [
    {
      id: "qr-scans",
      label: "QR scans",
      value: 0,
      trendPercent: null,
      hasRealData: false,
    },
    {
      id: "feedback",
      label: "Feedback submitted",
      value: feedbackTotal ?? 0,
      trendPercent: null,
      hasRealData: hasFeedback,
    },
    {
      id: "guests-joined",
      label: "Guests joined",
      value: 0,
      trendPercent: null,
      hasRealData: false,
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
  feedbackItems: OperatorHomeActivityItem[]
): Record<OperatorHomeActivityTabId, OperatorHomeActivityItem[]> {
  return {
    all: feedbackItems,
    feedback: feedbackItems,
    guests: [],
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
          id: "download-qr-materials",
          label: "Download QR materials",
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

  // Map API `guestUrl` onto the Home contract Smart Guest Link field.
  const smartGuestLink = selected.guestUrl.trim() || null
  const canPreviewGuestForm = smartGuestLink != null
  const canCopySmartGuestLink = smartGuestLink != null
  const feedbackItems = mapFeedbackActivity(input.feedback?.recent ?? [])
  const feedbackTotal =
    input.feedback != null ? input.feedback.total : null
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
    dateRangeLabel: "Last 7 days",
    setupSteps: buildSetupSteps({
      canPreviewGuestForm,
      logoUploaded: checklistAcks.logoUploaded,
      guestFormPreviewed: checklistAcks.guestFormPreviewed,
      qrPlacementGuideViewed: checklistAcks.qrPlacementGuideViewed,
      feedbackTotal,
    }),
    kpis: buildKpis(feedbackTotal),
    activityByTab: buildActivityByTab(feedbackItems),
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
