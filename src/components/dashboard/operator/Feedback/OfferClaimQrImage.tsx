import { useEffect, useState } from "react"
import QRCode from "qrcode"

import { cn } from "@/lib/utils"

type OfferClaimQrImageProps = {
  /** Plain Offer Claim code (or preview sample code). */
  claimCode: string
  className?: string
}

/**
 * Guest-facing Offer claim QR — encodes the plain Claim/sample code for Staff Redeem scan.
 */
export function OfferClaimQrImage({
  claimCode,
  className,
}: OfferClaimQrImageProps) {
  const [dataUrl, setDataUrl] = useState<string | null>(null)
  const trimmed = claimCode.trim()
  const sizePx = 129

  useEffect(() => {
    if (trimmed.length === 0) {
      setDataUrl(null)
      return
    }

    let cancelled = false
    void QRCode.toDataURL(trimmed, {
      errorCorrectionLevel: "Q",
      margin: 1,
      width: sizePx,
      color: {
        dark: "#000000",
        light: "#ffffff",
      },
    }).then((url) => {
      if (!cancelled) {
        setDataUrl(url)
      }
    })

    return () => {
      cancelled = true
    }
  }, [trimmed])

  if (trimmed.length === 0 || dataUrl == null) {
    return null
  }

  return (
    <img
      data-guest-preview-offer-qr="1"
      src={dataUrl}
      alt=""
      width={sizePx}
      height={sizePx}
      className={cn("block size-[129px] object-contain", className)}
    />
  )
}
