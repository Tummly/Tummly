import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type {
  CaptureDigitalGuestLinkChannel,
  CapturePlacementItem,
  CapturePlacementQrType,
  CaptureQrCodeStatus,
} from "@/types/dashboard"

/** Max length for Detail drawer / create internal description (ticket 04). */
export const PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH = 500

export const PLACEMENT_DETAIL_CONNECTED_GUEST_FORM =
  "Default guest feedback form" as const

export const PLACEMENT_DETAIL_CONNECTED_OFFER_STUB = "No offers" as const

export type PlacementDetailKind = "catalog" | "smartGuest" | "digital"

export type PlacementDetailDrawerView = {
  qrCodeId: number
  kind: PlacementDetailKind
  title: string
  status: CaptureQrCodeStatus
  locationName: string
  editGuestFormEnabled: false
  previewGuestExperienceEnabled: boolean
  canCopy: boolean
  canPauseOrActivate: boolean
  pauseActivateLabel:
    | "Pause placement"
    | "Activate placement"
    | "Pause link"
    | "Activate link"
    | null
  canRotate: boolean
  canArchive: boolean
  detailsSectionTitle: "Placement details" | "Link details"
  typeFieldLabel: "Placement type" | "Link type"
  typeValue: string
  channelLabel: string | null
  connectedGuestForm: typeof PLACEMENT_DETAIL_CONNECTED_GUEST_FORM
  connectedOfferText: typeof PLACEMENT_DETAIL_CONNECTED_OFFER_STUB
  createdDisplay: string
  lastUpdatedDisplay: string
  assetsSectionTitle: "QR assets" | "Link assets"
  showOrderPrintMaterials: boolean
  orderPrintMaterialsEnabled: false
  guestFormOpensText: string
  feedbackSubmittedText: string
  marketingOptInsText: string
  offerClaimsText: string
  submissionRateText: string
  lastScanText: string
  descriptionDraft: string
  descriptionPlaceholder: string
  descriptionMaxLength: typeof PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH
}

const QR_TYPE_LABELS: Record<CapturePlacementQrType, string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
  DigitalGuestLink: "Digital guest link",
}

const CHANNEL_LABELS: Record<CaptureDigitalGuestLinkChannel, string> = {
  SocialMedia: "Social media",
  Email: "Email",
  WhatsApp: "WhatsApp",
  Website: "Website",
  OnlineOrdering: "Online ordering",
  Other: "Other",
}

function digitalChannelDisplay(
  fact: Pick<CapturePlacementItem, "channel" | "channelLabel">
): string {
  if (fact.channelLabel != null && fact.channelLabel.trim() !== "") {
    return fact.channelLabel.trim()
  }
  if (fact.channel != null) {
    return CHANNEL_LABELS[fact.channel]
  }
  return "—"
}

export function placementDetailKindForQrType(
  qrType: CapturePlacementQrType
): PlacementDetailKind {
  if (qrType === "SmartGuest") {
    return "smartGuest"
  }
  if (qrType === "DigitalGuestLink") {
    return "digital"
  }
  return "catalog"
}

function formatActorTimestamp(
  at: string | null | undefined,
  by: string | null | undefined
): string {
  if (at == null || at === "") {
    return "—"
  }
  const date = new Date(at)
  if (Number.isNaN(date.getTime())) {
    return "—"
  }
  const datePart = date.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
  if (by == null || by.trim() === "") {
    return datePart
  }
  return `${datePart} by ${by.trim()}`
}

function submissionRateText(opens: number, feedbackSubmitted: number): string {
  if (opens <= 0) {
    return "—"
  }
  const rate = Math.round((feedbackSubmitted / opens) * 100)
  return `${rate}%`
}

export type BuildPlacementDetailDrawerInput = {
  fact: Omit<CapturePlacementItem, "status"> & { status: CaptureQrCodeStatus }
  locationName: string
  descriptionDraft: string
  /** When true, Pause/Activate is disabled (location capture paused). */
  locationCapturePaused?: boolean
  nowMs?: number
}

/** Build shared Placement / Link Detail drawer view for one QR code. */
export function buildPlacementDetailDrawer(
  input: BuildPlacementDetailDrawerInput
): PlacementDetailDrawerView {
  const { fact, locationName, descriptionDraft } = input
  const nowMs = input.nowMs ?? Date.now()
  const locationCapturePaused = input.locationCapturePaused === true
  const kind = placementDetailKindForQrType(fact.qrType)
  const isDigital = kind === "digital"
  const typeLabel = QR_TYPE_LABELS[fact.qrType]
  const title =
    isDigital && fact.linkName != null && fact.linkName.trim() !== ""
      ? fact.linkName.trim()
      : typeLabel
  const isArchived = fact.status === "Archived"

  const lastScanText =
    fact.lastScanAt == null || fact.lastScanAt === ""
      ? "—"
      : formatRelativeTime(fact.lastScanAt, nowMs) || "—"

  return {
    qrCodeId: fact.qrCodeId,
    kind,
    title,
    status: fact.status,
    locationName,
    editGuestFormEnabled: false,
    previewGuestExperienceEnabled: !isArchived,
    canCopy: !isArchived,
    canPauseOrActivate: !isArchived && !locationCapturePaused,
    pauseActivateLabel: isArchived
      ? null
      : fact.status === "Active"
        ? isDigital
          ? "Pause link"
          : "Pause placement"
        : isDigital
          ? "Activate link"
          : "Activate placement",
    canRotate: !isDigital && !isArchived,
    canArchive: !isArchived,
    detailsSectionTitle: isDigital ? "Link details" : "Placement details",
    typeFieldLabel: isDigital ? "Link type" : "Placement type",
    typeValue: isDigital ? "Digital guest link" : typeLabel,
    channelLabel: isDigital ? digitalChannelDisplay(fact) : null,
    connectedGuestForm: PLACEMENT_DETAIL_CONNECTED_GUEST_FORM,
    connectedOfferText: PLACEMENT_DETAIL_CONNECTED_OFFER_STUB,
    createdDisplay: formatActorTimestamp(
      fact.createdAt,
      fact.createdByDisplayName
    ),
    lastUpdatedDisplay: formatActorTimestamp(
      fact.updatedAt,
      fact.updatedByDisplayName
    ),
    assetsSectionTitle: isDigital ? "Link assets" : "QR assets",
    showOrderPrintMaterials: !isDigital,
    orderPrintMaterialsEnabled: false,
    guestFormOpensText: String(fact.qrScans),
    feedbackSubmittedText: String(fact.feedbackSubmitted),
    marketingOptInsText: String(fact.marketingOptIns ?? 0),
    offerClaimsText: String(fact.offerClaims ?? 0),
    submissionRateText: submissionRateText(
      fact.qrScans,
      fact.feedbackSubmitted
    ),
    lastScanText,
    descriptionDraft,
    descriptionPlaceholder: isDigital
      ? "Add a description about this digital guest link or any follow-up taken…"
      : "Add a description about this QR placement or any follow-up taken…",
    descriptionMaxLength: PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH,
  }
}
