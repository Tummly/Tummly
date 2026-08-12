import { ScanIcon, XIcon } from "lucide-react"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { StaffRedeemSnapshot } from "@/lib/operatorOffers/createStaffRedeemModule"
import {
  STAFF_REDEEM_CODE_FIELD_CLASS,
  STAFF_REDEEM_CONTENT_CLASS,
  STAFF_REDEEM_COPY,
  STAFF_REDEEM_DIVIDER_CLASS,
  STAFF_REDEEM_ERROR_CLASS,
  STAFF_REDEEM_INSTRUCTION_BODY_CLASS,
  STAFF_REDEEM_INSTRUCTION_TITLE_CLASS,
  STAFF_REDEEM_LABEL_CLASS,
  STAFF_REDEEM_ROW_LABEL_CLASS,
  STAFF_REDEEM_ROW_VALUE_CLASS,
  STAFF_REDEEM_SUBTITLE_CLASS,
  STAFF_REDEEM_TITLE_CLASS,
} from "@/lib/operatorOffers/staffRedeemPresentation"

type StaffRedeemDialogProps = {
  snapshot: StaffRedeemSnapshot
  onOpenChange: (open: boolean) => void
  onCodeChange: (code: string) => void
  onCheckOffer: () => Promise<void>
  onCancelConfirm: () => void
  onMarkAsRedeemed: () => Promise<"redeemed" | "failed" | "noop">
  /** QR fill + Check — Details and Main share this path. */
  onApplyScannedCode: (code: string) => Promise<void>
}

const CONFIRM_ROWS = [
  { key: "offerTitle", label: STAFF_REDEEM_COPY.offerLabel },
  { key: "guestName", label: STAFF_REDEEM_COPY.guestLabel },
  { key: "validAt", label: STAFF_REDEEM_COPY.validAtLabel },
  { key: "expires", label: STAFF_REDEEM_COPY.expiresLabel },
  { key: "usage", label: STAFF_REDEEM_COPY.usageLabel },
] as const

/**
 * Best-effort camera QR read. Denied / unsupported / fail → stay on enter-code;
 * staff type the code manually (ticket 05).
 */
async function tryScanOfferCode(): Promise<string | null> {
  if (typeof window === "undefined" || !("BarcodeDetector" in window)) {
    return null
  }

  let stream: MediaStream | null = null
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: "environment" },
    })
    const Detector = (
      window as unknown as {
        BarcodeDetector: new (options?: {
          formats?: string[]
        }) => {
          detect: (source: ImageBitmapSource) => Promise<Array<{ rawValue: string }>>
        }
      }
    ).BarcodeDetector
    const detector = new Detector({ formats: ["qr_code"] })
    const video = document.createElement("video")
    video.srcObject = stream
    video.playsInline = true
    await video.play()

    const deadline = Date.now() + 8_000
    while (Date.now() < deadline) {
      const codes = await detector.detect(video)
      const value = codes[0]?.rawValue?.trim()
      if (value) {
        return value
      }
      await new Promise((resolve) => {
        window.setTimeout(resolve, 200)
      })
    }
    return null
  } catch {
    return null
  } finally {
    stream?.getTracks().forEach((track) => {
      track.stop()
    })
  }
}

/** Staff Redeem — Figma enter 3527:56860 / confirm 3527:57426. */
export function StaffRedeemDialog({
  snapshot,
  onOpenChange,
  onCodeChange,
  onCheckOffer,
  onCancelConfirm,
  onMarkAsRedeemed,
  onApplyScannedCode,
}: StaffRedeemDialogProps) {
  const copy = STAFF_REDEEM_COPY
  const busy = snapshot.checkBusy || snapshot.redeemBusy
  const isConfirm =
    snapshot.step === "confirm" && snapshot.confirmPreview != null
  const subtitle = isConfirm ? copy.confirmSubtitle : copy.enterSubtitle

  return (
    <Dialog
      open={snapshot.open}
      onOpenChange={(open) => {
        if (busy) {
          return
        }
        onOpenChange(open)
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={STAFF_REDEEM_CONTENT_CLASS}
      >
        <div className="flex flex-col gap-[30px]">
          <div className="flex items-start gap-[22px]">
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className={STAFF_REDEEM_TITLE_CLASS}>
                {copy.title}
              </DialogTitle>
              <DialogDescription className={STAFF_REDEEM_SUBTITLE_CLASS}>
                {subtitle}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                disabled={busy}
                className="shrink-0"
                aria-label={copy.closeAriaLabel}
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DialogClose>
          </div>

          {!isConfirm ? (
            <div className="flex flex-col gap-2">
              <Label htmlFor="staff-redeem-code" className={STAFF_REDEEM_LABEL_CLASS}>
                {copy.codeLabel}
              </Label>
              <Input
                id="staff-redeem-code"
                value={snapshot.code}
                disabled={busy}
                placeholder={copy.codePlaceholder}
                aria-invalid={snapshot.checkError != null}
                aria-describedby={
                  snapshot.checkError != null ? "staff-redeem-code-error" : undefined
                }
                className={STAFF_REDEEM_CODE_FIELD_CLASS}
                onChange={(event) => {
                  onCodeChange(event.target.value)
                }}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault()
                    void onCheckOffer()
                  }
                }}
              />
              {snapshot.checkError != null ? (
                <p
                  id="staff-redeem-code-error"
                  role="alert"
                  className={STAFF_REDEEM_ERROR_CLASS}
                >
                  {snapshot.checkError}
                </p>
              ) : null}
            </div>
          ) : (
            <div className="flex flex-col gap-10">
              <dl className="m-0 flex flex-col gap-5">
                {CONFIRM_ROWS.map((row, index) => (
                  <div key={row.key} className="flex flex-col gap-5">
                    <div className="flex items-center justify-between gap-4">
                      <dt className={STAFF_REDEEM_ROW_LABEL_CLASS}>{row.label}</dt>
                      <dd className={`m-0 ${STAFF_REDEEM_ROW_VALUE_CLASS}`}>
                        {snapshot.confirmPreview?.[row.key]}
                      </dd>
                    </div>
                    {index < CONFIRM_ROWS.length - 1 ? (
                      <div className={STAFF_REDEEM_DIVIDER_CLASS} aria-hidden />
                    ) : null}
                  </div>
                ))}
              </dl>

              <div className="flex flex-col gap-1.5">
                <p className={STAFF_REDEEM_INSTRUCTION_TITLE_CLASS}>
                  {copy.staffInstructionLabel}
                </p>
                <p className={STAFF_REDEEM_INSTRUCTION_BODY_CLASS}>
                  {snapshot.confirmPreview?.staffInstruction}
                </p>
              </div>

              {snapshot.checkError != null ? (
                <p role="alert" className={STAFF_REDEEM_ERROR_CLASS}>
                  {snapshot.checkError}
                </p>
              ) : null}
            </div>
          )}
        </div>

        <DialogFooter className="flex-row flex-wrap gap-3 sm:justify-start">
          {!isConfirm ? (
            <>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={() => {
                  void onCheckOffer()
                }}
              >
                {copy.checkOffer}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={busy}
                aria-label={copy.scanAriaLabel}
                onClick={() => {
                  void (async () => {
                    const scanned = await tryScanOfferCode()
                    if (scanned == null) {
                      return
                    }
                    await onApplyScannedCode(scanned)
                  })()
                }}
              >
                <ScanIcon className="size-4" aria-hidden />
                {copy.scan}
              </Button>
            </>
          ) : (
            <>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={() => {
                  void (async () => {
                    const result = await onMarkAsRedeemed()
                    if (result === "redeemed") {
                      toast.success(copy.successToast)
                    }
                  })()
                }}
              >
                {copy.markAsRedeemed}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={busy}
                onClick={() => {
                  onCancelConfirm()
                }}
              >
                {copy.cancel}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
