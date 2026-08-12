import jsQR from "jsqr"

/** Prefer rear camera; fall back to any camera on laptops without one. */
export async function openStaffRedeemCamera(): Promise<MediaStream> {
  if (
    typeof navigator === "undefined"
    || navigator.mediaDevices?.getUserMedia == null
  ) {
    throw new Error("getUserMedia unavailable")
  }

  try {
    return await navigator.mediaDevices.getUserMedia({
      audio: false,
      video: { facingMode: { ideal: "environment" } },
    })
  } catch {
    return await navigator.mediaDevices.getUserMedia({
      audio: false,
      video: true,
    })
  }
}

export function stopStaffRedeemCamera(stream: MediaStream | null) {
  stream?.getTracks().forEach((track) => {
    track.stop()
  })
}

/** Decode one camera frame for the guest offer QR (claim code). */
export async function decodeOfferCodeFromVideo(
  video: HTMLVideoElement,
  canvas: HTMLCanvasElement
): Promise<string | null> {
  if (video.readyState < HTMLMediaElement.HAVE_ENOUGH_DATA) {
    return null
  }

  const width = video.videoWidth
  const height = video.videoHeight
  if (width === 0 || height === 0) {
    return null
  }

  const context = canvas.getContext("2d", { willReadFrequently: true })
  if (context == null) {
    return null
  }

  canvas.width = width
  canvas.height = height
  context.drawImage(video, 0, 0, width, height)
  const imageData = context.getImageData(0, 0, width, height)
  const code = jsQR(imageData.data, imageData.width, imageData.height, {
    inversionAttempts: "dontInvert",
  })
  const value = code?.data?.trim()
  return value && value.length > 0 ? value : null
}
