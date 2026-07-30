import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import { placementDetailKindForQrType } from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import type {
  CapturePlacementItem,
  CapturePlacementQrType,
  CapturePlacementStatus,
} from "@/types/dashboard"

export const PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM =
  "Default guest feedback form" as const

/** Stub until offers product ships (ticket 06). */
export const PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER = "No offer" as const

export type PauseActivateConfirmAction = "pause" | "activate"

export type PauseActivateConfirmView = {
  qrCodeId: number
  action: PauseActivateConfirmAction
  title: string
  body: string
  nameLabel: "Placement" | "Link name"
  nameValue: string
  locationName: string
  /** Pause only — null on Activate. */
  currentStatus: CapturePlacementStatus | null
  /** Pause only — null on Activate. */
  lastScanText: string | null
  /** Activate only — null on Pause. */
  connectedGuestForm: typeof PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM | null
  /** Activate only — null on Pause. */
  connectedOfferText: typeof PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER | null
  warningText: string | null
  primaryLabel: string
  cancelLabel: "Cancel"
  successToastMessage: string
}

const QR_TYPE_LABELS: Record<CapturePlacementQrType, string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
  DigitalGuestLink: "Digital guest link",
}

const PRINT_WARNING =
  "Any printed materials using this QR code will remain in circulation but will not work while the placement is paused."

function displayName(fact: CapturePlacementItem, isDigital: boolean): string {
  if (isDigital && fact.linkName != null && fact.linkName.trim() !== "") {
    return fact.linkName.trim()
  }
  return QR_TYPE_LABELS[fact.qrType]
}

export type BuildPauseActivateConfirmInput = {
  fact: CapturePlacementItem
  action: PauseActivateConfirmAction
  locationName: string
  nowMs?: number
}

/** Build Pause / Activate confirm dialogue copy for one QR code. */
export function buildPauseActivateConfirm(
  input: BuildPauseActivateConfirmInput
): PauseActivateConfirmView {
  const { fact, action, locationName } = input
  const nowMs = input.nowMs ?? Date.now()
  const isDigital = placementDetailKindForQrType(fact.qrType) === "digital"
  const name = displayName(fact, isDigital)

  if (action === "pause") {
    const lastScanText =
      fact.lastScanAt == null || fact.lastScanAt === ""
        ? "—"
        : formatRelativeTime(fact.lastScanAt, nowMs) || "—"

    return {
      qrCodeId: fact.qrCodeId,
      action: "pause",
      title: isDigital ? "Pause digital guest link?" : "Pause QR placement?",
      body: isDigital
        ? "Guests using this link will not be able to open the guest form or submit feedback until it is activated again. Historical performance will remain available."
        : "Guests using this placement will not be able to open the guest form or submit feedback until it is activated again. Historical performance will remain available.",
      nameLabel: isDigital ? "Link name" : "Placement",
      nameValue: name,
      locationName,
      currentStatus: fact.status,
      lastScanText,
      connectedGuestForm: null,
      connectedOfferText: null,
      warningText: isDigital ? null : PRINT_WARNING,
      primaryLabel: isDigital ? "Pause link" : "Pause placement",
      cancelLabel: "Cancel",
      successToastMessage: `${name} is now paused. You can activate it again at any time.`,
    }
  }

  return {
    qrCodeId: fact.qrCodeId,
    action: "activate",
    title: isDigital ? "Activate digital guest link?" : "Activate QR placement?",
    body: isDigital
      ? "Activating this link will allow guests to open the connected guest form using this digital guest link."
      : "Activating this placement will allow guests to open the connected guest form using its QR code or Smart Guest Link.",
    nameLabel: isDigital ? "Link name" : "Placement",
    nameValue: name,
    locationName,
    currentStatus: null,
    lastScanText: null,
    connectedGuestForm: PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
    connectedOfferText: PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
    warningText: null,
    primaryLabel: isDigital ? "Activate link" : "Activate placement",
    cancelLabel: "Cancel",
    successToastMessage: `${name} is now active. Guests can use it again.`,
  }
}
