import { useEffect, useState } from "react"
import { XIcon } from "lucide-react"

import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { isValidUkPostcode } from "@/lib/addressLookup"
import type { LocationDetailEditFields } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import { LOCATION_DETAIL_PAGE_COPY } from "@/lib/operatorLocations/locationDetailPresentation"
import { cn } from "@/lib/utils"

const FIELD_INPUT_CLASS =
  "h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-text-muted md:text-sm dark:bg-transparent"

const FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

export type LocationDetailEditDetailsDialogProps = {
  open: boolean
  initialValues: LocationDetailEditFields
  busy?: boolean
  error?: string | null
  onOpenChange: (open: boolean) => void
  onSubmit: (input: {
    locationName: string
    address: string
    city: string
    postcode: string
    locationPhone?: string
    localContact?: string
  }) => Promise<void>
}

export function LocationDetailEditDetailsDialog({
  open,
  initialValues,
  busy = false,
  error = null,
  onOpenChange,
  onSubmit,
}: LocationDetailEditDetailsDialogProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY
  const [locationName, setLocationName] = useState(initialValues.locationName)
  const [address, setAddress] = useState(initialValues.address)
  const [addressOverridden, setAddressOverridden] = useState(false)
  const [city, setCity] = useState(initialValues.city)
  const [postcode, setPostcode] = useState(initialValues.postcode)
  const [detailsRevealed, setDetailsRevealed] = useState(true)
  const [locationPhone, setLocationPhone] = useState(
    initialValues.locationPhone
  )
  const [localContact, setLocalContact] = useState(initialValues.localContact)
  const [fieldError, setFieldError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) {
      return
    }
    setLocationName(initialValues.locationName)
    setAddress(initialValues.address)
    setAddressOverridden(false)
    setCity(initialValues.city)
    setPostcode(initialValues.postcode)
    setDetailsRevealed(true)
    setLocationPhone(initialValues.locationPhone)
    setLocalContact(initialValues.localContact)
    setFieldError(null)
  }, [initialValues, open])

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
      <DialogContent
        showCloseButton={false}
        className="max-h-[90vh] gap-[60px] overflow-y-auto border-0 bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[792px]"
      >
        <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
          <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
            <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
              {copy.editDetailsTitle}
            </DialogTitle>
            <DialogDescription className="max-w-[493px] text-base font-medium leading-normal text-op-card-subtitle-color">
              {copy.editDetailsDescription}
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

        <div className="flex flex-col gap-5">
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="locations-edit-name" className={FIELD_LABEL_CLASS}>
                Location name
              </Label>
              <Input
                id="locations-edit-name"
                className={FIELD_INPUT_CLASS}
                placeholder="Enter"
                value={locationName}
                onChange={(event) => setLocationName(event.target.value)}
                disabled={busy}
              />
            </div>

            <AddressPostcodeFields
              appearance="operator"
              address={address}
              postcode={postcode}
              city={city}
              addressOverridden={addressOverridden}
              showCityAndPostcode={detailsRevealed}
              onAddressChange={setAddress}
              onPostcodeChange={setPostcode}
              onCityChange={setCity}
              onAddressOverriddenChange={setAddressOverridden}
              onCityResolved={setCity}
              onDetailsRevealed={(source) => {
                setDetailsRevealed(true)
                if (source === "manual") {
                  setCity("")
                  setPostcode("")
                }
              }}
            />
          </div>

          <div className="h-px w-full bg-op-border-default" />

          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="locations-edit-country" className={FIELD_LABEL_CLASS}>
                Country
              </Label>
              <Input
                id="locations-edit-country"
                className={cn(FIELD_INPUT_CLASS, "opacity-70")}
                value="United Kingdom"
                disabled
                readOnly
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="locations-edit-phone" className={FIELD_LABEL_CLASS}>
                Location phone
              </Label>
              <Input
                id="locations-edit-phone"
                className={FIELD_INPUT_CLASS}
                placeholder="Enter"
                type="tel"
                autoComplete="tel"
                value={locationPhone}
                onChange={(event) => setLocationPhone(event.target.value)}
                disabled={busy}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="locations-edit-contact"
                className={FIELD_LABEL_CLASS}
              >
                Local contact
              </Label>
              <Input
                id="locations-edit-contact"
                className={FIELD_INPUT_CLASS}
                placeholder="Enter"
                value={localContact}
                onChange={(event) => setLocalContact(event.target.value)}
                disabled={busy}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="locations-edit-currency"
                className={FIELD_LABEL_CLASS}
              >
                Currency
              </Label>
              <Input
                id="locations-edit-currency"
                className={cn(FIELD_INPUT_CLASS, "opacity-70")}
                value="GBP"
                disabled
                readOnly
              />
            </div>
          </div>

          {(fieldError ?? error) != null ? (
            <p className="m-0 text-sm font-medium text-[var(--op-color-red-550)]">
              {fieldError ?? error}
            </p>
          ) : null}
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={busy}
            onClick={() => {
              if (!locationName.trim() || !address.trim()) {
                setFieldError("Name and address are required.")
                return
              }
              if (!city.trim() || !postcode.trim()) {
                setFieldError("City and postcode are required.")
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
                locationPhone: locationPhone.trim() || undefined,
                localContact: localContact.trim() || undefined,
              }).then(() => {
                onOpenChange(false)
              })
            }}
          >
            {busy ? copy.savingDetails : copy.saveDetails}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
