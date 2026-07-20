import type { GuestMicSttAdapters } from "@/lib/guestFeedback/createGuestMicSttModule"
import {
  computeRmsLevel,
  GUEST_MIC_LEVEL_GAIN,
  type GuestMicAudioLevelSource,
} from "@/lib/guestFeedback/guestMicAudioLevel"

type CaptureState = {
  stream: MediaStream | null
  recorder: MediaRecorder | null
  chunks: BlobPart[]
}

type AnalyserState = {
  context: AudioContext | null
  analyser: AnalyserNode | null
  timeDomainData: Uint8Array<ArrayBuffer> | null
}

function pickMimeType(): string | undefined {
  if (typeof MediaRecorder === "undefined") {
    return undefined
  }

  const candidates = [
    "audio/webm;codecs=opus",
    "audio/webm",
    "audio/mp4",
    "audio/ogg;codecs=opus",
  ]

  return candidates.find((type) => MediaRecorder.isTypeSupported(type))
}

function stopTracks(stream: MediaStream | null) {
  stream?.getTracks().forEach((track) => track.stop())
}

export type BrowserGuestMic = {
  adapters: GuestMicSttAdapters
  /**
   * Live mic amplitude for the recording waveform. Reads 0 / inactive
   * whenever no recording is in flight or Web Audio is unavailable —
   * recording itself never depends on this.
   */
  audioLevelSource: GuestMicAudioLevelSource
}

export function createBrowserGuestMicAdapters(
  options: {
    transcribe: (audio: Blob) => Promise<
      Awaited<ReturnType<GuestMicSttAdapters["transcribe"]>>
    >
    replaceComment: (text: string) => void
  }
): BrowserGuestMic {
  const capture: CaptureState = {
    stream: null,
    recorder: null,
    chunks: [],
  }

  const level: AnalyserState = {
    context: null,
    analyser: null,
    timeDomainData: null,
  }

  const teardownAnalyser = () => {
    level.analyser = null
    level.timeDomainData = null
    void level.context?.close().catch(() => {
      // Context may already be closed; nothing to recover.
    })
    level.context = null
  }

  const setupAnalyser = (stream: MediaStream) => {
    try {
      const context = new AudioContext()
      const analyser = context.createAnalyser()
      analyser.fftSize = 512
      context.createMediaStreamSource(stream).connect(analyser)

      level.context = context
      level.analyser = analyser
      level.timeDomainData = new Uint8Array(analyser.fftSize)
    } catch {
      // No Web Audio support — the waveform falls back to static bars.
      teardownAnalyser()
    }
  }

  const resetCapture = () => {
    teardownAnalyser()
    stopTracks(capture.stream)
    capture.stream = null
    capture.recorder = null
    capture.chunks = []
  }

  const audioLevelSource: GuestMicAudioLevelSource = {
    getLevel: () => {
      if (!level.analyser || !level.timeDomainData) {
        return 0
      }
      level.analyser.getByteTimeDomainData(level.timeDomainData)
      return Math.min(
        1,
        computeRmsLevel(level.timeDomainData) * GUEST_MIC_LEVEL_GAIN
      )
    },
    isActive: () => level.analyser != null,
  }

  const adapters: GuestMicSttAdapters = {
    startRecording: async () => {
      resetCapture()

      const stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
      })
      const mimeType = pickMimeType()
      const recorder = mimeType
        ? new MediaRecorder(stream, { mimeType })
        : new MediaRecorder(stream)

      capture.stream = stream
      capture.recorder = recorder
      capture.chunks = []

      setupAnalyser(stream)

      recorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          capture.chunks.push(event.data)
        }
      }

      recorder.start()
    },
    stopRecording: async () => {
      const recorder = capture.recorder
      if (!recorder || recorder.state === "inactive") {
        const blob = new Blob(capture.chunks, {
          type: recorder?.mimeType || "audio/webm",
        })
        resetCapture()
        return blob
      }

      const blob = await new Promise<Blob>((resolve, reject) => {
        recorder.onerror = () => {
          reject(new Error("Recording failed."))
        }
        recorder.onstop = () => {
          resolve(
            new Blob(capture.chunks, {
              type: recorder.mimeType || "audio/webm",
            })
          )
        }
        recorder.stop()
      })

      resetCapture()
      return blob
    },
    cancelRecording: async () => {
      const recorder = capture.recorder
      if (recorder && recorder.state !== "inactive") {
        await new Promise<void>((resolve) => {
          recorder.onstop = () => resolve()
          recorder.stop()
        })
      }
      resetCapture()
    },
    transcribe: options.transcribe,
    replaceComment: options.replaceComment,
  }

  return { adapters, audioLevelSource }
}
