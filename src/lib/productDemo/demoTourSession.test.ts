import { afterEach, describe, expect, it } from "vitest"

import {
  clearDemoTourSession,
  clearGuestSubmission,
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
      freeExplore: false,
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
      freeExplore: true,
      phase: "tour",
      entryPath: "guest",
      hasGuestSubmission: true,
    })
    expect(readDemoTourSession()?.phase).toBe("tour")
    expect(readDemoTourSession()?.freeExplore).toBe(true)
    expect(readDemoTourSession()?.completedChapterIds).toEqual(["feedback"])
    clearDemoTourSession()
    expect(isDemoTourActive()).toBe(false)
  })

  it("stores guest submission for restaurant welcome copy", () => {
    writeGuestSubmission({
      commentPreview: "Sample comment",
      submittedAt: "2026-10-16T12:00:00.000Z",
    })
    expect(readGuestSubmission()?.commentPreview).toBe("Sample comment")
  })
})
