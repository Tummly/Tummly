import type { GuestMicSttAdapters } from "@/lib/guestFeedback/createGuestMicSttModule"

type CaptureState = {
  stream: MediaStream | null
  recorder: MediaRecorder | null
  chunks: BlobPart[]
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

export function createBrowserGuestMicAdapters(
  options: {
    transcribe: (audio: Blob) => Promise<
      Awaited<ReturnType<GuestMicSttAdapters["transcribe"]>>
    >
    replaceComment: (text: string) => void
  }
): GuestMicSttAdapters {
  const capture: CaptureState = {
    stream: null,
    recorder: null,
    chunks: [],
  }

  const resetCapture = () => {
    stopTracks(capture.stream)
    capture.stream = null
    capture.recorder = null
    capture.chunks = []
  }

  return {
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
}
