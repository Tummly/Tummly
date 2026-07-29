import { describe, expect, it } from "vitest"

import { CAPTURE_CONNECTED_OFFERS_STUB } from "./buildCaptureGuestExperience"
import {
  CAPTURE_BREADCRUMB_LINK_CLASS,
  CAPTURE_BREADCRUMB_MUTED_LINK_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS,
  CAPTURE_GUEST_FORM_STUB,
  CAPTURE_GUEST_PREVIEW_DEVICE,
  CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS,
  CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
  CAPTURE_GUEST_PREVIEW_PAGE_TAB,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
  CAPTURE_MATERIALS_INNER_TITLE_CLASS,
  CAPTURE_PAGE_TITLE_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS,
  OPERATOR_CAPTURE_GUEST_PREVIEW_COPY,
} from "./capturePresentation"
import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

describe("capturePresentation — operator token audit", () => {
  it("prefers text-op-card-title-color over remapped text-foreground for titles", () => {
    for (const titleClass of [
      CAPTURE_PAGE_TITLE_CLASS,
      CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
    ]) {
      expect(titleClass).toContain("text-op-card-title-color")
      expect(titleClass).not.toContain("text-foreground")
    }
  })

  it("prefers text-op-text-primary over remapped text-foreground for body/value copy", () => {
    for (const bodyClass of [
      CAPTURE_GUEST_EXPERIENCE_VALUE_CLASS,
      CAPTURE_MATERIALS_INNER_TITLE_CLASS,
      CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
      CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS,
      CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
      CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
      CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
      CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
      CAPTURE_EMPTY_TITLE_CLASS,
    ]) {
      expect(bodyClass).toContain("text-op-text-primary")
      expect(bodyClass).not.toContain("text-foreground")
    }
  })

  it("prefers explicit op text tokens for breadcrumb links over remapped text-foreground", () => {
    expect(CAPTURE_BREADCRUMB_LINK_CLASS).toContain("text-op-text-primary")
    expect(CAPTURE_BREADCRUMB_LINK_CLASS).not.toContain("text-foreground")
    expect(CAPTURE_BREADCRUMB_MUTED_LINK_CLASS).toContain("text-op-text-muted")
    expect(CAPTURE_BREADCRUMB_MUTED_LINK_CLASS).not.toContain("text-foreground")
  })

  it("shares the operator shell dropdown chrome for the placements row actions menu", () => {
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS).toContain(
      OPERATOR_SHELL_MENU_PANEL_CLASS
    )
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS).toBe(
      OPERATOR_SHELL_MENU_ITEM_CLASS
    )
  })

  it("keeps the row actions trigger a plain icon-sized hit area for the op-ghost variant", () => {
    expect(CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS).toBe("size-8")
  })

  it("defines presentational preview chrome copy for issue 23", () => {
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.guestFormValue).toBe(
      CAPTURE_GUEST_FORM_STUB
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.connectedOfferLabel).toBe(
      "Connected offer"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.openPreviewInNewTabCta).toBe(
      "Open preview in new tab"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.feedbackPageTab).toBe(
      "Feedback page"
    )
    expect(OPERATOR_CAPTURE_GUEST_PREVIEW_COPY.thankYouPageTab).toBe(
      "Thank you page"
    )
  })

  it("locks preview tab and device values for the overlay chrome", () => {
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback).toBe("feedback")
    expect(CAPTURE_GUEST_PREVIEW_PAGE_TAB.thankYou).toBe("thank-you")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE.desktop).toBe("desktop")
    expect(CAPTURE_GUEST_PREVIEW_DEVICE.mobile).toBe("mobile")
    expect(CAPTURE_CONNECTED_OFFERS_STUB).toBe("No active offers")
  })
})
