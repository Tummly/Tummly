import { useState } from "react"
import { XIcon } from "lucide-react"

import type { CreateOwnedLocationInput } from "@/api/locationsWriteApi"
import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { isValidUkPostcode } from "@/lib/addressLookup"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

/** Tummly only supports UK Locations on the London reporting clock today. */
const COUNTRY_OPTIONS = [{ value: "GB", label: "United Kingdom" }]
const TIMEZONE_OPTIONS = [{ value: "Europe/London", label: "Europe/London" }]

/** Select menus portal to body; keep them above the Dialog (z-[120]). */
const SELECT_MENU_CLASS = "z-[130]"

type SignupAddLocationDialogProps = {
  open: boolean
  busy?: boolean
  error?: string | null
  onOpenChange: (open: boolean) => void
  onSubmit: (input: CreateOwnedLocationInput) => Promise<void>
}

/** Signup "Add location" dialog — Figma `6780:42136`. */
export function SignupAddLocationDialog({
  open,
  busy = false,
  error = null,
  onOpenChange,
  onSubmit,
}: SignupAddLocationDialogProps) {
  const [locationName, setLocationName] = useState("")
  const [address, setAddress] = useState("")
  const [addressOverridden, setAddressOverridden] = useState(false)
  const [city, setCity] = useState("")
  const [postcode, setPostcode] = useState("")
  const [fieldError, setFieldError] = useState<string | null>(null)

  const reset = () => {
    setLocationName("")
    setAddress("")
    setAddressOverridden(false)
    setCity("")
    setPostcode("")
    setFieldError(null)
  }

  const handleOpenChange = (next: boolean) => {
    if (!next && busy) {
      return
    }
    if (!next) {
      reset()
    }
    onOpenChange(next)
  }

  const handleSubmit = () => {
    if (!locationName.trim() || !address.trim()) {
      setFieldError("Name and address are required.")
      return
    }
    if (!city.trim() || !postcode.trim()) {
      setFieldError("City / town and postcode are required.")
      return
    }
    if (!isValidUkPostcode(postcode)) {
      setFieldError("Enter a valid UK postcode.")
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
  }

  const message = fieldError ?? error

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="max-h-[90vh] gap-5 overflow-y-auto border-0 bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[642px]"
      >
        <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 font-sans text-2xl font-bold tracking-normal text-op-text-primary">
              Add location
            </DialogTitle>
            <DialogDescription className="font-sans text-sm font-normal leading-[19px] text-op-text-primary">
              Enter your restaurant&apos;s details to add it to your Tummly
              account
            </DialogDescription>
          </DialogHeader>
          <DialogClose asChild>
            <Button
              type="button"
              variant="op-collapse"
              className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
              disabled={busy}
              aria-label="Close"
            >
              <XIcon aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className="flex flex-col gap-9">
          <div className="flex flex-col gap-[18px]">
            <FloatingLabelInput
              label="Location name*"
              value={locationName}
              onChange={(event) => setLocationName(event.target.value)}
              disabled={busy}
              reserveClassName="min-h-0"
            />

            <AddressPostcodeFields
              address={address}
              postcode={postcode}
              city={city}
              addressOverridden={addressOverridden}
              addressLabel="Address*"
              cityLabel="City / town*"
              postcodeLabel="Postcode*"
              showPin={false}
              onAddressChange={setAddress}
              onPostcodeChange={setPostcode}
              onCityChange={setCity}
              onCityResolved={setCity}
              onAddressOverriddenChange={setAddressOverridden}
            />

            <FloatingLabelSelect
              label="Country*"
              options={COUNTRY_OPTIONS}
              value="GB"
              contentClassName={SELECT_MENU_CLASS}
              disabled={busy}
            />

            <div className="flex flex-col gap-3">
              <FloatingLabelSelect
                label="Timezone"
                options={TIMEZONE_OPTIONS}
                value="Europe/London"
                contentClassName={SELECT_MENU_CLASS}
                disabled={busy}
              />
              <p className="m-0 font-sans text-sm leading-5 text-op-text-primary">
                We&apos;ll use your Location timezone for reporting, Campaign
                scheduling and activity dates.
              </p>
            </div>

            {message != null ? (
              <p
                className="m-0 text-sm font-medium text-[var(--op-color-red-550)]"
                role="alert"
              >
                {message}
              </p>
            ) : null}
          </div>

          <div className="flex gap-4">
            <Button
              type="button"
              variant="outline"
              disabled={busy}
              onClick={() => handleOpenChange(false)}
              className="h-auto w-[158px] rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-sm font-medium leading-5 text-[#141414] shadow-none hover:bg-white/60"
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={busy}
              onClick={handleSubmit}
              className="h-auto flex-1 rounded-[4px] bg-[#14a74a] px-[18px] py-3 font-sans text-sm font-medium leading-[22px] text-white shadow-none hover:bg-[#14a74a]/90"
            >
              {busy ? "Adding…" : "Add location"}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
