import {
  PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
  PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
} from "@/lib/operatorCapture/buildPauseActivateConfirm"
import { placementDetailKindForQrType } from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import type {
  CaptureArchivedPlacementItem,
  CapturePlacementQrType,
} from "@/types/dashboard"

export type RestoreConfirmView = {
  qrCodeId: number
  locationId: number
  title: string
  body: string
  nameLabel: "Placement" | "Link name"
  nameValue: string
  locationName: string
  connectedGuestForm: typeof PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM
  connectedOfferText: typeof PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER
  /** Channel row for digital only. */
  channelLabel: string | null
  channelValue: string | null
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

const CHANNEL_LABELS: Record<string, string> = {
  SocialMedia: "Social media",
  Email: "Email",
  WhatsApp: "WhatsApp",
  Website: "Website",
  OnlineOrdering: "Online ordering",
  Other: "Other",
}

const PRINT_WARNING =
  "Activating the restored placement may make existing printed materials using this QR code work again."

const DIGITAL_WARNING =
  "Activating the restored link will make the guest URL work again for anyone who still has it."

function displayName(fact: CaptureArchivedPlacementItem, isDigital: boolean): string {
  if (isDigital && fact.linkName != null && fact.linkName.trim() !== "") {
    return fact.linkName.trim()
  }
  return QR_TYPE_LABELS[fact.qrType]
}

/** Build Restore confirm dialogue copy for an archived QR code. */
export function buildRestoreConfirm(
  fact: CaptureArchivedPlacementItem
): RestoreConfirmView {
  const isDigital = placementDetailKindForQrType(fact.qrType) === "digital"
  const name = displayName(fact, isDigital)

  return {
    qrCodeId: fact.qrCodeId,
    locationId: fact.locationId,
    title: isDigital ? "Restore digital guest link?" : "Restore QR placement?",
    body: isDigital
      ? "This link will return to the Digital guest links list in a paused state. Review its guest form and offer before activating it."
      : "This placement will return to the QR placements list in a paused state. Review its guest form and offer before activating it.",
    nameLabel: isDigital ? "Link name" : "Placement",
    nameValue: name,
    locationName: fact.locationName,
    connectedGuestForm: PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
    connectedOfferText: PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
    channelLabel: isDigital ? "Where will you use it?" : null,
    channelValue: isDigital
      ? (fact.channel != null ? CHANNEL_LABELS[fact.channel] ?? fact.channel : "—")
      : null,
    warningText: isDigital ? DIGITAL_WARNING : PRINT_WARNING,
    primaryLabel: isDigital ? "Restore link" : "Restore placement",
    cancelLabel: "Cancel",
    successToastMessage: `${name} restored. It’s paused — activate it when you’re ready.`,
  }
}
