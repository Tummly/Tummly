import { useState } from "react"
import { X } from "lucide-react"
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
import { triggerBrowserDownload } from "@/lib/operatorHome/homeActions"
import { cn } from "@/lib/utils"

type ExportItem = {
  id: string
  title: string
  description: string
  buttonLabel: "Download PDF" | "Download CSV"
  format: "pdf" | "csv"
  filenamePrefix: string
}

const EXPORT_ITEMS: ExportItem[] = [
  {
    id: "overview-summary",
    title: "Overview summary",
    description:
      "Download a PDF summary of guest capture, feedback, offers and campaigns.",
    buttonLabel: "Download PDF",
    format: "pdf",
    filenamePrefix: "overview-summary",
  },
  {
    id: "capture-report",
    title: "Capture report",
    description: "Download QR performance by location, placement and source.",
    buttonLabel: "Download CSV",
    format: "csv",
    filenamePrefix: "capture-report",
  },
  {
    id: "feedback-report",
    title: "Feedback report",
    description: "Download private feedback records for the selected period.",
    buttonLabel: "Download CSV",
    format: "csv",
    filenamePrefix: "feedback-report",
  },
  {
    id: "campaign-report",
    title: "Campaign report",
    description:
      "Download campaign sends, claims, redemptions and opt-outs.",
    buttonLabel: "Download CSV",
    format: "csv",
    filenamePrefix: "campaign-report",
  },
  // Offer redemption log + guest consent stay out of Reports export
  // pack (lock 09 / 10) — Offers / Privacy own those surfaces.
]

type ReportsExportDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  locationName?: string
  dateRangeLabel?: string
  reportType?: string
}

export function ReportsExportDialog({
  open,
  onOpenChange,
  locationName = "Mehmet's Grill",
  dateRangeLabel = "Last 7 days",
}: ReportsExportDialogProps) {
  const [pendingExportItem, setPendingExportItem] = useState<ExportItem | null>(null)
  const [consentChecked, setConsentChecked] = useState(false)

  const executeDownload = (item: ExportItem) => {
    const dateStr = new Date().toISOString().split("T")[0]
    const locSlug = locationName.toLowerCase().replace(/[^a-z0-9]/g, "-")
    const filename = `${item.filenamePrefix}-${locSlug}-${dateStr}.${item.format}`

    let content = ""
    let mimeType = "text/plain"

    if (item.format === "csv") {
      mimeType = "text/csv;charset=utf-8;"
      if (item.id === "capture-report") {
        content = [
          "Source,Scans,Feedback,Contactable,Conversion Rate",
          "Delivery insert,72,18,11,25%",
          "Counter card,45,12,7,26%",
          "Receipt QR,33,15,5,45%",
          "Table card,88,22,9,25%",
        ].join("\n")
      } else if (item.id === "feedback-report") {
        content = [
          "Date,Guest,Rating,Category,Source,Follow Up",
          "12 Jul,Sarah,5/5,Food,Delivery insert,Resolved",
          "11 Jul,Ahmed,4/5,Service,Counter card,Resolved",
          "10 Jul,Elena,2/5,Wait time,Receipt QR,Action needed",
        ].join("\n")
      } else if (item.id === "campaign-report") {
        content = [
          "Campaign,Goal,Channel,Sent,Claims,Redemptions,Unsubscribes,Status",
          "Quiet Tuesday offer,Quiet-day boost,SMS,9,41,38,2,Sent",
          "Weekend Flash Deal,Saturday special,Email,15,60,45,5,Scheduled",
          "Midweek Motivation,Wednesday surprise,SMS,12,55,50,3,Sent",
        ].join("\n")
      } else {
        content = [
          "Metric,Value,Period",
          `Location,${locationName},${dateRangeLabel}`,
          "Active Offers,4,Last 7 days",
          "Offer Claims,44,Last 7 days",
          "Redemptions,203,Last 7 days",
        ].join("\n")
      }
    } else {
      mimeType = "text/html"
      content = `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>${item.title} - ${locationName}</title>
  <style>
    body { font-family: sans-serif; padding: 2rem; color: #222; }
    h1 { margin-bottom: 0.5rem; }
    p { color: #666; margin-top: 0; }
  </style>
</head>
<body>
  <h1>${item.title}</h1>
  <p>Location: ${locationName} | Period: ${dateRangeLabel} | Exported: ${dateStr}</p>
  <p>Summary of guest capture, feedback, offers and campaigns.</p>
</body>
</html>`
    }

    const blob = new Blob([content], { type: mimeType })
    triggerBrowserDownload(blob, filename)
    toast.success("Your file has been downloaded")
    setPendingExportItem(null)
    onOpenChange(false)
  }

  const handleItemClick = (item: ExportItem) => {
    if (item.format === "csv") {
      setConsentChecked(false)
      setPendingExportItem(item)
    } else {
      executeDownload(item)
    }
  }

  const handleConfirmExport = () => {
    if (pendingExportItem) {
      executeDownload(pendingExportItem)
    }
  }

  return (
    <>
      {/* 1. Main Exports Dialog (hidden while confirmation dialog is active) */}
      <Dialog
        open={open && !pendingExportItem}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            setPendingExportItem(null)
          }
          onOpenChange(nextOpen)
        }}
      >
        <DialogContent
          showCloseButton={false}
          className="z-[200] w-full max-w-[1000px] sm:max-w-[1000px] md:max-w-[1000px] gap-4 rounded-op-lg border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden"
          overlayClassName="z-[190] bg-black/75 backdrop-blur-xs"
        >
          {/* Modal Header Row */}
          <div className="flex items-start justify-between">
            <DialogHeader className="text-left gap-0.5">
              <DialogTitle className="text-xl font-bold tracking-tight text-op-text-primary">
                Exports
              </DialogTitle>
              <DialogDescription className="text-xs font-normal text-op-text-muted">
                Download reports and consent-safe data for your records.
              </DialogDescription>
            </DialogHeader>

            <DialogClose asChild>
              <button
                type="button"
                className="size-7 rounded-op-sm border border-op-border-default bg-op-button-collapse-background hover:bg-op-button-collapse-hover text-op-text-muted hover:text-op-text-primary flex items-center justify-center transition-colors shrink-0"
                aria-label="Close"
              >
                <X className="size-3.5" />
              </button>
            </DialogClose>
          </div>

          {/* 6 Export Cards Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            {EXPORT_ITEMS.map((item) => (
              <div
                key={item.id}
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
                    className="h-7.5 px-3 rounded-op-sm text-xs font-medium self-start border border-op-border-default"
                    onClick={() => handleItemClick(item)}
                  >
                    {item.buttonLabel}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      {/* 2. Confirmation Dialog for CSV Export (Higher Z-Index to come on top) */}
      <Dialog
        open={Boolean(pendingExportItem)}
        onOpenChange={(isOpen) => {
          if (!isOpen) setPendingExportItem(null)
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
                This export may include guest data. Only download it if you are authorised to use it for this restaurant.
              </DialogDescription>
            </DialogHeader>

            <DialogClose asChild>
              <button
                type="button"
                className="size-7 rounded-op-sm border border-op-border-default bg-op-button-collapse-background hover:bg-op-button-collapse-hover text-op-text-muted hover:text-op-text-primary flex items-center justify-center transition-colors shrink-0"
                aria-label="Close"
              >
                <X className="size-3.5" />
              </button>
            </DialogClose>
          </div>

          <label className="flex items-center gap-3 cursor-pointer py-3 select-none">
            <Checkbox
              checked={consentChecked}
              onCheckedChange={(checked) => setConsentChecked(Boolean(checked))}
              className="size-4 rounded-op-sm border-op-checkbox-border data-[state=checked]:bg-op-action-primary data-[state=checked]:border-op-action-primary"
            />
            <span className="text-xs text-op-text-secondary font-normal leading-relaxed">
              I understand this export may contain guest data and should be handled securely.
            </span>
          </label>

          <div className="flex items-center gap-2.5 pt-1">
            <Button
              type="button"
              variant={consentChecked ? "op-primary" : "op-secondary"}
              disabled={!consentChecked}
              className={cn(
                "h-9 px-4 rounded-op-sm text-xs font-medium transition-colors",
                !consentChecked && "opacity-50 cursor-not-allowed border border-op-border-default"
              )}
              onClick={handleConfirmExport}
            >
              Download export
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              className="h-9 px-4 rounded-op-sm text-xs font-medium border border-op-border-default"
              onClick={() => setPendingExportItem(null)}
            >
              Cancel
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
