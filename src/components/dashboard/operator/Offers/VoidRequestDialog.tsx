import { XIcon } from "lucide-react"
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
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import type {
  VoidOutcomeActionResult,
  VoidRequestSnapshot,
  VoidSendResult,
} from "@/lib/operatorOffers/createVoidRequestModule"
import type { VoidPassSummary } from "@/lib/operatorOffers/voidRequestAdapters"
import {
  VOID_REQUEST_CONTENT_CLASS,
  VOID_REQUEST_COPY,
  VOID_REQUEST_CORRECTION_CARD_CLASS,
  VOID_REQUEST_CORRECTION_HELPER_CLASS,
  VOID_REQUEST_CORRECTION_OPTIONS,
  VOID_REQUEST_CORRECTION_TITLE_CLASS,
  VOID_REQUEST_DIVIDER_CLASS,
  VOID_REQUEST_ERROR_CLASS,
  VOID_REQUEST_FIELD_CLASS,
  VOID_REQUEST_LABEL_CLASS,
  VOID_REQUEST_REASON_OPTIONS,
  VOID_REQUEST_ROW_LABEL_CLASS,
  VOID_REQUEST_ROW_VALUE_CLASS,
  VOID_REQUEST_SECTION_TITLE_CLASS,
  VOID_REQUEST_SELECT_CONTENT_CLASS,
  VOID_REQUEST_SUBTITLE_CLASS,
  VOID_REQUEST_TEXTAREA_CLASS,
  VOID_REQUEST_TITLE_CLASS,
  type VoidRequestCorrectionId,
  type VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"

type VoidRequestDialogProps = {
  snapshot: VoidRequestSnapshot
  onOpenChange: (open: boolean) => void
  onReasonChange: (reasonId: VoidRequestReasonId) => void
  onExplanationChange: (explanation: string) => void
  onCorrectionChange: (correctionId: VoidRequestCorrectionId) => void
  onSendRequest: () => Promise<VoidSendResult>
  onRequestApprove: () => void
  onRequestReject: () => void
  onConfirmApprove: () => Promise<VoidOutcomeActionResult>
  onConfirmReject: () => Promise<VoidOutcomeActionResult>
  onGoBack: () => void
}

const PASS_SUMMARY_ROWS = [
  { key: "offerTitle", label: VOID_REQUEST_COPY.offerLabel },
  { key: "guestName", label: VOID_REQUEST_COPY.guestLabel },
  { key: "passCodeMasked", label: VOID_REQUEST_COPY.passCodeLabel },
  { key: "currentStateText", label: VOID_REQUEST_COPY.currentStateLabel },
  { key: "expiresText", label: VOID_REQUEST_COPY.expiresLabel },
  { key: "locationName", label: VOID_REQUEST_COPY.locationLabel },
  { key: "linkedCampaignText", label: VOID_REQUEST_COPY.linkedCampaignLabel },
] as const

function PassSummaryRows({ summary }: { summary: VoidPassSummary }) {
  return (
    <dl className="m-0 flex flex-col gap-4">
      {PASS_SUMMARY_ROWS.map((row) => (
        <div key={row.key} className="flex items-center justify-between gap-4">
          <dt className={VOID_REQUEST_ROW_LABEL_CLASS}>{row.label}</dt>
          <dd className={`m-0 ${VOID_REQUEST_ROW_VALUE_CLASS}`}>
            {summary[row.key]}
          </dd>
        </div>
      ))}
    </dl>
  )
}

function titleForStep(step: VoidRequestSnapshot["step"]): string {
  switch (step) {
    case "create":
      return VOID_REQUEST_COPY.createTitle
    case "review":
      return VOID_REQUEST_COPY.reviewTitle
    case "confirm-approve":
      return VOID_REQUEST_COPY.approveConfirmTitle
    case "confirm-reject":
      return VOID_REQUEST_COPY.rejectConfirmTitle
    default:
      return VOID_REQUEST_COPY.createTitle
  }
}

function subtitleForStep(step: VoidRequestSnapshot["step"]): string {
  switch (step) {
    case "create":
      return VOID_REQUEST_COPY.createSubtitle
    case "review":
      return VOID_REQUEST_COPY.reviewSubtitle
    case "confirm-approve":
      return VOID_REQUEST_COPY.approveConfirmSubtitle
    case "confirm-reject":
      return VOID_REQUEST_COPY.rejectConfirmSubtitle
    default:
      return ""
  }
}

/** Void request — Figma create / review / approve / reject confirms. */
export function VoidRequestDialog({
  snapshot,
  onOpenChange,
  onReasonChange,
  onExplanationChange,
  onCorrectionChange,
  onSendRequest,
  onRequestApprove,
  onRequestReject,
  onConfirmApprove,
  onConfirmReject,
  onGoBack,
}: VoidRequestDialogProps) {
  const copy = VOID_REQUEST_COPY
  const busy = snapshot.busy
  const step = snapshot.step
  const isCreate = step === "create"
  const isReview = step === "review"
  const isConfirmApprove = step === "confirm-approve"
  const isConfirmReject = step === "confirm-reject"
  const isConfirm = isConfirmApprove || isConfirmReject
  const passSummary =
    snapshot.createPreview
    ?? snapshot.reviewDetail
    ?? null

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
        className={VOID_REQUEST_CONTENT_CLASS}
      >
        <div className="flex flex-col gap-[30px]">
          <div className="flex items-start gap-[22px]">
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className={VOID_REQUEST_TITLE_CLASS}>
                {titleForStep(step)}
              </DialogTitle>
              <DialogDescription className={VOID_REQUEST_SUBTITLE_CLASS}>
                {subtitleForStep(step)}
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

          {isCreate && passSummary != null ? (
            <div className="flex flex-col gap-8">
              <PassSummaryRows summary={passSummary} />
              <div className={VOID_REQUEST_DIVIDER_CLASS} aria-hidden />
              <div className="flex flex-col gap-2">
                <Label
                  htmlFor="void-request-reason"
                  className={VOID_REQUEST_LABEL_CLASS}
                >
                  {copy.reasonForRequestLabel}
                </Label>
                <Select
                  value={snapshot.reasonId ?? undefined}
                  disabled={busy}
                  onValueChange={(value) => {
                    onReasonChange(value as VoidRequestReasonId)
                  }}
                >
                  <SelectTrigger
                    id="void-request-reason"
                    className={VOID_REQUEST_FIELD_CLASS}
                  >
                    <SelectValue placeholder={copy.reasonPlaceholder} />
                  </SelectTrigger>
                  <SelectContent
                    position="popper"
                    align="start"
                    className={VOID_REQUEST_SELECT_CONTENT_CLASS}
                  >
                    <SelectGroup>
                      {VOID_REQUEST_REASON_OPTIONS.map((option) => (
                        <SelectItem key={option.id} value={option.id}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-2">
                <Label
                  htmlFor="void-request-explanation"
                  className={VOID_REQUEST_LABEL_CLASS}
                >
                  {copy.explanationLabel}
                </Label>
                <Textarea
                  id="void-request-explanation"
                  value={snapshot.explanation}
                  disabled={busy}
                  placeholder={copy.explanationPlaceholder}
                  className={VOID_REQUEST_TEXTAREA_CLASS}
                  onChange={(event) => {
                    onExplanationChange(event.target.value)
                  }}
                />
              </div>
              <div className={VOID_REQUEST_DIVIDER_CLASS} aria-hidden />
              <div className="flex flex-col gap-[18px]">
                <p className={VOID_REQUEST_SECTION_TITLE_CLASS}>
                  {copy.requestedCorrectionLabel}
                </p>
                <div className="flex flex-col gap-3" role="radiogroup">
                  {VOID_REQUEST_CORRECTION_OPTIONS.map((option) => {
                    const selected = snapshot.correctionId === option.id
                    return (
                      <button
                        key={option.id}
                        type="button"
                        role="radio"
                        aria-checked={selected}
                        data-selected={selected ? "true" : "false"}
                        disabled={busy}
                        className={VOID_REQUEST_CORRECTION_CARD_CLASS}
                        onClick={() => {
                          onCorrectionChange(option.id)
                        }}
                      >
                        <span className={VOID_REQUEST_CORRECTION_TITLE_CLASS}>
                          {option.title}
                        </span>
                        <span className={VOID_REQUEST_CORRECTION_HELPER_CLASS}>
                          {option.helper}
                        </span>
                      </button>
                    )
                  })}
                </div>
              </div>
              {snapshot.formError != null ? (
                <p role="alert" className={VOID_REQUEST_ERROR_CLASS}>
                  {snapshot.formError}
                </p>
              ) : null}
            </div>
          ) : null}

          {isReview && snapshot.reviewDetail != null ? (
            <div className="flex flex-col gap-8">
              <dl className="m-0 flex flex-col gap-4">
                {(
                  [
                    {
                      label: copy.requestedByLabel,
                      value: snapshot.reviewDetail.requestedByText,
                    },
                    {
                      label: copy.requestedAtLabel,
                      value: snapshot.reviewDetail.requestedAtText,
                    },
                    {
                      label: copy.reasonLabel,
                      value: snapshot.reviewDetail.reasonText,
                    },
                    {
                      label: copy.explanationLabel,
                      value: snapshot.reviewDetail.explanation ?? "—",
                    },
                    {
                      label: copy.requestedCorrectionLabel,
                      value: snapshot.reviewDetail.correctionText,
                    },
                  ] as const
                ).map((row) => (
                  <div
                    key={row.label}
                    className="flex items-center justify-between gap-4"
                  >
                    <dt className={VOID_REQUEST_ROW_LABEL_CLASS}>{row.label}</dt>
                    <dd className={`m-0 ${VOID_REQUEST_ROW_VALUE_CLASS}`}>
                      {row.value}
                    </dd>
                  </div>
                ))}
              </dl>
              <div className={VOID_REQUEST_DIVIDER_CLASS} aria-hidden />
              <PassSummaryRows summary={snapshot.reviewDetail} />
              {snapshot.formError != null ? (
                <p role="alert" className={VOID_REQUEST_ERROR_CLASS}>
                  {snapshot.formError}
                </p>
              ) : null}
            </div>
          ) : null}

          {isConfirm && snapshot.formError != null ? (
            <p role="alert" className={VOID_REQUEST_ERROR_CLASS}>
              {snapshot.formError}
            </p>
          ) : null}
        </div>

        <DialogFooter className="flex-row flex-wrap gap-3 sm:justify-start">
          {isCreate ? (
            <>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={() => {
                  void (async () => {
                    const result = await onSendRequest()
                    if (result === "sent") {
                      toast.success(copy.successCreateToast)
                    }
                  })()
                }}
              >
                {copy.sendRequest}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={busy}
                onClick={() => {
                  onOpenChange(false)
                }}
              >
                {copy.cancel}
              </Button>
            </>
          ) : null}

          {isReview ? (
            <>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={() => {
                  onRequestApprove()
                }}
              >
                {copy.approveRequest}
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={busy}
                onClick={() => {
                  onRequestReject()
                }}
              >
                {copy.rejectRequest}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={busy}
                onClick={() => {
                  onOpenChange(false)
                }}
              >
                {copy.cancel}
              </Button>
            </>
          ) : null}

          {isConfirmApprove ? (
            <>
              <Button
                type="button"
                variant="op-primary"
                disabled={busy}
                onClick={() => {
                  void (async () => {
                    const result = await onConfirmApprove()
                    if (result === "approved") {
                      toast.success(copy.successApproveToast)
                    }
                  })()
                }}
              >
                {copy.approveRequest}
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={busy}
                onClick={() => {
                  onGoBack()
                }}
              >
                {copy.goBack}
              </Button>
            </>
          ) : null}

          {isConfirmReject ? (
            <>
              <Button
                type="button"
                variant="destructive-solid"
                disabled={busy}
                onClick={() => {
                  void (async () => {
                    const result = await onConfirmReject()
                    if (result === "rejected") {
                      toast.success(copy.successRejectToast)
                    }
                  })()
                }}
              >
                {copy.rejectRequest}
              </Button>
              <Button
                type="button"
                variant="op-secondary"
                disabled={busy}
                onClick={() => {
                  onGoBack()
                }}
              >
                {copy.goBack}
              </Button>
            </>
          ) : null}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
