import guestLoopLiveHero from "@/assets/operator-home/guest-loop-live-hero.png"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type OperatorHomeHeroProps = {
  canPreviewGuestForm: boolean
  canDownloadQr: boolean
  previewBusy?: boolean
  downloadBusy?: boolean
  onPreviewGuestForm?: () => void
  onDownloadQr?: () => void
}

const primaryButtonClassName =
  "h-auto min-h-0 rounded-lg border-transparent bg-primary px-[17px] py-[11px] text-sm font-medium leading-5 text-primary-foreground hover:bg-primary/90 disabled:opacity-50"

const secondaryButtonClassName =
  "h-auto min-h-0 rounded-lg border border-foreground bg-transparent px-[17px] py-[11px] text-sm font-medium leading-5 text-foreground hover:bg-black/5 dark:hover:bg-white/10 disabled:opacity-50"

/** Figma “Your Guest Loop is live” hero — Preview + Download QR. */
export function OperatorHomeHero({
  canPreviewGuestForm,
  canDownloadQr,
  previewBusy = false,
  downloadBusy = false,
  onPreviewGuestForm,
  onDownloadQr,
}: OperatorHomeHeroProps) {
  return (
    <section
      className={cn(
        "flex flex-col items-stretch overflow-hidden rounded-lg bg-[#eee] px-6 py-8 sm:flex-row sm:items-center sm:gap-6 sm:px-[34px]",
        "dark:bg-white/8"
      )}
    >
      <div className="flex min-w-0 flex-1 flex-col items-start gap-[26px]">
        <div className="flex flex-col gap-4">
          <h2 className="text-[32px] leading-10 font-bold text-[#1b1b1b] dark:text-white">
            Your Guest Loop is live
          </h2>
          <p className="max-w-[555px] text-sm leading-6 text-[#1b1b1b] dark:text-white/80">
            Your QR materials are activated. Place them where guests order,
            collect or receive deliveries to start capturing feedback and
            consented guest details.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            className={primaryButtonClassName}
            disabled={!canPreviewGuestForm || previewBusy}
            aria-disabled={!canPreviewGuestForm || previewBusy}
            onClick={onPreviewGuestForm}
          >
            Preview guest form
          </Button>
          <Button
            type="button"
            variant="outline"
            className={secondaryButtonClassName}
            disabled={!canDownloadQr || downloadBusy}
            aria-disabled={!canDownloadQr || downloadBusy}
            onClick={onDownloadQr}
          >
            Download QR
          </Button>
        </div>
      </div>

      <div className="mt-6 hidden shrink-0 sm:mt-0 sm:block sm:w-[280px] lg:w-[333px]">
        <img
          src={guestLoopLiveHero}
          alt=""
          className="h-auto w-full object-contain"
        />
      </div>
    </section>
  )
}
