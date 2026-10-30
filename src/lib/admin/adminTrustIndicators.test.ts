import { describe, expect, it } from "vitest"

import type {
  AdminTrustIndicatorItem,
  TrustIndicatorValues,
} from "@/api/adminTrustIndicatorsApi"

import { TRUST_STATUSES } from "@/api/adminTrustIndicatorsApi"

import {
  allowedTrustCtaTypes,
  allowedTrustStatuses,
  describeTrustValue,
  diffTrustIndicatorValues,
  nextCtaTargetOnTypeChange,
  TRUST_CTA_TYPE_LABELS,
  TRUST_EDITOR_RELOAD_FAILED_MESSAGE,
  TRUST_EDITOR_RELOADED_MESSAGE,
  TRUST_EVIDENCE_CLASS_LABELS,
  TRUST_STATUS_LABELS,
  toTrustSaveValues,
  trustDraftChanges,
  trustItemPublishState,
  trustPublishErrorMessage,
  trustSaveErrorMessage,
} from "./adminTrustIndicators"

function values(overrides: Partial<TrustIndicatorValues> = {}): TrustIndicatorValues {
  return {
    enabled: false,
    status: "Roadmap",
    statusLabel: "Roadmap (not awarded)",
    ctaType: "None",
    ctaLabel: null,
    ctaTarget: null,
    evidenceUrl: null,
    imageUrl: null,
    imageRightsNote: null,
    iconKey: null,
    altText: "",
    tooltip: "",
    issuer: null,
    scope: null,
    issuedOn: null,
    expiresOn: null,
    reviewOn: null,
    ...overrides,
  }
}

function item(
  itemNumber: number,
  overrides: Partial<TrustIndicatorValues> = {}
): AdminTrustIndicatorItem {
  return {
    itemNumber,
    title: `Item ${itemNumber}`,
    evidenceClass: "Roadmap",
    values: values(overrides),
    lastEditedByAdminUserId: null,
    lastEditedAtUtc: null,
  }
}

describe("TRUST_STATUSES", () => {
  it("pins the six canonical internal statuses in order", () => {
    expect([...TRUST_STATUSES]).toEqual([
      "VerifiedPublicRecord",
      "VerifiedCredential",
      "Roadmap",
      "QaPending",
      "QaPassed",
      "UnderReview",
    ])
  })
})

describe("TRUST_STATUS_LABELS", () => {
  it("labels each internal status with the CONTRACT wording", () => {
    expect(TRUST_STATUS_LABELS).toEqual({
      VerifiedPublicRecord: "Verified public record",
      VerifiedCredential: "Verified credential",
      Roadmap: "Roadmap (not awarded)",
      QaPending: "QA pending",
      QaPassed: "QA passed",
      UnderReview: "Under review",
    })
  })
})

describe("TRUST_EVIDENCE_CLASS_LABELS", () => {
  it("labels own-control items as Own control", () => {
    expect(TRUST_EVIDENCE_CLASS_LABELS.OwnControlQaPending).toBe("Own control")
  })
})

describe("TRUST_CTA_TYPE_LABELS", () => {
  it("labels the cookie settings CTA", () => {
    expect(TRUST_CTA_TYPE_LABELS.CookieSettings).toBe("Open cookie settings")
  })
})

describe("allowedTrustStatuses", () => {
  it("offers the Verified-mapped and neutral statuses for public-record items", () => {
    expect(allowedTrustStatuses("VerifiedPublicRecord")).toEqual([
      "VerifiedPublicRecord",
      "UnderReview",
    ])
  })

  it("offers credential and neutral statuses for self-assessment items", () => {
    expect(allowedTrustStatuses("VerifiedSelfAssessment")).toEqual([
      "VerifiedCredential",
      "UnderReview",
    ])
  })

  it("offers only Roadmap for roadmap items", () => {
    expect(allowedTrustStatuses("Roadmap")).toEqual(["Roadmap"])
  })

  it("offers QA pending and QA passed for own-control items", () => {
    expect(allowedTrustStatuses("OwnControlQaPending")).toEqual(["QaPending", "QaPassed"])
  })
})

describe("allowedTrustCtaTypes", () => {
  it("always includes None", () => {
    expect(allowedTrustCtaTypes("Roadmap", "Roadmap")).toEqual(["None"])
    expect(allowedTrustCtaTypes("OwnControlQaPending", "QaPending")).toEqual(["None"])
  })

  it("adds ExternalProof for verified public-record and credential statuses", () => {
    expect(allowedTrustCtaTypes("VerifiedPublicRecord", "VerifiedPublicRecord")).toEqual([
      "None",
      "ExternalProof",
    ])
    expect(allowedTrustCtaTypes("VerifiedSelfAssessment", "VerifiedCredential")).toEqual([
      "None",
      "ExternalProof",
    ])
  })

  it("never offers ExternalProof for neutral statuses", () => {
    expect(allowedTrustCtaTypes("VerifiedPublicRecord", "UnderReview")).toEqual(["None"])
  })

  it("adds InternalPage and CookieSettings for own-control QA passed", () => {
    expect(allowedTrustCtaTypes("OwnControlQaPending", "QaPassed")).toEqual([
      "None",
      "InternalPage",
      "CookieSettings",
    ])
  })

  it("offers no internal CTA while own-control QA is pending", () => {
    expect(allowedTrustCtaTypes("OwnControlQaPending", "QaPending")).toEqual(["None"])
  })
})

describe("trustPublishErrorMessage", () => {
  it("explains that only an approver may publish, without reloading", () => {
    expect(trustPublishErrorMessage("approver_required", "Forbidden.")).toEqual({
      message: "Only a Director or compliance approver can publish.",
      reload: false,
    })
  })

  it("shows the server message for a self-approval refusal", () => {
    expect(
      trustPublishErrorMessage("self_approval_denied", "A second admin must publish.")
    ).toEqual({ message: "A second admin must publish.", reload: false })
  })

  it.each(["draft_changed", "draft_conflict", "publish_conflict"])(
    "reloads the view after %s",
    (code) => {
      expect(trustPublishErrorMessage(code, "Server text.")).toEqual({
        message: "The draft changed. It has been reloaded — review it again.",
        reload: true,
      })
    }
  )

  it("shows the server message and does not reload after a reused idempotency key", () => {
    expect(trustPublishErrorMessage("idempotency_key_reused", "Key reused.")).toEqual({
      message: "Key reused.",
      reload: false,
    })
  })

  it("tells the approver to submit first when nothing is submitted", () => {
    expect(trustPublishErrorMessage("not_submitted", "Server text.")).toEqual({
      message: "Submit the draft for review before publishing.",
      reload: false,
    })
  })

  it("explains the gate failure and leaves the details to the panel", () => {
    expect(trustPublishErrorMessage("publish_gate_failed", "Server text.")).toEqual({
      message:
        "Some items cannot be published yet. Fix them, submit again, then publish.",
      reload: false,
    })
  })

  it("keeps the server message for an unknown code", () => {
    expect(trustPublishErrorMessage("something_new", "Server text.")).toEqual({
      message: "Server text.",
      reload: false,
    })
  })

  it("falls back to a generic message when the server sends no text", () => {
    expect(trustPublishErrorMessage("something_new", "  ")).toEqual({
      message: "Could not publish the trust indicators.",
      reload: false,
    })
  })
})

describe("diffTrustIndicatorValues", () => {
  it("returns nothing when draft and published values match", () => {
    expect(diffTrustIndicatorValues(values(), values())).toEqual([])
  })

  it("lists each changed field with its published and draft text", () => {
    const diff = diffTrustIndicatorValues(
      values({ enabled: true, ctaType: "ExternalProof", evidenceUrl: "https://a.example" }),
      values({ enabled: false })
    )

    expect(diff).toEqual([
      { field: "enabled", label: "Shown on website", published: "Off", draft: "On" },
      {
        field: "ctaType",
        label: "CTA type",
        published: "No CTA",
        draft: "External proof link",
      },
      {
        field: "evidenceUrl",
        label: "Evidence URL",
        published: "—",
        draft: "https://a.example",
      },
    ])
  })

  it("reports status, statusLabel, ctaLabel and imageRightsNote changes", () => {
    const diff = diffTrustIndicatorValues(
      values({
        status: "QaPassed",
        statusLabel: "QA passed",
        ctaLabel: "Open settings",
        imageRightsNote: "Licensed",
      }),
      values()
    )

    expect(diff.map((row) => row.field)).toEqual([
      "status",
      "statusLabel",
      "ctaLabel",
      "imageRightsNote",
    ])
    expect(diff[0]).toEqual({
      field: "status",
      label: "Status",
      published: "Roadmap (not awarded)",
      draft: "QA passed",
    })
  })
})

describe("trustItemPublishState", () => {
  it("marks an item as not published before any publication", () => {
    expect(trustItemPublishState(item(1), undefined)).toBe("not_published")
  })

  it("marks an item that matches the live version as unchanged", () => {
    expect(trustItemPublishState(item(1), item(1))).toBe("unchanged")
  })

  it("marks an item whose draft differs from the live version as changed", () => {
    expect(trustItemPublishState(item(1, { tooltip: "New" }), item(1))).toBe("changed")
  })
})

describe("trustDraftChanges", () => {
  it("returns only draft items that differ from their live version", () => {
    const draft = [item(1, { enabled: true }), item(2), item(3)]
    const live = [item(1), item(2), item(3)]

    const changes = trustDraftChanges(draft, live)

    expect(changes.map((row) => row.item.itemNumber)).toEqual([1])
    expect(changes[0].differences.map((row) => row.field)).toEqual(["enabled"])
  })

  it("returns nothing when no version is live", () => {
    expect(trustDraftChanges([item(1, { enabled: true })], null)).toEqual([])
  })
})

describe("trustSaveErrorMessage", () => {
  it.each(["draft_changed", "draft_conflict", "publish_conflict"])(
    "tells the editor user the form was reset after %s",
    (code) => {
      expect(trustSaveErrorMessage(code, "Server text.")).toEqual({
        message: TRUST_EDITOR_RELOADED_MESSAGE,
        reload: true,
      })
    }
  )

  it("states that the editor now shows the latest values", () => {
    expect(TRUST_EDITOR_RELOADED_MESSAGE).toBe(
      "Another admin changed the draft. Your editor now shows the latest values — make your change again."
    )
  })

  it("keeps the server message for other save refusals", () => {
    expect(trustSaveErrorMessage("validation_failed", "Alt text is required.")).toEqual({
      message: "Alt text is required.",
      reload: false,
    })
  })

  it("falls back to a generic save message when the server sends no text", () => {
    expect(trustSaveErrorMessage("", " ")).toEqual({
      message: "Could not save the draft item.",
      reload: false,
    })
  })

  it("has a truthful message for a failed reload", () => {
    expect(TRUST_EDITOR_RELOAD_FAILED_MESSAGE).toBe(
      "Another admin changed the draft. The latest values could not be loaded. Close the editor and open the item again."
    )
  })
})

describe("nextCtaTargetOnTypeChange", () => {
  it("clears the target when switching between ExternalProof and InternalPage", () => {
    expect(nextCtaTargetOnTypeChange("ExternalProof", "InternalPage", "https://a.example")).toBeNull()
    expect(nextCtaTargetOnTypeChange("InternalPage", "ExternalProof", "/settings")).toBeNull()
  })

  it("keeps the target when the type does not change", () => {
    expect(nextCtaTargetOnTypeChange("InternalPage", "InternalPage", "/settings")).toBe("/settings")
  })

  it("clears the target for None and CookieSettings", () => {
    expect(nextCtaTargetOnTypeChange("ExternalProof", "None", "https://a.example")).toBeNull()
    expect(nextCtaTargetOnTypeChange("InternalPage", "CookieSettings", "/x")).toBeNull()
  })

  it("keeps the target when a type that takes a target is picked", () => {
    expect(nextCtaTargetOnTypeChange("None", "ExternalProof", "https://a.example")).toBe(
      "https://a.example"
    )
    expect(nextCtaTargetOnTypeChange("None", "InternalPage", "/settings")).toBe("/settings")
  })

})

describe("toTrustSaveValues", () => {
  it("turns blank optional fields and dates into null, and trims text", () => {
    const saved = toTrustSaveValues(
      values({
        evidenceUrl: "   ",
        issuer: "  ICO  ",
        issuedOn: "",
        expiresOn: "2027-01-31",
        altText: "  Badge  ",
        tooltip: "  ",
      })
    )

    expect(saved.evidenceUrl).toBeNull()
    expect(saved.issuer).toBe("ICO")
    expect(saved.issuedOn).toBeNull()
    expect(saved.expiresOn).toBe("2027-01-31")
    expect(saved.altText).toBe("Badge")
    expect(saved.tooltip).toBe("")
  })

  it("blanks ctaLabel and imageRightsNote to null", () => {
    const saved = toTrustSaveValues(
      values({ ctaLabel: "   ", imageRightsNote: "" })
    )

    expect(saved.ctaLabel).toBeNull()
    expect(saved.imageRightsNote).toBeNull()
  })

  it("keeps a filled ctaLabel and imageRightsNote, trimmed", () => {
    const saved = toTrustSaveValues(
      values({ ctaLabel: " Open settings ", imageRightsNote: " Licensed " })
    )

    expect(saved.ctaLabel).toBe("Open settings")
    expect(saved.imageRightsNote).toBe("Licensed")
  })
})

describe("describeTrustValue", () => {
  it("formats the switch, empty values and CTA labels for people", () => {
    expect(describeTrustValue("enabled", true)).toBe("On")
    expect(describeTrustValue("enabled", false)).toBe("Off")
    expect(describeTrustValue("scope", "")).toBe("—")
    expect(describeTrustValue("ctaType", "InternalPage")).toBe("Internal page")
    expect(describeTrustValue("ctaType", "CookieSettings")).toBe("Open cookie settings")
  })

  it("shows the status label text for the status field", () => {
    expect(describeTrustValue("status", "QaPassed")).toBe("QA passed")
  })
})
