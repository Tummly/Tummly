import { describe, expect, it } from "vitest"

import {
  campaignScheduledAtUtcIso,
  canContinueCampaignSchedule,
  defaultCampaignScheduleModeId,
} from "@/lib/operatorCampaigns/campaignSchedulePresentation"

describe("campaignSchedulePresentation", () => {
  it("defaults schedule mode to send-now", () => {
    expect(defaultCampaignScheduleModeId()).toBe("send-now")
  })

  it("builds UTC ISO from local date and time", () => {
    const iso = campaignScheduledAtUtcIso({
      dateLocal: "2026-08-20",
      timeLocal: "10:30",
    })
    expect(iso).not.toBeNull()
    expect(new Date(iso!).getFullYear()).toBe(2026)
  })

  it("returns null for incomplete datetime parts", () => {
    expect(
      campaignScheduledAtUtcIso({ dateLocal: "", timeLocal: "10:00" })
    ).toBeNull()
    expect(
      campaignScheduledAtUtcIso({ dateLocal: "2026-08-20", timeLocal: "" })
    ).toBeNull()
  })

  it("allows send-now without datetime", () => {
    expect(
      canContinueCampaignSchedule({
        modeId: "send-now",
        dateLocal: "",
        timeLocal: "",
        now: new Date("2026-08-14T14:18:00"),
      })
    ).toBe(true)
  })

  it("requires schedule-later datetime strictly after now", () => {
    const now = new Date("2026-08-14T14:18:00")
    expect(
      canContinueCampaignSchedule({
        modeId: "schedule-later",
        dateLocal: "2026-08-10",
        timeLocal: "09:00",
        now,
      })
    ).toBe(false)
    expect(
      canContinueCampaignSchedule({
        modeId: "schedule-later",
        dateLocal: "2026-08-20",
        timeLocal: "18:00",
        now,
      })
    ).toBe(true)
  })
})
