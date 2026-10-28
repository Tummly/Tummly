import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import QRCode from "qrcode"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { DemoMediaImage } from "@/components/marketing/demo/DemoMediaImage"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import { cn } from "@/lib/utils"

type DemoEntryPanelProps = {
  scanAbsoluteUrl: string | null
  loading: boolean
  error: string | null
  unavailable: boolean
  onOpenGuestForm: () => void
  onRestaurantSkip: () => void
  onRestart: () => void
  starting: boolean
}

/** §3 public demo entry — QR / browser guest form / restaurant skip. */
export function DemoEntryPanel({
  scanAbsoluteUrl,
  loading,
  error,
  unavailable,
  onOpenGuestForm,
  onRestaurantSkip,
  onRestart,
  starting,
}: DemoEntryPanelProps) {
  const copy = PRODUCT_DEMO.entry
  const [qrDataUrl, setQrDataUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!scanAbsoluteUrl) {
      setQrDataUrl(null)
      return
    }
    let cancelled = false
    void QRCode.toDataURL(scanAbsoluteUrl, {
      width: 160,
      margin: 1,
      color: { dark: "#141414", light: "#ffffff" },
    }).then((url) => {
      if (!cancelled) setQrDataUrl(url)
    })
    return () => {
      cancelled = true
    }
  }, [scanAbsoluteUrl])

  if (unavailable) {
    return (
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="demo-entry-unavailable-title"
        className="relative z-10 flex w-full max-w-[440px] flex-col items-center gap-6 overflow-clip rounded-[10px] bg-white px-6 py-8 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-10"
      >
        <MarketingLogo onLight width={120} height={30} className="h-[30px] w-auto" />
        <div className="flex flex-col items-center gap-2 text-center">
          <h1
            id="demo-entry-unavailable-title"
            className="font-jakarta text-[22px] font-semibold text-[#141414] sm:text-[26px]"
          >
            {copy.unavailableTitle}
          </h1>
          <p className="font-sans text-base text-[#4e4e4e]">{copy.unavailableBody}</p>
        </div>
        <div className="flex w-full flex-col gap-3 sm:flex-row">
          <MarketingButton
            type="button"
            variant="primary"
            showArrow={false}
            className="w-full"
            asChild
          >
            <Link to={PRODUCT_DEMO.exit.primaryTo}>{PRODUCT_DEMO.exit.primaryLabel}</Link>
          </MarketingButton>
          <MarketingButton
            type="button"
            variant="secondary"
            showArrow={false}
            className="w-full"
            asChild
          >
            <Link to={PRODUCT_DEMO.exit.secondaryTo}>
              {PRODUCT_DEMO.exit.secondaryLabel}
            </Link>
          </MarketingButton>
        </div>
        <button
          type="button"
          onClick={onRestart}
          className="font-sans text-sm text-[#4e4e4e] underline-offset-2 hover:underline"
        >
          {copy.restartLabel}
        </button>
      </div>
    )
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="demo-entry-title"
      className="relative z-10 flex w-full max-w-[440px] flex-col items-center gap-5 overflow-clip rounded-[10px] bg-white px-5 py-7 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-8 sm:py-8"
    >
      <MarketingLogo onLight width={120} height={30} className="h-[30px] w-auto" />

      <div className="flex w-full flex-col items-center gap-2 text-center">
        <p className="font-sans text-[11px] font-medium tracking-[0.08em] text-[#7c7c7c] uppercase">
          Guest view · Demo
        </p>
        <h1
          id="demo-entry-title"
          className="font-jakarta text-[22px] leading-tight font-semibold text-[#141414] sm:text-[26px]"
        >
          {copy.headline}
        </h1>
        <p className="font-sans max-w-[360px] text-sm leading-5 text-[#4e4e4e]">
          {copy.body}
        </p>
      </div>

      {/* Desktop: QR primary */}
      <div className="hidden w-full flex-col items-center gap-2.5 sm:flex">
        <div
          className={cn(
            "flex size-[160px] items-center justify-center rounded-lg border border-[#ebebeb] bg-white",
            loading && "animate-pulse bg-[#f3f3f3]"
          )}
        >
          {qrDataUrl ? (
            <DemoMediaImage
              src={qrDataUrl}
              alt="Scan to open the Mavero Kitchen guest form on your phone"
              width={160}
              height={160}
              className="size-[160px]"
            />
          ) : null}
        </div>
        <p className="font-sans text-sm font-medium text-[#141414]">{copy.qrLabel}</p>
      </div>

      <div className="flex w-full flex-col items-center gap-2.5">
        <MarketingButton
          type="button"
          variant="primary"
          showArrow={false}
          className="w-full max-w-[300px] min-h-10"
          disabled={starting || loading || !scanAbsoluteUrl}
          onClick={onOpenGuestForm}
        >
          {copy.openGuestFormLabel}
        </MarketingButton>
        <MarketingButton
          type="button"
          variant="secondary"
          showArrow={false}
          className="w-full max-w-[300px] min-h-10"
          disabled={starting}
          onClick={onRestaurantSkip}
        >
          {copy.restaurantSkipLabel}
        </MarketingButton>
        {starting ? (
          <p className="font-sans text-sm text-[#4e4e4e]">Starting demo…</p>
        ) : null}
        {error ? (
          <p role="alert" className="font-sans max-w-md text-center text-sm text-[#b42318]">
            {error}
          </p>
        ) : null}
        <p className="font-sans text-xs text-[#7c7c7c]">{copy.reassurance}</p>
      </div>
    </div>
  )
}
