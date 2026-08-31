import { useState } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"

export type LocationsAddLocationDialogProps = {
  open: boolean
  busy?: boolean
  error?: string | null
  onOpenChange: (open: boolean) => void
  onSubmit: (input: {
    locationName: string
    address: string
    city: string
    postcode: string
  }) => Promise<void>
}

export function LocationsAddLocationDialog({
  open,
  busy = false,
  error = null,
  onOpenChange,
  onSubmit,
}: LocationsAddLocationDialogProps) {
  const [locationName, setLocationName] = useState("")
  const [address, setAddress] = useState("")
  const [city, setCity] = useState("")
  const [postcode, setPostcode] = useState("")
  const [fieldError, setFieldError] = useState<string | null>(null)

  const reset = () => {
    setLocationName("")
    setAddress("")
    setCity("")
    setPostcode("")
    setFieldError(null)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && busy) {
          return
        }
        if (!next) {
          reset()
        }
        onOpenChange(next)
      }}
    >
      <DialogContent className="gap-6 border-0 bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[560px]">
        <DialogHeader className="gap-2 text-left">
          <DialogTitle className="text-2xl font-bold text-op-text-primary">
            Add location
          </DialogTitle>
          <DialogDescription className="text-sm font-medium text-op-card-subtitle-color">
            Creates a Draft. Name, address, city, and postcode are required.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <FloatingLabelInput
            label="Location name"
            value={locationName}
            onChange={(event) => setLocationName(event.target.value)}
            disabled={busy}
          />
          <FloatingLabelInput
            label="Address"
            value={address}
            onChange={(event) => setAddress(event.target.value)}
            disabled={busy}
          />
          <FloatingLabelInput
            label="City"
            value={city}
            onChange={(event) => setCity(event.target.value)}
            disabled={busy}
          />
          <FloatingLabelInput
            label="Postcode"
            value={postcode}
            onChange={(event) => setPostcode(event.target.value)}
            disabled={busy}
          />
          {(fieldError ?? error) != null ? (
            <p className="m-0 text-sm font-medium text-[var(--op-color-red-550)]">
              {fieldError ?? error}
            </p>
          ) : null}
        </div>

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
            disabled={busy}
            onClick={() => {
              if (
                !locationName.trim()
                || !address.trim()
                || !city.trim()
                || !postcode.trim()
              ) {
                setFieldError(
                  "Name, address, city, and postcode are required."
                )
                return
              }
              setFieldError(null)
              void onSubmit({
                locationName: locationName.trim(),
                address: address.trim(),
                city: city.trim(),
                postcode: postcode.trim(),
              }).then(() => {
                reset()
                onOpenChange(false)
              })
            }}
          >
            {busy ? "Creating…" : "Create draft"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
