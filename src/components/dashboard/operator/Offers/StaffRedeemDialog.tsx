import { ScanIcon, XIcon } from "lucide-react"
import { useEffect, useRef, useState } from "react"
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
import {
  decodeOfferCodeFromVideo,
  openStaffRedeemCamera,
  stopStaffRedeemCamera,
} from "@/lib/operatorOffers/staffRedeemScan"

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
  const [scanning, setScanning] = useState(false)
  const [scanStarting, setScanStarting] = useState(false)
  const videoRef = useRef<HTMLVideoElement | null>(null)
  const canvasRef = useRef<HTMLCanvasElement | null>(null)
  const streamRef = useRef<MediaStream | null>(null)
  const applyScannedCodeRef = useRef(onApplyScannedCode)
  applyScannedCodeRef.current = onApplyScannedCode

  const busy =
    snapshot.checkBusy || snapshot.redeemBusy || scanning || scanStarting
  const isConfirm =
    snapshot.step === "confirm" && snapshot.confirmPreview != null
  const subtitle = isConfirm ? copy.confirmSubtitle : copy.enterSubtitle

  const stopScan = () => {
    stopStaffRedeemCamera(streamRef.current)
    streamRef.current = null
    const video = videoRef.current
    if (video != null) {
      video.srcObject = null
    }
    setScanning(false)
    setScanStarting(false)
  }

  useEffect(() => {
    if (!snapshot.open || isConfirm) {
      stopScan()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only reset when dialogue closes or moves to confirm
  }, [snapshot.open, isConfirm])

  useEffect(() => {
    if (!scanning) {
      return
    }

    let cancelled = false
    let frameId = 0

    const run = async () => {
      setScanStarting(true)
      try {
        const stream = await openStaffRedeemCamera()
        if (cancelled) {
          stopStaffRedeemCamera(stream)
          return
        }
        streamRef.current = stream
        const video = videoRef.current
        if (video == null) {
          stopStaffRedeemCamera(stream)
          setScanning(false)
          toast.message(copy.scanUnavailable)
          return
        }
        video.srcObject = stream
        video.muted = true
        video.playsInline = true
        await video.play()
        if (cancelled) {
          return
        }
        setScanStarting(false)

        const tick = async () => {
          if (cancelled) {
            return
          }
          const canvas = canvasRef.current
          if (video != null && canvas != null) {
            const code = await decodeOfferCodeFromVideo(video, canvas)
            if (cancelled) {
              return
            }
            if (code != null) {
              stopScan()
              await applyScannedCodeRef.current(code)
              return
            }
          }
          frameId = window.setTimeout(() => {
            void tick()
          }, 250)
        }
        void tick()
      } catch {
        if (!cancelled) {
          stopScan()
          toast.message(copy.scanUnavailable)
        }
      }
    }

    void run()

    return () => {
      cancelled = true
      window.clearTimeout(frameId)
      stopStaffRedeemCamera(streamRef.current)
      streamRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- start once per scanning session
  }, [scanning, copy.scanUnavailable])

  return (
    <Dialog
      open={snapshot.open}
      onOpenChange={(open) => {
        if (snapshot.checkBusy || snapshot.redeemBusy) {
          return
        }
        if (!open) {
          stopScan()
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
                {scanning ? copy.scanHint : subtitle}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                disabled={snapshot.checkBusy || snapshot.redeemBusy}
                className="shrink-0"
                aria-label={copy.closeAriaLabel}
                onClick={() => {
                  stopScan()
                }}
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DialogClose>
          </div>

          {!isConfirm ? (
            scanning ? (
              <div className="flex flex-col gap-3">
                <div className="overflow-hidden rounded-op-sm border border-op-border-default bg-black">
                  <video
                    ref={videoRef}
                    className="aspect-video w-full object-cover"
                    muted
                    playsInline
                    autoPlay
                  />
                </div>
                <canvas ref={canvasRef} className="hidden" aria-hidden />
                {scanStarting ? (
                  <p className="m-0 text-sm text-[var(--op-color-gray-550)]">
                    {copy.scanOpening}
                  </p>
                ) : null}
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                <Label
                  htmlFor="staff-redeem-code"
                  className={STAFF_REDEEM_LABEL_CLASS}
                >
                  {copy.codeLabel}
                </Label>
                <Input
                  id="staff-redeem-code"
                  value={snapshot.code}
                  disabled={busy}
                  placeholder={copy.codePlaceholder}
                  aria-invalid={snapshot.checkError != null}
                  aria-describedby={
                    snapshot.checkError != null
                      ? "staff-redeem-code-error"
                      : undefined
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
            )
          ) : (
            <div className="flex flex-col gap-10">
              <dl className="m-0 flex flex-col gap-5">
                {CONFIRM_ROWS.map((row, index) => (
                  <div key={row.key} className="flex flex-col gap-5">
                    <div className="flex items-center justify-between gap-4">
                      <dt className={STAFF_REDEEM_ROW_LABEL_CLASS}>
                        {row.label}
                      </dt>
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
            scanning ? (
              <Button
                type="button"
                variant="op-tertiary"
                onClick={() => {
                  stopScan()
                }}
              >
                {copy.scanCancel}
              </Button>
            ) : (
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
                    setScanning(true)
                  }}
                >
                  <ScanIcon className="size-4" aria-hidden />
                  {copy.scan}
                </Button>
              </>
            )
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
