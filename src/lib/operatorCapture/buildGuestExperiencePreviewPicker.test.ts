import { describe, expect, it } from "vitest"

import {
  buildGuestExperiencePreviewPicker,
  type GuestExperiencePreviewPickerFact,
} from "./buildGuestExperiencePreviewPicker"

function fact(
  overrides: Partial<GuestExperiencePreviewPickerFact> &
    Pick<GuestExperiencePreviewPickerFact, "qrCodeId" | "qrType" | "status">
): GuestExperiencePreviewPickerFact {
  return {
    linkName: null,
    ...overrides,
  }
}

describe("buildGuestExperiencePreviewPicker", () => {
  it("groups QR placements before digital links, sorting each group alphabetically by label", () => {
    const view = buildGuestExperiencePreviewPicker({
      placements: [
        fact({
          qrCodeId: 30,
          qrType: "DigitalGuestLink",
          status: "Active",
          linkName: "Zulu social",
        }),
        fact({
          qrCodeId: 10,
          qrType: "WindowSticker",
          status: "Paused",
        }),
        fact({
          qrCodeId: 20,
          qrType: "CounterCard",
          status: "Active",
        }),
        fact({
          qrCodeId: 31,
          qrType: "DigitalGuestLink",
          status: "Paused",
          linkName: "Alpha email",
        }),
        fact({
          qrCodeId: 11,
          qrType: "SmartGuest",
          status: "Active",
        }),
      ],
      selectedQrCodeId: null,
    })

    expect(view.groups).toEqual([
      {
        id: "qr-placements",
        label: "QR placements",
        options: [
          { qrCodeId: 20, label: "Counter card" },
          { qrCodeId: 11, label: "Smart Guest" },
          { qrCodeId: 10, label: "Window sticker" },
        ],
      },
      {
        id: "digital-guest-links",
        label: "Digital guest links",
        options: [
          { qrCodeId: 31, label: "Alpha email" },
          { qrCodeId: 30, label: "Zulu social" },
        ],
      },
    ])
    expect(view.canConfirm).toBe(false)
    expect(view.selectedQrCodeId).toBeNull()
  })

  it("enables confirm when a selectable option is chosen", () => {
    const view = buildGuestExperiencePreviewPicker({
      placements: [
        fact({ qrCodeId: 1, qrType: "SmartGuest", status: "Active" }),
        fact({
          qrCodeId: 2,
          qrType: "DigitalGuestLink",
          status: "Active",
          linkName: "Instagram",
        }),
      ],
      selectedQrCodeId: 2,
    })

    expect(view.canConfirm).toBe(true)
    expect(view.selectedQrCodeId).toBe(2)
    expect(view.selectedLabel).toBe("Instagram")
  })

  it("falls back to Digital guest link when Link name is missing", () => {
    const view = buildGuestExperiencePreviewPicker({
      placements: [
        fact({
          qrCodeId: 1,
          qrType: "DigitalGuestLink",
          status: "Active",
          linkName: "   ",
        }),
        fact({ qrCodeId: 2, qrType: "SmartGuest", status: "Active" }),
      ],
      selectedQrCodeId: 1,
    })

    expect(view.groups[1]?.options).toEqual([
      { qrCodeId: 1, label: "Digital guest link" },
    ])
    expect(view.selectedLabel).toBe("Digital guest link")
  })

  it("ignores a selected id that is not in the previewable universe", () => {
    const view = buildGuestExperiencePreviewPicker({
      placements: [
        fact({ qrCodeId: 1, qrType: "SmartGuest", status: "Active" }),
        fact({ qrCodeId: 2, qrType: "CounterCard", status: "Active" }),
      ],
      selectedQrCodeId: 99,
    })

    expect(view.canConfirm).toBe(false)
    expect(view.selectedQrCodeId).toBeNull()
    expect(view.selectedLabel).toBeNull()
  })
})
