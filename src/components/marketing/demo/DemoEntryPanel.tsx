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
      width: 220,
      margin: 0,
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
      className="relative z-10 flex w-full max-w-[648px] flex-col items-center gap-10 overflow-clip rounded-[10px] bg-white px-5 py-10 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-[100px]"
    >
      <MarketingLogo onLight width={144} height={36} className="h-9 w-auto" />

      <div className="flex w-full flex-col items-center gap-11">
        <div className="flex flex-col items-center gap-[22px] text-center text-[#141414]">
          <h1
            id="demo-entry-title"
            className="font-jakarta max-w-[330px] text-[26px] leading-[1.26] font-semibold sm:text-[30px]"
          >
            {copy.headline}
          </h1>
          <p className="font-sans max-w-[380px] text-base leading-5">{copy.body}</p>
        </div>

        <div className="flex w-full max-w-[376px] flex-col items-stretch gap-2">
          {/* Desktop: QR primary. On a phone the browser form is the path. */}
          <div className="hidden flex-col items-center gap-6 rounded-[12px] bg-[#ebebeb] p-[30px] sm:flex">
            <p className="font-sans text-lg leading-5 font-bold text-[#141414]">
              {copy.qrLabel}
            </p>
            <div className="rounded-[15px] bg-[#141414] px-[16.5px] py-[17.4px]">
              <div
                className={cn(
                  "flex h-[119px] w-[118px] items-center justify-center overflow-clip rounded-[9.5px] bg-white",
                  loading && "animate-pulse bg-[#f3f3f3]"
                )}
              >
                {qrDataUrl ? (
                  <DemoMediaImage
                    src={qrDataUrl}
                    alt="Scan to open the Mavero Kitchen guest form on your phone"
                    width={110}
                    height={110}
                    className="size-[110px]"
                  />
                ) : null}
              </div>
            </div>
            <p className="font-sans max-w-[226px] text-center text-sm leading-4 font-medium text-[#141414]">
              {copy.qrCaption}
            </p>
          </div>

          <div className="flex flex-col items-center justify-center gap-3 rounded-[10px] bg-[#ebebeb] px-[18px] py-4 min-[400px]:flex-row">
            <p className="font-sans text-center text-sm leading-[18px] font-medium text-[#141414] min-[400px]:w-[164px] min-[400px]:text-left">
              {copy.browserFormLabel}
            </p>
            <MarketingButton
              type="button"
              variant="secondary"
              className="h-[42px] w-[164px] shrink-0"
              disabled={starting || loading || !scanAbsoluteUrl}
              onClick={onOpenGuestForm}
            >
              {copy.openGuestFormLabel}
            </MarketingButton>
          </div>
        </div>

        <div className="flex w-full flex-col items-center gap-2.5">
          <MarketingButton
            type="button"
            variant="primary"
            className="h-[50px] w-full max-w-[376px]"
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
        </div>
      </div>

      <div className="flex w-full flex-col items-center gap-4 text-center">
        {copy.reassuranceLines.map((line) => (
          <p key={line} className="font-sans text-sm text-[#141414]">
            {line}
          </p>
        ))}
      </div>
    </div>
  )
}
