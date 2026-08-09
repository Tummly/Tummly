import { describe, expect, it } from "vitest"

import {
  campaignCommitConfirmCopy,
  campaignCommitSuccessChrome,
  campaignReviewPrimaryActionLabel,
  CAMPAIGN_COMMIT_COPY,
} from "@/lib/operatorCampaigns/campaignCommitPresentation"

describe("campaignCommitPresentation", () => {
  it("returns send-now confirm and primary labels", () => {
    expect(campaignReviewPrimaryActionLabel("send-now")).toBe(
      CAMPAIGN_COMMIT_COPY.sendNowPrimary
    )
    expect(campaignCommitConfirmCopy({ modeId: "send-now" })).toMatchObject({
      title: CAMPAIGN_COMMIT_COPY.sendNowConfirmTitle,
      confirmLabel: CAMPAIGN_COMMIT_COPY.sendNowConfirmLabel,
    })
  })

  it("returns schedule-later confirm and primary labels", () => {
    expect(campaignReviewPrimaryActionLabel("schedule-later")).toBe(
      CAMPAIGN_COMMIT_COPY.scheduleLaterPrimary
    )
    expect(
      campaignCommitConfirmCopy({ modeId: "schedule-later" })
    ).toMatchObject({
      title: CAMPAIGN_COMMIT_COPY.scheduleLaterConfirmTitle,
      confirmLabel: CAMPAIGN_COMMIT_COPY.scheduleLaterConfirmLabel,
    })
  })

  it("builds sending vs scheduled success chrome", () => {
    const sending = campaignCommitSuccessChrome({
      modeId: "send-now",
      campaignName: "Thanks",
      scheduledAtUtc: null,
      committedAt: new Date("2026-08-14T14:18:00.000Z"),
    })
    expect(sending.title).toBe("Campaign sending")
    expect(sending.rows[0]?.value).toBe("Sending")

    const scheduled = campaignCommitSuccessChrome({
      modeId: "schedule-later",
      campaignName: "Brunch",
      scheduledAtUtc: "2026-08-20T17:00:00.000Z",
      committedAt: new Date("2026-08-14T14:18:00.000Z"),
    })
    expect(scheduled.title).toBe("Campaign scheduled")
    expect(scheduled.rows[0]?.value).toBe("Scheduled")
  })
})
