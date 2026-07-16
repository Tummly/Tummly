import { describe, expect, it, vi } from "vitest"

import {
  COMMENT_MAX_LENGTH,
  createGuestMicSttModule,
  createInMemoryGuestMicSttAdapters,
  GUEST_MIC_ERROR_COPY,
  type GuestMicSttAdapters,
  type GuestSttResult,
} from "./createGuestMicSttModule"

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe("createGuestMicSttModule", () => {
  it("starts idle with mic chrome and unlocked message/submit", () => {
    const mic = createGuestMicSttModule(createInMemoryGuestMicSttAdapters())

    expect(mic.getSnapshot()).toEqual({
      phase: "idle",
      chrome: "mic",
      messageLocked: false,
      submitLocked: false,
      micAvailable: true,
      error: null,
      truncateNotice: null,
    })
  })

  it("locks message and submit while recording; confirm replaces comment", async () => {
    const replaceComment = vi.fn()
    const stopRecording = vi.fn(async () => new Blob(["audio"], { type: "audio/webm" }))
    const transcribe = vi.fn(async (): Promise<GuestSttResult> => ({
      ok: true,
      text: "Service was excellent.",
    }))

    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      stopRecording,
      transcribe,
      replaceComment,
    }
    const mic = createGuestMicSttModule(adapters)

    const startPromise = mic.start()
    expect(mic.getSnapshot()).toMatchObject({
      phase: "recording",
      chrome: "tick_cancel",
      messageLocked: true,
      submitLocked: true,
      micAvailable: true,
      error: null,
    })
    await startPromise

    const confirmPromise = mic.confirm()
    expect(mic.getSnapshot()).toMatchObject({
      phase: "transcribing",
      chrome: "loader",
      messageLocked: true,
      submitLocked: true,
    })
    await confirmPromise

    expect(stopRecording).toHaveBeenCalledOnce()
    expect(transcribe).toHaveBeenCalledOnce()
    expect(replaceComment).toHaveBeenCalledWith("Service was excellent.")
    expect(mic.getSnapshot()).toMatchObject({
      phase: "idle",
      chrome: "mic",
      messageLocked: false,
      submitLocked: false,
      truncateNotice: null,
      error: null,
    })
  })

  it("cancel leaves prior text intact and returns to idle mic", async () => {
    const replaceComment = vi.fn()
    const cancelRecording = vi.fn(async () => undefined)
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      cancelRecording,
      replaceComment,
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()
    await mic.cancel()

    expect(cancelRecording).toHaveBeenCalledOnce()
    expect(replaceComment).not.toHaveBeenCalled()
    expect(mic.getSnapshot()).toMatchObject({
      phase: "idle",
      chrome: "mic",
      messageLocked: false,
      submitLocked: false,
      error: null,
    })
  })

  it("permission denied disables mic with distinct inline error", async () => {
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      startRecording: async () => {
        throw new DOMException("Permission denied", "NotAllowedError")
      },
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()

    expect(mic.getSnapshot()).toMatchObject({
      phase: "idle",
      chrome: "mic",
      messageLocked: false,
      submitLocked: false,
      micAvailable: false,
      error: {
        kind: "permission",
        message: GUEST_MIC_ERROR_COPY.permission,
      },
    })
  })

  it("STT failure keeps prior text and restores mic", async () => {
    const replaceComment = vi.fn()
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      replaceComment,
      transcribe: async () => ({ ok: false, reason: "stt_failure" }),
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()
    await mic.confirm()

    expect(replaceComment).not.toHaveBeenCalled()
    expect(mic.getSnapshot()).toMatchObject({
      phase: "idle",
      chrome: "mic",
      micAvailable: true,
      messageLocked: false,
      submitLocked: false,
      error: {
        kind: "stt_failure",
        message: GUEST_MIC_ERROR_COPY.stt_failure,
      },
    })
  })

  it("empty speech uses distinct copy and does not replace text", async () => {
    const replaceComment = vi.fn()
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      replaceComment,
      transcribe: async () => ({ ok: false, reason: "empty_speech" }),
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()
    await mic.confirm()

    expect(replaceComment).not.toHaveBeenCalled()
    expect(mic.getSnapshot()).toMatchObject({
      error: {
        kind: "empty_speech",
        message: GUEST_MIC_ERROR_COPY.empty_speech,
      },
      micAvailable: true,
    })
  })

  it("rate limit disables mic for the window with try-typing copy", async () => {
    const timers: Array<{ fn: () => void; ms: number }> = []
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      transcribe: async () => ({ ok: false, reason: "rate_limit" }),
    }
    const mic = createGuestMicSttModule(adapters, {
      rateLimitWindowMs: 3_600_000,
      schedule: (fn, ms) => {
        timers.push({ fn, ms })
        return () => undefined
      },
    })

    await mic.start()
    await mic.confirm()

    expect(mic.getSnapshot()).toMatchObject({
      phase: "idle",
      micAvailable: false,
      error: {
        kind: "rate_limit",
        message: GUEST_MIC_ERROR_COPY.rate_limit,
      },
    })
    expect(timers.some((timer) => timer.ms === 3_600_000)).toBe(true)

    timers.find((timer) => timer.ms === 3_600_000)?.fn()
    expect(mic.getSnapshot()).toMatchObject({
      micAvailable: true,
      error: null,
    })
  })

  it("truncates transcripts over 1000 chars and shows shortened notice", async () => {
    const longText = "a".repeat(COMMENT_MAX_LENGTH + 40)
    const replaceComment = vi.fn()
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      replaceComment,
      transcribe: async () => ({ ok: true, text: longText }),
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()
    await mic.confirm()

    expect(replaceComment).toHaveBeenCalledWith(
      "a".repeat(COMMENT_MAX_LENGTH)
    )
    expect(mic.getSnapshot()).toMatchObject({
      truncateNotice: GUEST_MIC_ERROR_COPY.truncated,
      phase: "idle",
      error: null,
    })
  })

  it("auto-stop at 60s behaves as Tick", async () => {
    const timers: Array<{ fn: () => void; ms: number }> = []
    const replaceComment = vi.fn()
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      replaceComment,
      transcribe: async () => ({ ok: true, text: "Auto stopped transcript." }),
    }
    const mic = createGuestMicSttModule(adapters, {
      maxDurationMs: 60_000,
      schedule: (fn, ms) => {
        timers.push({ fn, ms })
        return () => undefined
      },
    })

    await mic.start()
    expect(timers).toHaveLength(1)
    expect(timers[0]?.ms).toBe(60_000)

    timers[0]?.fn()
    await vi.waitFor(() => {
      expect(replaceComment).toHaveBeenCalledWith("Auto stopped transcript.")
    })
    expect(mic.getSnapshot().phase).toBe("idle")
  })

  it("does not replace comment when cancel races ahead of a pending confirm", async () => {
    const stopGate = deferred<Blob>()
    const replaceComment = vi.fn()
    const adapters: GuestMicSttAdapters = {
      ...createInMemoryGuestMicSttAdapters(),
      stopRecording: () => stopGate.promise,
      cancelRecording: async () => undefined,
      replaceComment,
      transcribe: async () => ({ ok: true, text: "Should not land." }),
    }
    const mic = createGuestMicSttModule(adapters)

    await mic.start()
    const confirmPromise = mic.confirm()
    await mic.cancel()
    stopGate.resolve(new Blob(["late"], { type: "audio/webm" }))
    await confirmPromise

    expect(replaceComment).not.toHaveBeenCalled()
    expect(mic.getSnapshot().phase).toBe("idle")
  })
})
