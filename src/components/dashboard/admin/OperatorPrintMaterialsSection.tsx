import { useEffect, useState, useSyncExternalStore } from "react"
import { DownloadIcon, RefreshCwIcon } from "lucide-react"
import { toast } from "sonner"

import { hasCreatedOperatorAccount } from "@/components/dashboard/admin/adminTrialRequestStatus"
import { Button } from "@/components/ui/button"
import {
  createOperatorPrintMaterialsPageModule,
  httpOperatorPrintMaterialsAdapters,
  type OperatorPrintMaterialsPageModule,
} from "@/lib/admin/createOperatorPrintMaterialsPageModule"
import type { AdminTrialRequest } from "@/types/admin"
import type { AdminPrintMaterialQrType } from "@/types/adminPrintMaterials"

type OperatorPrintMaterialsSectionProps = {
  request: AdminTrialRequest
}

const QR_TYPE_LABELS: Record<AdminPrintMaterialQrType, string> = {
  TableTent: "Table Tent",
  WindowSticker: "Window Sticker",
  OfferCard: "Offer Card",
}

export function OperatorPrintMaterialsSection({
  request,
}: OperatorPrintMaterialsSectionProps) {
  const operatorUserId = request.operatorUserId
  const [module] = useState<OperatorPrintMaterialsPageModule>(() =>
    createOperatorPrintMaterialsPageModule(httpOperatorPrintMaterialsAdapters)
  )
  const snapshot = useSyncExternalStore(module.subscribe, module.getSnapshot)

  useEffect(() => {
    if (!hasCreatedOperatorAccount(request) || operatorUserId == null) {
      return
    }
    void module.load(operatorUserId)
  }, [module, operatorUserId, request])

  if (!hasCreatedOperatorAccount(request) || operatorUserId == null) {
    return null
  }

  const handleDownload = async (
    locationId: number,
    qrType: AdminPrintMaterialQrType
  ) => {
    const ok = await module.download(locationId, qrType)
    if (ok) {
      toast.success("Print material downloaded.")
    } else {
      toast.error("Could not download print material.")
    }
  }

  const handleRetry = async (
    locationId: number,
    qrType: AdminPrintMaterialQrType
  ) => {
    const ok = await module.retry(locationId, qrType)
    if (ok) {
      toast.success("Print material regenerated.")
    } else {
      toast.error("Could not retry print material.")
    }
  }

  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold text-foreground">
        Starter QR materials
      </h3>
      {snapshot.loadStatus === "loading" || snapshot.loadStatus === "idle" ? (
        <p className="text-sm text-muted-foreground">Loading print materials…</p>
      ) : null}
      {snapshot.loadStatus === "error" ? (
        <div className="flex flex-col gap-2">
          <p className="text-sm text-muted-foreground">
            {snapshot.errorMessage}
          </p>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="w-fit"
            onClick={() => void module.retryLoad()}
          >
            Retry load
          </Button>
        </div>
      ) : null}
      {snapshot.loadStatus === "loaded" && snapshot.rows.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No owned locations yet.
        </p>
      ) : null}
      {snapshot.loadStatus === "loaded"
        ? snapshot.rows.map((row) => {
            const busy =
              snapshot.busyKey === `${row.locationId}:${row.qrType}`
            return (
              <article
                key={`${row.locationId}-${row.qrType}`}
                className="flex flex-col gap-2 rounded-xl border bg-muted/20 p-4"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-col gap-0.5">
                    <p className="text-sm font-medium text-foreground">
                      {row.locationName}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {QR_TYPE_LABELS[row.qrType] ?? row.qrType} · {row.status}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={!row.canDownload || busy}
                      onClick={() =>
                        void handleDownload(row.locationId, row.qrType)
                      }
                    >
                      <DownloadIcon className="size-4" />
                      Download
                    </Button>
                    {row.canRetry ? (
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={busy}
                        onClick={() =>
                          void handleRetry(row.locationId, row.qrType)
                        }
                      >
                        <RefreshCwIcon className="size-4" />
                        Retry
                      </Button>
                    ) : null}
                  </div>
                </div>
                {row.lastError ? (
                  <p className="text-xs text-muted-foreground">{row.lastError}</p>
                ) : null}
              </article>
            )
          })
        : null}
    </section>
  )
}
