import { describe, expect, it } from "vitest"

import {
  canApplyEditDetectedTags,
  DETECTED_TAG_KEYS,
  detectedTagSetsEqual,
  stageDetectedTagKey,
  unstageDetectedTagKey,
} from "./detectedTags"

describe("detectedTags edit helpers", () => {
  it("stages Other alone and clears other keys", () => {
    expect(stageDetectedTagKey(["Service", "FoodQuality"], "Other")).toEqual([
      "Other",
    ])
  })

  it("staging a non-Other key removes Other", () => {
    expect(stageDetectedTagKey(["Other"], "Service")).toEqual(["Service"])
  })

  it("does not duplicate an already staged key", () => {
    expect(stageDetectedTagKey(["Service"], "Service")).toEqual(["Service"])
  })

  it("unstages a key", () => {
    expect(unstageDetectedTagKey(["Service", "FoodQuality"], "Service")).toEqual(
      ["FoodQuality"]
    )
  })

  it("compares tag sets order-insensitively", () => {
    expect(
      detectedTagSetsEqual(["Service", "FoodQuality"], ["FoodQuality", "Service"])
    ).toBe(true)
    expect(detectedTagSetsEqual(["Service"], ["FoodQuality"])).toBe(false)
  })

  it("disables Apply when unchanged on Succeeded", () => {
    expect(
      canApplyEditDetectedTags({
        classificationStatus: "Succeeded",
        openTagKeys: ["Service"],
        draftTagKeys: ["Service"],
        draftSentiment: null,
        saveStatus: "idle",
      })
    ).toBe(false)
  })

  it("enables Apply when tags change on Succeeded", () => {
    expect(
      canApplyEditDetectedTags({
        classificationStatus: "Succeeded",
        openTagKeys: ["Service"],
        draftTagKeys: ["Service", "FoodQuality"],
        draftSentiment: null,
        saveStatus: "idle",
      })
    ).toBe(true)
  })

  it("allows empty tag set as a change on Succeeded", () => {
    expect(
      canApplyEditDetectedTags({
        classificationStatus: "Succeeded",
        openTagKeys: ["Service"],
        draftTagKeys: [],
        draftSentiment: null,
        saveStatus: "idle",
      })
    ).toBe(true)
  })

  it("requires sentiment on Failed before Apply", () => {
    expect(
      canApplyEditDetectedTags({
        classificationStatus: "Failed",
        openTagKeys: [],
        draftTagKeys: ["Service"],
        draftSentiment: null,
        saveStatus: "idle",
      })
    ).toBe(false)
    expect(
      canApplyEditDetectedTags({
        classificationStatus: "Failed",
        openTagKeys: [],
        draftTagKeys: [],
        draftSentiment: "negative",
        saveStatus: "idle",
      })
    ).toBe(true)
  })

  it("exposes the closed vocabulary keys", () => {
    expect(DETECTED_TAG_KEYS).toContain("Other")
    expect(DETECTED_TAG_KEYS).toContain("FoodQuality")
  })
})
