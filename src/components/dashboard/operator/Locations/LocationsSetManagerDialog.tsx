import { useEffect, useState } from "react"

import { getTeamPermissionsPage } from "@/api/teamPermissionsApi"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"

const CLEAR_VALUE = "__clear__"

const MANAGER_ROLES = new Set([
  "Owner",
  "Admin",
  "Area Manager",
  "Location Manager",
])

export type LocationsSetManagerDialogProps = {
  open: boolean
  locationId: string | null
  locationName: string
  currentManagerUserId: number | null
  busy?: boolean
  error?: string | null
  onOpenChange: (open: boolean) => void
  onSubmit: (managerUserId: number | null) => Promise<void>
}

export function LocationsSetManagerDialog({
  open,
  locationId,
  locationName,
  currentManagerUserId,
  busy = false,
  error = null,
  onOpenChange,
  onSubmit,
}: LocationsSetManagerDialogProps) {
  const [options, setOptions] = useState<
    Array<{ value: string; label: string }>
  >([{ value: CLEAR_VALUE, label: "No manager (—)" }])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [selected, setSelected] = useState<string>(CLEAR_VALUE)

  useEffect(() => {
    if (!open || locationId == null) {
      return
    }
    setSelected(
      currentManagerUserId != null ? String(currentManagerUserId) : CLEAR_VALUE
    )
    setLoadError(null)
    let cancelled = false
    void getTeamPermissionsPage()
      .then((page) => {
        if (cancelled) {
          return
        }
        const locationNumeric = Number.parseInt(locationId, 10)
        const next = page.members
          .filter((member) => member.status === "active")
          .filter((member) => MANAGER_ROLES.has(member.permissionRole))
          .filter((member) => {
            if (member.locationScope === "all") {
              return true
            }
            return member.namedLocationIds.includes(locationNumeric)
          })
          .map((member) => ({
            value: String(member.userId),
            label: `${member.fullName} (${member.permissionRole})`,
          }))
        setOptions([
          { value: CLEAR_VALUE, label: "No manager (—)" },
          ...next,
        ])
      })
      .catch(() => {
        if (!cancelled) {
          setLoadError("Could not load team members.")
        }
      })
    return () => {
      cancelled = true
    }
  }, [open, locationId, currentManagerUserId])

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && busy) {
          return
        }
        onOpenChange(next)
      }}
    >
      <DialogContent className="gap-6 border-0 bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[560px]">
        <DialogHeader className="gap-2 text-left">
          <DialogTitle className="text-2xl font-bold text-op-text-primary">
            Set manager
          </DialogTitle>
          <DialogDescription className="text-sm font-medium text-op-card-subtitle-color">
            Nominate a manager for {locationName}. Clear to show —.
          </DialogDescription>
        </DialogHeader>

        <FloatingLabelSelect
          label="Manager"
          options={options}
          value={selected}
          onValueChange={setSelected}
          disabled={busy || loadError != null}
          contentClassName="z-[80]"
        />

        {(loadError ?? error) != null ? (
          <p className="m-0 text-sm font-medium text-[var(--op-color-red-550)]">
            {loadError ?? error}
          </p>
        ) : null}

        <DialogFooter className="gap-3 sm:justify-end">
          <Button
            type="button"
            variant="op-secondary"
            disabled={busy}
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="op-primary"
            disabled={busy || loadError != null}
            onClick={() => {
              const managerUserId =
                selected === CLEAR_VALUE
                  ? null
                  : Number.parseInt(selected, 10)
              void onSubmit(
                Number.isFinite(managerUserId) ? managerUserId : null
              ).then(() => onOpenChange(false))
            }}
          >
            {busy ? "Saving…" : "Save"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
