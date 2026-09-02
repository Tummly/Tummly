import { useEffect, useState } from "react"
import QRCode from "qrcode"
import {
  Check,
  Copy,
  Download,
  FileText,
  Printer,
  QrCode,
  AlertTriangle,
  Layers,
  Pause,
  Play,
  Archive,
} from "lucide-react"
import { toast } from "sonner"

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import type { CaptureReportPlacementRow } from "@/lib/operatorReports/captureReportPresentation"
import { cn } from "@/lib/utils"

export type CaptureReportPlacementActionType =
  | "view-qr"
  | "download-pdf"
  | "pause"
  | "activate"
  | "duplicate"
  | "archive"

export type CaptureReportPlacementActionModalProps = {
  open: boolean
  actionType: CaptureReportPlacementActionType | null
  placement: CaptureReportPlacementRow | null
  locationName?: string
  onOpenChange: (open: boolean) => void
  onConfirmAction?: (
    actionType: CaptureReportPlacementActionType,
    placement: CaptureReportPlacementRow,
    options?: { newName?: string }
  ) => void
}

export function CaptureReportPlacementActionModal({
  open,
  actionType,
  placement,
  locationName = "Mehmet's Grill",
  onOpenChange,
  onConfirmAction,
}: CaptureReportPlacementActionModalProps) {
  const [qrDataUrl, setQrDataUrl] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [duplicateName, setDuplicateName] = useState("")
  const [selectedPdfFormat, setSelectedPdfFormat] = useState<string>("tent")

  // Target guest URL for this QR placement
  const guestUrl =
    typeof window !== "undefined"
      ? `${window.location.origin}/g/${locationName.toLowerCase().replace(/[^a-z0-9]/g, "-")}/${placement?.placement.toLowerCase() ?? "delivery"}`
      : `https://tummly.co.uk/g/mehmets-grill/${placement?.placement.toLowerCase() ?? "delivery"}`

  useEffect(() => {
    if (placement != null) {
      setDuplicateName(`${placement.qrName} (Copy)`)
    }
  }, [placement])

  useEffect(() => {
    if (!open || !placement) {
      setQrDataUrl(null)
      return
    }

    let cancelled = false
    void QRCode.toDataURL(guestUrl, {
      errorCorrectionLevel: "H",
      margin: 1,
      width: 256,
      color: {
        dark: "#000000",
        light: "#ffffff",
      },
    }).then((url) => {
      if (!cancelled) {
        setQrDataUrl(url)
      }
    })

    return () => {
      cancelled = true
    }
  }, [open, placement, guestUrl])

  if (!placement || !actionType) {
    return null
  }

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(guestUrl)
      setCopied(true)
      toast.success("Guest link copied to clipboard")
      setTimeout(() => setCopied(false), 2000)
    } catch {
      toast.error("Could not copy link")
    }
  }

  const handleDownloadPng = () => {
    if (!qrDataUrl) return
    const a = document.createElement("a")
    a.href = qrDataUrl
    a.download = `${placement.qrName.toLowerCase().replace(/[^a-z0-9]/g, "-")}-qr.png`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    toast.success("QR Code PNG downloaded")
  }

  const handleDownloadPdf = () => {
    toast.success(`Printable PDF (${selectedPdfFormat.toUpperCase()}) generated and downloaded`)
    onOpenChange(false)
  }

  const handlePrint = () => {
    window.print()
  }

  const handleExecuteAction = () => {
    if (onConfirmAction) {
      onConfirmAction(actionType, placement, {
        newName: duplicateName,
      })
    }

    if (actionType === "pause") {
      toast.success(`"${placement.qrName}" paused.`)
    } else if (actionType === "activate") {
      toast.success(`"${placement.qrName}" activated.`)
    } else if (actionType === "duplicate") {
      toast.success(`Created duplicate "${duplicateName}".`)
    } else if (actionType === "archive") {
      toast.success(`"${placement.qrName}" moved to archive.`)
    }

    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="z-[200] max-h-[90vh] w-full max-w-xl overflow-y-auto rounded-md border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
        overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
      >
        {/* VIEW QR DIALOG */}
        {actionType === "view-qr" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-op-action-primary uppercase tracking-wider">
                <QrCode className="size-4 shrink-0" />
                <span>QR Placement</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                {placement.qrName}
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                Scan or share this QR placement to direct guests into your capture and feedback loop.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-6 pt-2">
              {/* QR Preview Card */}
              <div className="flex flex-col items-center justify-center gap-3 rounded-sm border border-op-border-default bg-op-background-primary p-6">
                <div className="rounded-md bg-white p-4 shadow-md">
                  {qrDataUrl ? (
                    <img
                      src={qrDataUrl}
                      alt={`QR Code for ${placement.qrName}`}
                      className="size-48 object-contain"
                    />
                  ) : (
                    <div className="flex size-48 items-center justify-center text-xs text-black/50">
                      Generating QR...
                    </div>
                  )}
                </div>

                <div className="flex flex-col items-center gap-1 text-center">
                  <span className="text-sm font-semibold text-op-text-primary">
                    {placement.qrName}
                  </span>
                  <span className="text-xs text-op-text-muted">
                    {placement.placement} ? {locationName}
                  </span>
                </div>
              </div>

              {/* Details & Link Box */}
              <div className="flex flex-col gap-3 rounded-sm border border-op-border-default bg-op-surface-secondary/40 p-4">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-op-text-muted">Status:</span>
                  <span
                    className={cn(
                      "inline-flex items-center px-2 py-0.5 rounded-xs font-medium text-xs",
                      placement.status === "Active"
                        ? "bg-green-600/20 text-green-500"
                        : "bg-amber-500/20 text-amber-400"
                    )}
                  >
                    {placement.status}
                  </span>
                </div>

                <div className="flex items-center justify-between text-xs">
                  <span className="text-op-text-muted">Total Scans:</span>
                  <span className="font-semibold text-op-text-primary">
                    {placement.scans} scans
                  </span>
                </div>

                <div className="flex flex-col gap-1.5 pt-1">
                  <span className="text-xs font-medium text-op-text-muted">
                    Live Guest Link
                  </span>
                  <div className="flex items-center gap-2">
                    <Input
                      readOnly
                      value={guestUrl}
                      className="h-8 text-xs font-mono text-op-text-muted border-op-border-default bg-op-background-primary"
                    />
                    <Button
                      type="button"
                      variant="op-secondary"
                      size="sm"
                      className="h-8 gap-1.5 px-3 text-xs shrink-0"
                      onClick={handleCopyLink}
                    >
                      {copied ? (
                        <>
                          <Check className="size-3.5 text-green-500" />
                          <span>Copied</span>
                        </>
                      ) : (
                        <>
                          <Copy className="size-3.5" />
                          <span>Copy</span>
                        </>
                      )}
                    </Button>
                  </div>
                </div>
              </div>
            </div>

            <DialogFooter className="flex flex-wrap items-center justify-between gap-2 pt-4 sm:justify-between">
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-9 gap-1.5 text-xs"
                  onClick={handleDownloadPng}
                >
                  <Download className="size-3.5" />
                  <span>Download PNG</span>
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-9 gap-1.5 text-xs"
                  onClick={handlePrint}
                >
                  <Printer className="size-3.5" />
                  <span>Print</span>
                </Button>
              </div>

              <Button
                type="button"
                variant="op-primary"
                className="h-9 px-4 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Done
              </Button>
            </DialogFooter>
          </>
        )}

        {/* DOWNLOAD PDF DIALOG */}
        {actionType === "download-pdf" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-op-action-primary uppercase tracking-wider">
                <FileText className="size-4 shrink-0" />
                <span>Printable Materials</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                Download PDF — {placement.qrName}
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                Select your preferred printable layout formatted for {locationName}.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-4 pt-2">
              <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
                {[
                  {
                    id: "tent",
                    title: "Table Tent (4\" x 6\")",
                    desc: "Pre-formatted fold tent for dining tables and bars.",
                  },
                  {
                    id: "card",
                    title: "Counter Card (5\" x 7\")",
                    desc: "Stands upright at POS and collection points.",
                  },
                  {
                    id: "insert",
                    title: "Delivery Insert (A6)",
                    desc: "Compact card slips easily into takeout bags.",
                  },
                  {
                    id: "vector",
                    title: "Vector Sheet (A4)",
                    desc: "High-resolution vector QR for custom printing.",
                  },
                ].map((fmt) => (
                  <button
                    key={fmt.id}
                    type="button"
                    onClick={() => setSelectedPdfFormat(fmt.id)}
                    className={cn(
                      "flex flex-col gap-1 rounded-sm border p-3.5 text-left transition-all",
                      selectedPdfFormat === fmt.id
                        ? "border-op-action-primary bg-op-action-primary/10"
                        : "border-op-border-default bg-op-background-primary hover:bg-op-surface-secondary/60"
                    )}
                  >
                    <span className="text-xs font-bold text-op-text-primary">
                      {fmt.title}
                    </span>
                    <span className="text-[11px] text-op-text-muted leading-snug">
                      {fmt.desc}
                    </span>
                  </button>
                ))}
              </div>

              <div className="rounded-sm border border-op-border-default bg-op-surface-secondary/40 p-3.5 text-xs text-op-text-muted">
                All PDFs include high-resolution vector QR codes that remain razor-sharp when printed at any scale.
              </div>
            </div>

            <DialogFooter className="flex items-center justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                className="h-9 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button
                type="button"
                variant="op-primary"
                className="h-9 gap-2 text-xs"
                onClick={handleDownloadPdf}
              >
                <Download className="size-3.5" />
                <span>Download PDF</span>
              </Button>
            </DialogFooter>
          </>
        )}

        {/* PAUSE CONFIRMATION DIALOG */}
        {actionType === "pause" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-amber-400 uppercase tracking-wider">
                <Pause className="size-4 shrink-0" />
                <span>Pause Placement</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                Pause "{placement.qrName}"?
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                Guests scanning this QR code will temporarily see a closed notification.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-4 pt-2">
              <div className="flex items-start gap-3 rounded-sm border border-amber-500/30 bg-amber-500/10 p-4">
                <AlertTriangle className="mt-0.5 size-5 shrink-0 text-amber-400" />
                <p className="text-xs leading-relaxed text-op-text-primary">
                  While paused, guests will not be able to submit feedback or claim live offers through this QR code. All scan analytics and redemption history will remain intact.
                </p>
              </div>

              <div className="rounded-sm border border-op-border-default bg-op-background-primary p-3.5 text-xs flex justify-between">
                <span className="text-op-text-muted">Current Scan Activity:</span>
                <span className="font-semibold text-op-text-primary">
                  {placement.scans} scans ? {placement.feedback} feedback
                </span>
              </div>
            </div>

            <DialogFooter className="flex items-center justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                className="h-9 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Keep Active
              </Button>
              <Button
                type="button"
                variant="destructive"
                className="h-9 text-xs"
                onClick={handleExecuteAction}
              >
                Pause Placement
              </Button>
            </DialogFooter>
          </>
        )}

        {/* ACTIVATE CONFIRMATION DIALOG */}
        {actionType === "activate" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-green-500 uppercase tracking-wider">
                <Play className="size-4 shrink-0" />
                <span>Activate Placement</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                Activate "{placement.qrName}"?
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                Reactivate this QR placement to resume collecting guest engagement.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-4 pt-2">
              <div className="rounded-sm border border-op-border-default bg-op-background-primary p-4 text-xs leading-relaxed text-op-text-primary">
                Guests scanning this QR code will immediately be directed to your live capture form and active offers for {locationName}.
              </div>
            </div>

            <DialogFooter className="flex items-center justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                className="h-9 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button
                type="button"
                variant="op-primary"
                className="h-9 text-xs"
                onClick={handleExecuteAction}
              >
                Activate Placement
              </Button>
            </DialogFooter>
          </>
        )}

        {/* DUPLICATE DIALOG */}
        {actionType === "duplicate" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-op-action-primary uppercase tracking-wider">
                <Layers className="size-4 shrink-0" />
                <span>Duplicate Placement</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                Duplicate "{placement.qrName}"
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                Create an independent QR code with the same destination and offer settings.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-4 pt-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="duplicate-qr-name" className="text-xs font-medium text-op-text-muted">
                  New QR Placement Name
                </Label>
                <Input
                  id="duplicate-qr-name"
                  value={duplicateName}
                  onChange={(e) => setDuplicateName(e.target.value)}
                  className="h-9 text-xs border-op-border-default bg-op-background-primary text-op-text-primary"
                  placeholder="e.g. Delivery insert 2"
                />
              </div>

              <div className="rounded-sm border border-op-border-default bg-op-surface-secondary/40 p-3.5 text-xs text-op-text-muted flex justify-between">
                <span>Placement Type:</span>
                <span className="font-semibold text-op-text-primary">
                  {placement.placement}
                </span>
              </div>
            </div>

            <DialogFooter className="flex items-center justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                className="h-9 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button
                type="button"
                variant="op-primary"
                disabled={duplicateName.trim().length === 0}
                className="h-9 text-xs"
                onClick={handleExecuteAction}
              >
                Duplicate Placement
              </Button>
            </DialogFooter>
          </>
        )}

        {/* ARCHIVE DIALOG */}
        {actionType === "archive" && (
          <>
            <DialogHeader className="text-left">
              <div className="flex items-center gap-2 text-xs font-semibold text-red-500 uppercase tracking-wider">
                <Archive className="size-4 shrink-0" />
                <span>Archive Placement</span>
              </div>
              <DialogTitle className="text-xl font-bold text-op-text-primary">
                Archive "{placement.qrName}"?
              </DialogTitle>
              <DialogDescription className="text-xs text-op-text-muted">
                This will deactivate the QR code and remove it from active reports.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-4 pt-2">
              <div className="flex items-start gap-3 rounded-sm border border-red-500/30 bg-red-500/10 p-4">
                <AlertTriangle className="mt-0.5 size-5 shrink-0 text-red-400" />
                <p className="text-xs leading-relaxed text-op-text-primary">
                  Archived placements no longer accept scans or feedback from guests. You can review past performance and restore this placement at any time from the Capture Archive.
                </p>
              </div>
            </div>

            <DialogFooter className="flex items-center justify-end gap-2 pt-4">
              <Button
                type="button"
                variant="outline"
                className="h-9 text-xs"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button
                type="button"
                variant="destructive"
                className="h-9 text-xs"
                onClick={handleExecuteAction}
              >
                Archive Placement
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
