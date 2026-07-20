import { describe, expect, it } from "vitest"

import type { OperatorHomeSetupStep } from "@/types/operatorHome"
import {
  countCompleteSetupSteps,
  getSetupStepIllustration,
  hasSetupStepTintedRow,
  resolveSetupActionButtonVariant,
  SETUP_STEP_COPY_GAP_CLASS,
  SETUP_STEP_DESCRIPTION_CLASS,
  SETUP_STEP_TITLE_CLASS,
  shouldShowSetupStatusMarker,
  shouldSpreadSetupStepActions,
} from "./setupChecklistPresentation"

const sampleSteps: OperatorHomeSetupStep[] = [
  {
    id: "account-ready",
    stepNumber: 1,
    title: "Account ready",
    description: "",
    status: "complete",
    actions: [],
  },
  {
    id: "upload-logo",
    stepNumber: 2,
    title: "Upload restaurant logo",
    description: "",
    status: "partial",
    actions: [],
  },
  {
    id: "qr-placement",
    stepNumber: 5,
    title: "Place your QR materials",
    description: "",
    status: "incomplete",
    actions: [],
  },
]

describe("setupChecklistPresentation", () => {
  it("counts only complete steps in progress subtitle inputs", () => {
    expect(countCompleteSetupSteps(sampleSteps)).toEqual({
      completeCount: 1,
      totalSteps: 3,
    })
  })

  it("shows status markers only for complete and partial steps", () => {
    expect(shouldShowSetupStatusMarker("complete")).toBe(true)
    expect(shouldShowSetupStatusMarker("partial")).toBe(true)
    expect(shouldShowSetupStatusMarker("incomplete")).toBe(false)
  })

  it("tints rows only for complete and partial steps", () => {
    expect(hasSetupStepTintedRow("complete")).toBe(true)
    expect(hasSetupStepTintedRow("partial")).toBe(true)
    expect(hasSetupStepTintedRow("incomplete")).toBe(false)
  })

  it("maps checklist CTA ids to operator button variants", () => {
    expect(resolveSetupActionButtonVariant("upload-logo")).toBe(
      "operator-secondary"
    )
    expect(resolveSetupActionButtonVariant("preview-guest-form")).toBe(
      "operator-secondary"
    )
    expect(resolveSetupActionButtonVariant("view-placement-guide")).toBe(
      "operator-tertiary"
    )
    expect(resolveSetupActionButtonVariant("download-qr-materials")).toBe(
      "operator-tertiary"
    )
    expect(resolveSetupActionButtonVariant("create-offer")).toBe(
      "operator-tertiary"
    )
  })

  it("spreads CTAs only on incomplete rows", () => {
    expect(shouldSpreadSetupStepActions("incomplete", 2)).toBe(true)
    expect(shouldSpreadSetupStepActions("incomplete", 1)).toBe(true)
    expect(shouldSpreadSetupStepActions("partial", 1)).toBe(false)
    expect(shouldSpreadSetupStepActions("complete", 0)).toBe(false)
  })

  it("uses per-step Figma illustration frame sizes", () => {
    expect(getSetupStepIllustration("account-ready")).toEqual({
      height: 35,
      crop: {
        width: "154.26%",
        height: "162.5%",
        left: "-34.59%",
        top: "-31.25%",
      },
    })
    expect(getSetupStepIllustration("guest-form").height).toBe(37)
    expect(getSetupStepIllustration("first-response").height).toBe(43)
    expect(getSetupStepIllustration("qr-placement").height).toBe(47)
    expect(getSetupStepIllustration("upload-logo").crop).toBe("cover")
  })

  it("uses Figma step copy metrics (16/24 title, 14/17 description)", () => {
    expect(SETUP_STEP_TITLE_CLASS).toContain("leading-6")
    expect(SETUP_STEP_DESCRIPTION_CLASS).toContain("leading-[17px]")
    expect(SETUP_STEP_COPY_GAP_CLASS).toBe("gap-[8px]")
  })
})
