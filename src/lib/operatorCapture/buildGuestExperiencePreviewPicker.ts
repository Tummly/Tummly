import type {
  CapturePlacementItem,
  CapturePlacementQrType,
} from "@/types/dashboard"

export type GuestExperiencePreviewPickerFact = Pick<
  CapturePlacementItem,
  "qrCodeId" | "qrType" | "status" | "linkName"
>

export type GuestExperiencePreviewPickerOption = {
  qrCodeId: number
  label: string
}

export type GuestExperiencePreviewPickerGroupId =
  | "qr-placements"
  | "digital-guest-links"

export type GuestExperiencePreviewPickerGroup = {
  id: GuestExperiencePreviewPickerGroupId
  label: string
  options: readonly GuestExperiencePreviewPickerOption[]
}

export type GuestExperiencePreviewPickerView = {
  groups: readonly GuestExperiencePreviewPickerGroup[]
  selectedQrCodeId: number | null
  selectedLabel: string | null
  canConfirm: boolean
}

const QR_TYPE_LABELS: Record<CapturePlacementQrType, string> = {
  CounterCard: "Counter card",
  PackagingSticker: "Packaging sticker",
  DeliveryInsert: "Delivery insert",
  WindowSticker: "Window sticker",
  SmartGuest: "Smart Guest",
  DigitalGuestLink: "Digital guest link",
}

export const GUEST_EXPERIENCE_PREVIEW_PICKER_GROUP_LABELS = {
  placements: "QR placements",
  digital: "Digital guest links",
} as const

function isPreviewable(fact: GuestExperiencePreviewPickerFact): boolean {
  return fact.status === "Active" || fact.status === "Paused"
}

function placementLabel(fact: GuestExperiencePreviewPickerFact): string {
  if (fact.qrType === "DigitalGuestLink") {
    const name = fact.linkName?.trim() ?? ""
    return name !== "" ? name : QR_TYPE_LABELS.DigitalGuestLink
  }
  return QR_TYPE_LABELS[fact.qrType]
}

/** Build Preview picker groups from Active/Paused QR codes at the location. */
export function buildGuestExperiencePreviewPicker(input: {
  placements: readonly GuestExperiencePreviewPickerFact[]
  selectedQrCodeId: number | null
}): GuestExperiencePreviewPickerView {
  const previewable = input.placements.filter(isPreviewable)

  const placementOptions = previewable
    .filter((fact) => fact.qrType !== "DigitalGuestLink")
    .map((fact) => ({
      qrCodeId: fact.qrCodeId,
      label: placementLabel(fact),
    }))

  const digitalOptions = previewable
    .filter((fact) => fact.qrType === "DigitalGuestLink")
    .map((fact) => ({
      qrCodeId: fact.qrCodeId,
      label: placementLabel(fact),
    }))
    .sort((a, b) =>
      a.label.localeCompare(b.label, "en", { sensitivity: "base" })
    )

  const groups: GuestExperiencePreviewPickerGroup[] = []
  if (placementOptions.length > 0) {
    groups.push({
      id: "qr-placements",
      label: GUEST_EXPERIENCE_PREVIEW_PICKER_GROUP_LABELS.placements,
      options: placementOptions,
    })
  }
  if (digitalOptions.length > 0) {
    groups.push({
      id: "digital-guest-links",
      label: GUEST_EXPERIENCE_PREVIEW_PICKER_GROUP_LABELS.digital,
      options: digitalOptions,
    })
  }

  const allOptions = groups.flatMap((group) => group.options)
  const selected =
    input.selectedQrCodeId == null
      ? null
      : (allOptions.find(
          (option) => option.qrCodeId === input.selectedQrCodeId
        ) ?? null)

  return {
    groups,
    selectedQrCodeId: selected?.qrCodeId ?? null,
    selectedLabel: selected?.label ?? null,
    canConfirm: selected != null,
  }
}
