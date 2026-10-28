import { afterEach, describe, expect, it } from "vitest"

import {
  clearDemoTourSession,
  clearGuestSubmission,
  DEMO_TOUR_SESSION_KEY,
  isDemoTourActive,
  readDemoTourSession,
  readGuestSubmission,
  startDemoTourSession,
  writeDemoTourSession,
  writeGuestSubmission,
} from "@/lib/productDemo/demoTourSession"

describe("demoTourSession", () => {
  afterEach(() => {
    clearDemoTourSession()
    clearGuestSubmission()
  })

  it("starts entry or welcome phase with chapter/step progress", () => {
    expect(isDemoTourActive()).toBe(false)
    startDemoTourSession({ entryPath: "skip", phase: "entry" })
    expect(isDemoTourActive()).toBe(true)
    expect(readDemoTourSession()).toMatchObject({
      chapterIndex: 0,
      stepIndex: 0,
      exploreOpen: false,
      phase: "entry",
      entryPath: "skip",
      hasGuestSubmission: false,
    })
    startDemoTourSession({ entryPath: "skip", phase: "welcome" })
    expect(readDemoTourSession()?.phase).toBe("welcome")
    writeDemoTourSession({
      chapterIndex: 1,
      stepIndex: 2,
      completedChapterIds: ["feedback"],
      exploreOpen: true,
      phase: "tour",
      entryPath: "guest",
      hasGuestSubmission: true,
    })
    expect(readDemoTourSession()?.phase).toBe("tour")
    expect(readDemoTourSession()?.completedChapterIds).toEqual(["feedback"])
    clearDemoTourSession()
    expect(isDemoTourActive()).toBe(false)
  })

  it("drops a stored free-explore flag so no free-explore mode can resume", () => {
    sessionStorage.setItem(
      DEMO_TOUR_SESSION_KEY,
      JSON.stringify({
        chapterIndex: 0,
        stepIndex: 0,
        completedChapterIds: [],
        exploreOpen: false,
        freeExplore: true,
        phase: "tour",
        entryPath: "skip",
        hasGuestSubmission: false,
      })
    )
    const state = readDemoTourSession()
    expect(state?.phase).toBe("tour")
    expect(state).not.toHaveProperty("freeExplore")
  })

  it("stores guest submission for restaurant welcome copy", () => {
    writeGuestSubmission({
      commentPreview: "Sample comment",
      submittedAt: "2026-10-16T12:00:00.000Z",
    })
    expect(readGuestSubmission()?.commentPreview).toBe("Sample comment")
  })
})
