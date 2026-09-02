import { useState } from "react"
import { Download, FileText, Table as TableIcon, FileJson } from "lucide-react"
import { toast } from "sonner"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { triggerBrowserDownload } from "@/lib/operatorHome/homeActions"
import { cn } from "@/lib/utils"

type ExportFormat = "csv" | "pdf" | "json"

type ReportsExportDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  locationName?: string
  dateRangeLabel?: string
}

export function ReportsExportDialog({
  open,
  onOpenChange,
  locationName = "Mehmet's Grill",
  dateRangeLabel = "Last 7 days",
}: ReportsExportDialogProps) {
  const [format, setFormat] = useState<ExportFormat>("csv")
  const [isExporting, setIsExporting] = useState(false)

  const handleExport = () => {
    setIsExporting(true)
    setTimeout(() => {
      setIsExporting(false)
      const dateStr = new Date().toISOString().split("T")[0]
      const filename = `reports-${locationName.toLowerCase().replace(/[^a-z0-9]/g, "-")}-${dateStr}.${format}`

      let content = ""
      let mimeType = "text/plain"

      if (format === "csv") {
        mimeType = "text/csv;charset=utf-8;"
        content = [
          "Metric,Value,Period",
          `Location,${locationName},${dateRangeLabel}`,
          "QR Scans,158,Last 7 days",
          "Feedback Received,42,Last 7 days",
          "Contactable Guests,28,Last 7 days",
          "Offer Redemptions,12,Last 7 days",
          "Campaign Activity,3,Last 7 days",
          "Active Offers,3,Last 7 days",
          "Offer Claims,38,Last 7 days",
          "Campaigns Sent,2,Last 7 days",
        ].join("\n")
      } else if (format === "json") {
        mimeType = "application/json"
        content = JSON.stringify(
          {
            location: locationName,
            period: dateRangeLabel,
            exportedAt: new Date().toISOString(),
            funnel: {
              scans: 158,
              feedbackReceived: 42,
              contactableGuests: 28,
              offerRedemptions: 12,
              campaignActivity: 3,
            },
            privateFeedback: {
              total: 42,
              contactable: 28,
              followUpNeeded: 6,
              followedUp: 36,
            },
            captureSources: [
              { source: "Delivery insert", scans: 72, feedback: 18, contactable: 11 },
              { source: "Counter card", scans: 45, feedback: 12, contactable: 7 },
              { source: "Receipt QR", scans: 33, feedback: 15, contactable: 5 },
              { source: "Table card", scans: 88, feedback: 22, contactable: 9 },
            ],
            offersAndCampaigns: {
              activeOffers: 3,
              offerClaims: 38,
              offerRedemptions: 12,
              campaignsSent: 2,
              unsubscribes: 1,
            },
          },
          null,
          2
        )
      } else {
        mimeType = "text/html"
        content = `<!DOCTYPE html><html><head><title>Report ${locationName}</title></head><body><h1>Report: ${locationName}</h1><p>Period: ${dateRangeLabel}</p></body></html>`
      }

      const blob = new Blob([content], { type: mimeType })
      triggerBrowserDownload(blob, filename)
      toast.success(`Exported ${filename}`)
      onOpenChange(false)
    }, 600)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="z-[200] w-full max-w-md gap-6 rounded-md border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
        overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
      >
        <DialogHeader className="text-left">
          <DialogTitle className="text-lg font-bold text-op-text-primary">
            Export Reports Data
          </DialogTitle>
          <DialogDescription className="text-xs text-op-text-muted">
            Download analytics and performance metrics for {locationName} ({dateRangeLabel}).
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <label className="text-xs font-semibold text-op-text-muted">Select format</label>
            <div className="grid grid-cols-3 gap-2">
              <button
                type="button"
                onClick={() => setFormat("csv")}
                className={cn(
                  "flex flex-col items-center justify-center gap-1.5 rounded-sm border p-3 text-xs font-medium transition-colors",
                  format === "csv"
                    ? "border-op-action-primary bg-op-surface-secondary text-op-text-primary"
                    : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:bg-op-surface-secondary/50"
                )}
              >
                <TableIcon className="size-4 text-op-action-primary" />
                <span>CSV</span>
              </button>

              <button
                type="button"
                onClick={() => setFormat("json")}
                className={cn(
                  "flex flex-col items-center justify-center gap-1.5 rounded-sm border p-3 text-xs font-medium transition-colors",
                  format === "json"
                    ? "border-op-action-primary bg-op-surface-secondary text-op-text-primary"
                    : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:bg-op-surface-secondary/50"
                )}
              >
                <FileJson className="size-4 text-op-action-primary" />
                <span>JSON</span>
              </button>

              <button
                type="button"
                onClick={() => setFormat("pdf")}
                className={cn(
                  "flex flex-col items-center justify-center gap-1.5 rounded-sm border p-3 text-xs font-medium transition-colors",
                  format === "pdf"
                    ? "border-op-action-primary bg-op-surface-secondary text-op-text-primary"
                    : "border-op-border-default bg-op-background-primary text-op-text-secondary hover:bg-op-surface-secondary/50"
                )}
              >
                <FileText className="size-4 text-op-action-primary" />
                <span>PDF (HTML)</span>
              </button>
            </div>
          </div>

          <div className="flex items-center justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="h-9 text-xs"
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="op-primary"
              disabled={isExporting}
              onClick={handleExport}
              className="h-9 text-xs font-medium"
            >
              <Download className="mr-1.5 size-3.5" />
              {isExporting ? "Exporting..." : "Download Export"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
