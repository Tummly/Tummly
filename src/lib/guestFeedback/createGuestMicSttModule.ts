export const COMMENT_MAX_LENGTH = 1000
export const DEFAULT_MAX_RECORDING_MS = 60_000

export const GUEST_MIC_ERROR_COPY = {
  permission:
    "Microphone access was denied. You can still type your feedback.",
  empty_speech:
    "We didn't catch any speech. Try again or type your feedback.",
  stt_failure:
    "We couldn't transcribe that recording. Try again or type your feedback.",
  rate_limit:
    "Too many voice attempts from this link. Try typing instead.",
  truncated: "Your message was shortened to 1000 characters.",
} as const

export type GuestMicChrome = "mic" | "tick_cancel" | "loader"

export type GuestMicErrorKind =
  | "permission"
  | "empty_speech"
  | "stt_failure"
  | "rate_limit"

export type GuestMicSttSnapshot = {
  phase: "idle" | "recording" | "transcribing"
  chrome: GuestMicChrome
  messageLocked: boolean
  submitLocked: boolean
  micAvailable: boolean
  error: { kind: GuestMicErrorKind; message: string } | null
  truncateNotice: string | null
}

export type GuestSttResult =
  | { ok: true; text: string }
  | { ok: false; reason: "empty_speech" | "stt_failure" | "rate_limit" }

export type GuestMicSttAdapters = {
  startRecording: () => Promise<void>
  stopRecording: () => Promise<Blob>
  cancelRecording: () => Promise<void>
  transcribe: (audio: Blob) => Promise<GuestSttResult>
  replaceComment: (text: string) => void
}

export type MicSttErrorCopy = {
  permission: string
  empty_speech: string
  stt_failure: string
  rate_limit: string
  truncated: string
}

export type GuestMicSttOptions = {
  maxDurationMs?: number
  maxCommentLength?: number
  /** How long mic stays disabled after a rate-limit response. */
  rateLimitWindowMs?: number
  schedule?: (fn: () => void, ms: number) => () => void
  errorCopy?: MicSttErrorCopy
}

export type GuestMicSttModule = {
  getSnapshot: () => GuestMicSttSnapshot
  subscribe: (listener: () => void) => () => void
  start: () => Promise<void>
  confirm: () => Promise<void>
  cancel: () => Promise<void>
  dismissError: () => void
  dismissTruncateNotice: () => void
  reset: () => void
}

type MicState = GuestMicSttSnapshot & {
  generation: number
}

function chromeForPhase(
  phase: GuestMicSttSnapshot["phase"]
): GuestMicChrome {
  if (phase === "recording") {
    return "tick_cancel"
  }
  if (phase === "transcribing") {
    return "loader"
  }
  return "mic"
}

function toSnapshot(state: MicState): GuestMicSttSnapshot {
  const locked = state.phase === "recording" || state.phase === "transcribing"
  return {
    phase: state.phase,
    chrome: chromeForPhase(state.phase),
    messageLocked: locked,
    submitLocked: locked,
    micAvailable: state.micAvailable,
    error: state.error,
    truncateNotice: state.truncateNotice,
  }
}

function isPermissionError(error: unknown): boolean {
  if (error instanceof DOMException) {
    return (
      error.name === "NotAllowedError" ||
      error.name === "PermissionDeniedError"
    )
  }
  return false
}

export function createInMemoryGuestMicSttAdapters(
  options: {
    transcript?: string
    failReason?: Exclude<GuestSttResult, { ok: true }>["reason"]
  } = {}
): GuestMicSttAdapters {
  let recording = false

  return {
    startRecording: async () => {
      recording = true
    },
    stopRecording: async () => {
      recording = false
      return new Blob(["audio"], { type: "audio/webm" })
    },
    cancelRecording: async () => {
      recording = false
    },
    transcribe: async () => {
      if (options.failReason) {
        return { ok: false, reason: options.failReason }
      }
      return { ok: true, text: options.transcript ?? "In-memory transcript." }
    },
    replaceComment: () => {
      void recording
    },
  }
}

export function createGuestMicSttModule(
  adapters: GuestMicSttAdapters,
  options: GuestMicSttOptions = {}
): GuestMicSttModule {
  const maxDurationMs = options.maxDurationMs ?? DEFAULT_MAX_RECORDING_MS
  const maxCommentLength = options.maxCommentLength ?? COMMENT_MAX_LENGTH
  const rateLimitWindowMs = options.rateLimitWindowMs ?? 60 * 60 * 1000
  const errorCopy = options.errorCopy ?? GUEST_MIC_ERROR_COPY
  const schedule =
    options.schedule ??
    ((fn, ms) => {
      const id = globalThis.setTimeout(fn, ms)
      return () => globalThis.clearTimeout(id)
    })

  let state: MicState = {
    phase: "idle",
    chrome: "mic",
    messageLocked: false,
    submitLocked: false,
    micAvailable: true,
    error: null,
    truncateNotice: null,
    generation: 0,
  }

  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  let cancelAutoStop: (() => void) | null = null
  let cancelRateLimitWindow: (() => void) | null = null

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = toSnapshot(state)
    emit()
  }

  const clearAutoStop = () => {
    cancelAutoStop?.()
    cancelAutoStop = null
  }

  const clearRateLimitWindow = () => {
    cancelRateLimitWindow?.()
    cancelRateLimitWindow = null
  }

  const disableMicForRateLimitWindow = () => {
    clearRateLimitWindow()
    cancelRateLimitWindow = schedule(() => {
      cancelRateLimitWindow = null
      if (!state.micAvailable) {
        state = {
          ...state,
          micAvailable: true,
          error:
            state.error?.kind === "rate_limit" ? null : state.error,
        }
        publish()
      }
    }, rateLimitWindowMs)
  }

  const setIdle = (patch: Partial<MicState> = {}) => {
    state = {
      ...state,
      phase: "idle",
      ...patch,
    }
    publish()
  }

  const applyTranscript = (text: string) => {
    const trimmed = text.trim()
    if (trimmed.length > maxCommentLength) {
      adapters.replaceComment(trimmed.slice(0, maxCommentLength))
      state = {
        ...state,
        truncateNotice: errorCopy.truncated,
        error: null,
      }
      return
    }

    adapters.replaceComment(trimmed)
    state = {
      ...state,
      truncateNotice: null,
      error: null,
    }
  }

  const runConfirm = async (generation: number) => {
    clearAutoStop()
    state = {
      ...state,
      phase: "transcribing",
      error: null,
      truncateNotice: null,
    }
    publish()

    let audio: Blob
    try {
      audio = await adapters.stopRecording()
    } catch {
      if (state.generation !== generation) {
        return
      }
      setIdle({
        error: {
          kind: "stt_failure",
          message: errorCopy.stt_failure,
        },
      })
      return
    }

    if (state.generation !== generation) {
      return
    }

    const result = await adapters.transcribe(audio)

    if (state.generation !== generation) {
      return
    }

    if (result.ok) {
      applyTranscript(result.text)
      setIdle()
      return
    }

    if (result.reason === "rate_limit") {
      disableMicForRateLimitWindow()
      setIdle({
        micAvailable: false,
        error: {
          kind: "rate_limit",
          message: errorCopy.rate_limit,
        },
      })
      return
    }

    setIdle({
      error: {
        kind: result.reason,
        message: errorCopy[result.reason],
      },
    })
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    start: async () => {
      if (!state.micAvailable || state.phase !== "idle") {
        return
      }

      const generation = state.generation + 1
      state = {
        ...state,
        generation,
        phase: "recording",
        error: null,
        truncateNotice: null,
      }
      publish()

      try {
        await adapters.startRecording()
      } catch (error) {
        if (state.generation !== generation) {
          return
        }

        if (isPermissionError(error)) {
          setIdle({
            micAvailable: false,
            error: {
              kind: "permission",
              message: errorCopy.permission,
            },
          })
          return
        }

        setIdle({
          error: {
            kind: "stt_failure",
            message: errorCopy.stt_failure,
          },
        })
        return
      }

      if (state.generation !== generation || state.phase !== "recording") {
        return
      }

      clearAutoStop()
      cancelAutoStop = schedule(() => {
        if (state.generation !== generation || state.phase !== "recording") {
          return
        }
        void runConfirm(generation)
      }, maxDurationMs)
    },
    confirm: async () => {
      if (state.phase !== "recording") {
        return
      }
      await runConfirm(state.generation)
    },
    cancel: async () => {
      if (state.phase !== "recording" && state.phase !== "transcribing") {
        return
      }

      const generation = state.generation + 1
      clearAutoStop()
      state = {
        ...state,
        generation,
      }

      try {
        await adapters.cancelRecording()
      } catch {
        // Still return to idle — guest must keep prior text.
      }

      setIdle({
        error: null,
        truncateNotice: null,
      })
    },
    dismissError: () => {
      if (state.error == null) {
        return
      }
      state = {
        ...state,
        error: null,
      }
      publish()
    },
    dismissTruncateNotice: () => {
      if (state.truncateNotice == null) {
        return
      }
      state = {
        ...state,
        truncateNotice: null,
      }
      publish()
    },
    reset: () => {
      clearAutoStop()
      clearRateLimitWindow()
      state = {
        phase: "idle",
        chrome: "mic",
        messageLocked: false,
        submitLocked: false,
        micAvailable: true,
        error: null,
        truncateNotice: null,
        generation: state.generation + 1,
      }
      publish()
    },
  }
}
