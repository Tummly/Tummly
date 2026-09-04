import { toast } from "sonner"
import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Spinner } from "@/components/ui/spinner"
import {
  FEEDBACK_DIALOG_BODY_CLASS,
  FEEDBACK_DIALOG_CONTENT_CLASS,
  FEEDBACK_DIALOG_DESCRIPTION_CLASS,
  FEEDBACK_DIALOG_FOOTER_CLASS,
  FEEDBACK_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type { ReportsExportKind } from "@/lib/operatorReports/createOperatorReportsPageModule"

type ExportItem = {
  kind: ReportsExportKind
  title: string
  description: string
  buttonLabel: "Download PDF" | "Download CSV"
  format: "pdf" | "csv"
}

/** Export picker — Figma `3674:36723`. */
const EXPORT_ITEMS: ExportItem[] = [
  {
    kind: "overview",
    title: "Overview summary",
    description:
      "Download a PDF summary of guest capture, feedback, offers and campaigns.",
    buttonLabel: "Download PDF",
    format: "pdf",
  },
  {
    kind: "capture",
    title: "Capture report",
    description:
      "Download QR performance by location, placement and source.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "feedback",
    title: "Feedback report",
    description:
      "Download private feedback records for the selected period.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "campaigns",
    title: "Campaign report",
    description:
      "Download campaign sends, claims, redemptions and opt-outs.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "offers-redemptions",
    title: "Offer redemption log",
    description:
      "Download offer claims, redemptions, expired offers and invalid attempts.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "guest-consent",
    title: "Guest consent export",
    description: "Download guest contact and consent records.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
]

/** Wide shell for the 2-column export grid — same chrome as Feedback dialogs. */
const EXPORTS_DIALOG_CONTENT_CLASS =
  "max-h-[min(90vh,900px)] gap-0 overflow-hidden border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-lg sm:max-w-[1000px] dark:bg-[var(--op-color-gray-1000)]"

const EXPORTS_DIALOG_BODY_CLASS =
  "flex min-h-0 flex-1 flex-col gap-5 overflow-y-auto px-8 pt-5 pb-8"

const EXPORT_CARD_CLASS =
  "flex flex-col justify-between gap-6 rounded-op-lg border border-op-border-default bg-op-card-background p-6"

const EXPORT_CARD_TITLE_CLASS =
  "m-0 text-lg font-semibold leading-normal text-op-text-primary"

const EXPORT_CARD_DESCRIPTION_CLASS =
  "m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]"

type ReportsExportDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** When false, hide the Offer redemption log CSV card. Omit/true shows it. */
  showOfferRedemptionLog?: boolean
  /** When false, hide the Guest consent CSV card. Omit/true shows it. */
  showGuestConsent?: boolean
  pendingCsvExportKind: ReportsExportKind | null
  csvConsentChecked: boolean
  exportDownloadBusyKind: ReportsExportKind | null
  exportDownloadError: string | null
  onRequestExport: (kind: ReportsExportKind) => Promise<boolean>
  onSetCsvConsentChecked: (checked: boolean) => void
  onConfirmCsvExport: () => Promise<boolean>
  onCancelCsvConsent: () => void
}

export function ReportsExportDialog({
  open,
  onOpenChange,
  showOfferRedemptionLog = true,
  showGuestConsent = true,
  pendingCsvExportKind,
  csvConsentChecked,
  exportDownloadBusyKind,
  exportDownloadError,
  onRequestExport,
  onSetCsvConsentChecked,
  onConfirmCsvExport,
  onCancelCsvConsent,
}: ReportsExportDialogProps) {
  const visibleItems = EXPORT_ITEMS.filter((item) => {
    if (item.kind === "offers-redemptions") {
      return showOfferRedemptionLog
    }
    if (item.kind === "guest-consent") {
      return showGuestConsent
    }
    return true
  })

  const downloadBusy = exportDownloadBusyKind != null

  const handleItemClick = async (item: ExportItem) => {
    const ok = await onRequestExport(item.kind)
    if (item.format === "pdf" && ok) {
      toast.success("Your file has been downloaded")
    }
  }

  const handleConfirmExport = async () => {
    const ok = await onConfirmCsvExport()
    if (ok) {
      toast.success("Your file has been downloaded")
    }
  }

  return (
    <>
      <Dialog
        open={open && pendingCsvExportKind == null}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            onCancelCsvConsent()
          }
          onOpenChange(nextOpen)
        }}
      >
        <DialogContent
          showCloseButton={false}
          className={EXPORTS_DIALOG_CONTENT_CLASS}
        >
          <div className={FEEDBACK_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                Exports
              </DialogTitle>
              <DialogDescription className="max-w-none text-base font-medium leading-normal text-[var(--op-color-gray-550)] dark:text-[var(--op-color-gray-550)]">
                Download reports and consent-safe data for your records.
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                aria-label="Close"
                className="shrink-0"
                disabled={downloadBusy}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className={EXPORTS_DIALOG_BODY_CLASS}>
            {exportDownloadError != null ? (
              <p
                className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
                role="alert"
              >
                {exportDownloadError}
              </p>
            ) : null}

            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              {visibleItems.map((item) => {
                const busy = exportDownloadBusyKind === item.kind
                return (
                  <div key={item.kind} className={EXPORT_CARD_CLASS}>
                    <div className="flex flex-col gap-2">
                      <h3 className={EXPORT_CARD_TITLE_CLASS}>{item.title}</h3>
                      <p className={EXPORT_CARD_DESCRIPTION_CLASS}>
                        {item.description}
                      </p>
                    </div>

                    <div>
                      <Button
                        type="button"
                        variant="op-tertiary"
                        disabled={downloadBusy}
                        onClick={() => void handleItemClick(item)}
                      >
                        {busy ? (
                          <>
                            <Spinner size="sm" data-icon="inline-start" />
                            Downloading…
                          </>
                        ) : (
                          item.buttonLabel
                        )}
                      </Button>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog
        open={pendingCsvExportKind != null}
        onOpenChange={(isOpen) => {
          if (!isOpen) onCancelCsvConsent()
        }}
      >
        <DialogContent
          showCloseButton={false}
          className={FEEDBACK_DIALOG_CONTENT_CLASS}
        >
          <div className={FEEDBACK_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                Export report?
              </DialogTitle>
              <DialogDescription className={FEEDBACK_DIALOG_DESCRIPTION_CLASS}>
                This export may include guest data. Only download it if you are
                authorised to use it for this restaurant.
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                aria-label="Close"
                className="shrink-0"
                disabled={downloadBusy}
                onClick={onCancelCsvConsent}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className={FEEDBACK_DIALOG_BODY_CLASS}>
            <CheckboxLabel
              checked={csvConsentChecked}
              disabled={downloadBusy}
              onCheckedChange={onSetCsvConsentChecked}
              labelClassName="text-op-text-primary dark:text-op-text-primary"
            >
              I understand this export may contain guest data and should be
              handled securely.
            </CheckboxLabel>
          </div>

          <DialogFooter className={FEEDBACK_DIALOG_FOOTER_CLASS}>
            <Button
              type="button"
              variant="op-primary"
              disabled={!csvConsentChecked || downloadBusy}
              aria-disabled={!csvConsentChecked || downloadBusy}
              onClick={() => void handleConfirmExport()}
            >
              {downloadBusy ? (
                <>
                  <Spinner size="sm" data-icon="inline-start" />
                  Downloading…
                </>
              ) : (
                "Download export"
              )}
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              disabled={downloadBusy}
              onClick={onCancelCsvConsent}
            >
              Cancel
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
