import { useState } from "react"
import { CopyIcon, DownloadIcon } from "lucide-react"
import { toast } from "sonner"

import {
  downloadActivationAsset,
  extendActivation,
} from "@/api/adminApi"
import {
  getActivationStatusDetailLabel,
  hasCreatedOperatorAccount,
} from "@/components/dashboard/admin/adminTrialRequestStatus"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { formatAdminDate, formatAdminText } from "@/lib/adminTrialRequestLabels"
import type { AdminTrialRequest } from "@/types/admin"

type OperatorActivationSectionProps = {
  request: AdminTrialRequest
  onRequestUpdated: (request: AdminTrialRequest) => void
}

function defaultExtensionDate() {
  const date = new Date()
  date.setUTCDate(date.getUTCDate() + 30)
  return date.toISOString().slice(0, 10)
}

export function OperatorActivationSection({
  request,
  onRequestUpdated,
}: OperatorActivationSectionProps) {
  const [extendOpen, setExtendOpen] = useState(false)
  const [extensionDate, setExtensionDate] = useState(defaultExtensionDate)
  const [extending, setExtending] = useState(false)
  const [downloading, setDownloading] = useState(false)

  if (!hasCreatedOperatorAccount(request)) {
    return null
  }

  const showExtend = request.activationStatusDetail === "expired"
  const operatorUserId = request.operatorUserId

  const handleCopyCode = async () => {
    if (!request.activationCode) {
      toast.error("Activation code is not available yet.")
      return
    }

    try {
      await navigator.clipboard.writeText(request.activationCode)
      toast.success("Activation code copied.")
    } catch {
      toast.error("Could not copy activation code.")
    }
  }

  const handleDownload = async () => {
    if (!operatorUserId) {
      toast.error("Operator account is not available.")
      return
    }

    setDownloading(true)

    try {
      const blob = await downloadActivationAsset(operatorUserId)
      const objectUrl = URL.createObjectURL(blob)
      const link = document.createElement("a")
      link.href = objectUrl
      link.download = `tummly-activation-${request.businessName
        .toLowerCase()
        .replace(/[^a-z0-9-]+/g, "-")
        .replace(/-+/g, "-")
        .replace(/^-|-$/g, "") || "operator"}.svg`
      link.click()
      URL.revokeObjectURL(objectUrl)
      toast.success("Activation asset downloaded.")
    } catch {
      toast.error("Could not download activation asset.")
    } finally {
      setDownloading(false)
    }
  }

  const handleExtend = async () => {
    if (!operatorUserId) {
      toast.error("Operator account is not available.")
      return
    }

    setExtending(true)

    try {
      const expiresAt = new Date(`${extensionDate}T00:00:00.000Z`).toISOString()
      const updated = await extendActivation(operatorUserId, { expiresAt })
      onRequestUpdated(updated)
      setExtendOpen(false)
      toast.success("Activation period extended.")
    } catch {
      toast.error("Could not extend activation.")
    } finally {
      setExtending(false)
    }
  }

  return (
    <>
      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-foreground">Activation</h3>
        <dl className="grid grid-cols-2 gap-x-4 gap-y-4">
          <div className="flex flex-col gap-1">
            <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
              Status
            </dt>
            <dd className="text-sm text-foreground">
              {formatAdminText(
                getActivationStatusDetailLabel(request.activationStatusDetail)
              )}
            </dd>
          </div>
          {request.activationExpiresAt ? (
            <div className="flex flex-col gap-1">
              <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                Activation period ends
              </dt>
              <dd className="text-sm text-foreground">
                {formatAdminText(formatAdminDate(request.activationExpiresAt))}
              </dd>
            </div>
          ) : null}
          {request.activationCode ? (
            <div className="col-span-2 flex flex-col gap-2">
              <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                Activation code
              </dt>
              <dd className="flex flex-wrap items-center gap-2">
                <code className="rounded-md bg-muted px-2 py-1 text-sm font-medium">
                  {request.activationCode}
                </code>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleCopyCode}
                >
                  <CopyIcon className="size-4" />
                  Copy
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleDownload}
                  disabled={downloading || !operatorUserId}
                >
                  <DownloadIcon className="size-4" />
                  Download
                </Button>
              </dd>
            </div>
          ) : null}
        </dl>
        {showExtend ? (
          <Button
            type="button"
            variant="secondary"
            className="w-fit"
            onClick={() => {
              setExtensionDate(defaultExtensionDate())
              setExtendOpen(true)
            }}
          >
            Extend activation
          </Button>
        ) : null}
      </section>

      <Dialog open={extendOpen} onOpenChange={setExtendOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Extend activation</DialogTitle>
            <DialogDescription>
              Restore dashboard access for {request.businessName} without
              re-shipping materials.
            </DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-2">
            <label
              htmlFor="activation-extension-date"
              className="text-sm font-medium text-foreground"
            >
              New activation period end (UTC)
            </label>
            <Input
              id="activation-extension-date"
              type="date"
              value={extensionDate}
              onChange={(event) => setExtensionDate(event.target.value)}
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setExtendOpen(false)}
              disabled={extending}
            >
              Cancel
            </Button>
            <Button type="button" onClick={handleExtend} disabled={extending}>
              Extend activation
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
