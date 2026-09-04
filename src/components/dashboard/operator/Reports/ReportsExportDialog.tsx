import { toast } from "sonner"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogClose,
} from "@/components/ui/dialog"
import { Checkbox } from "@/components/ui/checkbox"
import { Button } from "@/components/ui/button"
import { X } from "lucide-react"
import { cn } from "@/lib/utils"
import type { ReportsExportKind } from "@/lib/operatorReports/createOperatorReportsPageModule"

type ExportItem = {
  kind: ReportsExportKind
  title: string
  description: string
  buttonLabel: "Download PDF" | "Download CSV"
  format: "pdf" | "csv"
}

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
    description: "Download QR performance by placement and source.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "feedback",
    title: "Feedback report",
    description:
      "Download feedback aggregates and by-source rows for the selected period.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  {
    kind: "campaigns",
    title: "Campaign report",
    description:
      "Download campaign performance — name, goal, channel, sent and status.",
    buttonLabel: "Download CSV",
    format: "csv",
  },
  // Offer redemption log + guest consent stay out of Reports export
  // pack (lock 09 / 10) — Offers / Privacy own those surfaces.
]

type ReportsExportDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  locationName?: string
  dateRangeLabel?: string
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
  locationName = "Location",
  dateRangeLabel = "Last 7 days",
  pendingCsvExportKind,
  csvConsentChecked,
  exportDownloadBusyKind,
  exportDownloadError,
  onRequestExport,
  onSetCsvConsentChecked,
  onConfirmCsvExport,
  onCancelCsvConsent,
}: ReportsExportDialogProps) {
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
          className="z-[200] w-full max-w-[1000px] sm:max-w-[1000px] md:max-w-[1000px] gap-4 rounded-op-lg border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden"
          overlayClassName="z-[190] bg-black/75 backdrop-blur-xs"
        >
          <div className="flex items-start justify-between">
            <DialogHeader className="text-left gap-0.5">
              <DialogTitle className="text-xl font-bold tracking-tight text-op-text-primary">
                Exports
              </DialogTitle>
              <DialogDescription className="text-xs font-normal text-op-text-muted">
                Download reports for {locationName} · {dateRangeLabel}.
              </DialogDescription>
            </DialogHeader>

            <DialogClose asChild>
              <Button
                type="button"
                variant="op-secondary"
                className="size-7 rounded-op-sm border border-op-border-default bg-op-button-collapse-background hover:bg-op-button-collapse-hover text-op-text-muted hover:text-op-text-primary flex items-center justify-center transition-colors shrink-0 p-0"
                aria-label="Close"
              >
                <X className="size-3.5" />
              </Button>
            </DialogClose>
          </div>

          {exportDownloadError != null ? (
            <p className="text-xs text-destructive" role="alert">
              {exportDownloadError}
            </p>
          ) : null}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            {EXPORT_ITEMS.map((item) => {
              const busy = exportDownloadBusyKind === item.kind
              return (
                <div
                  key={item.kind}
                  className="flex flex-col justify-between gap-4 rounded-op-md border border-op-border-default bg-op-background-primary px-5 py-4 shadow-sm"
                >
                  <div className="flex flex-col gap-1">
                    <h3 className="text-sm font-semibold text-op-text-primary">
                      {item.title}
                    </h3>
                    <p className="text-xs text-op-text-muted font-normal leading-normal whitespace-normal">
                      {item.description}
                    </p>
                  </div>

                  <div>
                    <Button
                      type="button"
                      variant="op-secondary"
                      disabled={exportDownloadBusyKind != null}
                      className="h-7.5 px-3 rounded-op-sm text-xs font-medium self-start border border-op-border-default"
                      onClick={() => void handleItemClick(item)}
                    >
                      {busy ? "Downloading…" : item.buttonLabel}
                    </Button>
                  </div>
                </div>
              )
            })}
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
          className="z-[220] w-full max-w-[500px] gap-5 rounded-op-lg border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
          overlayClassName="z-[210] bg-black/80 backdrop-blur-xs"
        >
          <div className="flex items-start justify-between">
            <DialogHeader className="text-left gap-1">
              <DialogTitle className="text-xl font-bold tracking-tight text-op-text-primary">
                Export report?
              </DialogTitle>
              <DialogDescription className="text-xs sm:text-[13px] font-normal text-op-text-muted leading-relaxed">
                This export may include guest data. Only download it if you are
                authorised to use it for this restaurant.
              </DialogDescription>
            </DialogHeader>

            <DialogClose asChild>
              <Button
                type="button"
                variant="op-secondary"
                className="size-7 rounded-op-sm border border-op-border-default bg-op-button-collapse-background hover:bg-op-button-collapse-hover text-op-text-muted hover:text-op-text-primary flex items-center justify-center transition-colors shrink-0 p-0"
                aria-label="Close"
                onClick={onCancelCsvConsent}
              >
                <X className="size-3.5" />
              </Button>
            </DialogClose>
          </div>

          <label className="flex items-center gap-3 cursor-pointer py-3 select-none">
            <Checkbox
              checked={csvConsentChecked}
              onCheckedChange={(checked) =>
                onSetCsvConsentChecked(Boolean(checked))
              }
              className="size-4 rounded-op-sm border-op-checkbox-border data-[state=checked]:bg-op-action-primary data-[state=checked]:border-op-action-primary"
            />
            <span className="text-xs text-op-text-secondary font-normal leading-relaxed">
              I understand this export may contain guest data and should be
              handled securely.
            </span>
          </label>

          <div className="flex items-center gap-2.5 pt-1">
            <Button
              type="button"
              variant={csvConsentChecked ? "op-primary" : "op-secondary"}
              disabled={
                !csvConsentChecked || exportDownloadBusyKind != null
              }
              className={cn(
                "h-9 px-4 rounded-op-sm text-xs font-medium transition-colors",
                !csvConsentChecked
                  && "opacity-50 cursor-not-allowed border border-op-border-default"
              )}
              onClick={() => void handleConfirmExport()}
            >
              {exportDownloadBusyKind != null
                ? "Downloading…"
                : "Download export"}
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              className="h-9 px-4 rounded-op-sm text-xs font-medium border border-op-border-default"
              onClick={onCancelCsvConsent}
            >
              Cancel
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
