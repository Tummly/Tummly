import { useRef, useState } from "react"
import { XIcon } from "lucide-react"

import { downloadLocationUploadTemplate } from "@/api/locationUploadApi"
import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"
import { getGuestLoopLocationLabel } from "@/components/guest-loop/guestLoopLocationLabel"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { Badge } from "@/components/ui/badge"
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
import { Separator } from "@/components/ui/separator"
import { resolveTownCity } from "@/lib/addressLookup"
import { LOCATION_UPLOAD_ACCEPT } from "@/lib/locationUpload/locationUploadConstants"
import { parseLocationUploadFile } from "@/lib/locationUpload/parseLocationUploadFile"
import {
  getUploadedLocationStatus,
  getUploadedLocationStatusLabel,
  type UploadedLocationDraft,
} from "@/lib/locationUpload/locationUploadValidation"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

const FIELD_INPUT_CLASS =
  "h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-text-muted md:text-sm dark:bg-transparent"

const FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

export type LocationsImportRow = {
  locationName: string
  address: string
  city: string
  postcode: string
  locationPhone?: string
  localContact?: string
}

type LocationsImportLocationsDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (locations: LocationsImportRow[]) => void | Promise<void>
  isSubmitting?: boolean
}

type DialogView = "upload" | "review"

function withResolvedCity(location: UploadedLocationDraft): UploadedLocationDraft {
  const city =
    location.city.trim() || resolveTownCity({ address: location.address })
  return { ...location, city }
}

function isSettingsImportReady(location: UploadedLocationDraft) {
  return getUploadedLocationStatus(withResolvedCity(location)) === "ready"
}

function countSettingsReady(locations: UploadedLocationDraft[]) {
  return locations.filter(isSettingsImportReady).length
}

type ReviewFieldsProps = {
  location: UploadedLocationDraft
  index: number
  canRemove: boolean
  onChange: (index: number, nextLocation: UploadedLocationDraft) => void
  onRemove: (index: number) => void
}

function ReviewFields({
  location,
  index,
  canRemove,
  onChange,
  onRemove,
}: ReviewFieldsProps) {
  const updateField = <K extends keyof UploadedLocationDraft>(
    field: K,
    value: UploadedLocationDraft[K]
  ) => {
    onChange(index, {
      ...location,
      [field]: value,
    })
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label className={FIELD_LABEL_CLASS} htmlFor={`import-name-${index}`}>
            Location name
          </Label>
          <Input
            id={`import-name-${index}`}
            className={FIELD_INPUT_CLASS}
            value={location.locationName}
            onChange={(event) => updateField("locationName", event.target.value)}
          />
        </div>

        <AddressPostcodeFields
          appearance="operator"
          address={location.address}
          postcode={location.postcode}
          city={
            location.addressOverridden
              ? location.city
              : location.city.trim()
                || resolveTownCity({ address: location.address })
          }
          addressOverridden={location.addressOverridden}
          showCityAndPostcode
          onAddressChange={(value) => updateField("address", value)}
          onPostcodeChange={(value) => updateField("postcode", value)}
          onCityChange={(value) => updateField("city", value)}
          onAddressOverriddenChange={(value) =>
            updateField("addressOverridden", value)
          }
          onCityResolved={(city) => updateField("city", city)}
        />
      </div>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label className={FIELD_LABEL_CLASS} htmlFor={`import-phone-${index}`}>
            Location phone
          </Label>
          <Input
            id={`import-phone-${index}`}
            className={FIELD_INPUT_CLASS}
            type="tel"
            autoComplete="tel"
            value={location.locationPhone}
            onChange={(event) =>
              updateField("locationPhone", event.target.value)
            }
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label
            className={FIELD_LABEL_CLASS}
            htmlFor={`import-contact-${index}`}
          >
            Local contact
          </Label>
          <Input
            id={`import-contact-${index}`}
            className={FIELD_INPUT_CLASS}
            value={location.localContact}
            onChange={(event) =>
              updateField("localContact", event.target.value)
            }
          />
        </div>
      </div>

      {canRemove ? (
        <Button
          type="button"
          variant="op-link"
          onClick={() => onRemove(index)}
          className="justify-start"
        >
          Remove
        </Button>
      ) : null}
    </div>
  )
}

export function LocationsImportLocationsDialog({
  open,
  onOpenChange,
  onConfirm,
  isSubmitting = false,
}: LocationsImportLocationsDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [view, setView] = useState<DialogView>("upload")
  const [draftLocations, setDraftLocations] = useState<UploadedLocationDraft[]>(
    []
  )
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const [isDownloadingTemplate, setIsDownloadingTemplate] = useState(false)
  const [expandedItems, setExpandedItems] = useState<string[]>(["location-0"])

  const resetDialogState = () => {
    setView("upload")
    setDraftLocations([])
    setUploadError(null)
    setDownloadError(null)
    setIsDownloadingTemplate(false)
    setExpandedItems(["location-0"])
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      resetDialogState()
    }

    onOpenChange(nextOpen)
  }

  const handleDownloadTemplate = async () => {
    setDownloadError(null)
    setIsDownloadingTemplate(true)

    try {
      await downloadLocationUploadTemplate()
    } catch (error: unknown) {
      const message =
        error instanceof Error
          ? error.message
          : "Unable to download the locations template. Please try again."

      setDownloadError(message)
    } finally {
      setIsDownloadingTemplate(false)
    }
  }

  const handleFileChange = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0]
    event.target.value = ""

    if (!file) {
      return
    }

    setUploadError(null)
    const result = await parseLocationUploadFile(file)

    if ("error" in result) {
      setUploadError(result.error)
      return
    }

    setDraftLocations(result.locations.map(withResolvedCity))
    setExpandedItems(["location-0"])
    setView("review")
  }

  const handleDraftChange = (
    index: number,
    nextLocation: UploadedLocationDraft
  ) => {
    setDraftLocations((current) =>
      current.map((location, locationIndex) =>
        locationIndex === index ? nextLocation : location
      )
    )
  }

  const handleRemoveLocation = (index: number) => {
    setDraftLocations((current) => {
      if (current.length <= 1) {
        return current
      }

      return current.filter((_, locationIndex) => locationIndex !== index)
    })

    setExpandedItems((current) =>
      current
        .filter((item) => item !== `location-${index}`)
        .map((item) => {
          const itemIndex = Number(item.replace("location-", ""))
          return itemIndex > index ? `location-${itemIndex - 1}` : item
        })
    )
  }

  const handleConfirmReview = async () => {
    const resolved = draftLocations.map(withResolvedCity)
    if (!resolved.every(isSettingsImportReady)) {
      return
    }

    await onConfirm(
      resolved.map((location) => ({
        locationName: location.locationName.trim(),
        address: location.address.trim(),
        city: location.city.trim(),
        postcode: location.postcode.trim(),
        locationPhone: location.locationPhone.trim() || undefined,
        localContact: location.localContact.trim() || undefined,
      }))
    )
    resetDialogState()
  }

  const readyCount = countSettingsReady(draftLocations)
  const canConfirm =
    draftLocations.length > 0 && readyCount === draftLocations.length

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="max-h-[90vh] overflow-y-auto border-0 bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[640px]"
      >
        <input
          ref={fileInputRef}
          type="file"
          accept={LOCATION_UPLOAD_ACCEPT}
          className="sr-only"
          onChange={(event) => {
            void handleFileChange(event)
          }}
        />

        {view === "upload" ? (
          <div className="flex flex-col gap-[60px]">
            <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
              <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
                <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                  Import locations
                </DialogTitle>
                <DialogDescription className="max-w-[493px] text-base font-medium leading-normal text-op-card-subtitle-color">
                  Use our template to add several locations at once. After
                  upload, you can review and edit each location before
                  continuing.
                </DialogDescription>
              </DialogHeader>
              <DialogClose asChild>
                <Button
                  type="button"
                  variant="op-collapse"
                  className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                  aria-label="Close"
                >
                  <XIcon aria-hidden />
                </Button>
              </DialogClose>
            </div>

            <div className="flex flex-col gap-[18px]">
              <div className="flex flex-col gap-3 sm:flex-row">
                <Button
                  type="button"
                  variant="op-primary"
                  className="flex-1"
                  disabled={isDownloadingTemplate}
                  onClick={() => void handleDownloadTemplate()}
                >
                  {isDownloadingTemplate
                    ? "Downloading..."
                    : "Download template"}
                </Button>

                <Button
                  type="button"
                  variant="op-tertiary"
                  className="flex-1"
                  onClick={() => fileInputRef.current?.click()}
                >
                  Upload file
                </Button>
              </div>

              <p className="m-0 text-sm font-medium text-op-text-muted">
                CSV or XLSX only.
              </p>

              {uploadError ? (
                <p
                  className="m-0 text-sm text-[var(--op-color-red-550)]"
                  role="alert"
                >
                  {uploadError}
                </p>
              ) : null}

              {downloadError ? (
                <p
                  className="m-0 text-sm text-[var(--op-color-red-550)]"
                  role="alert"
                >
                  {downloadError}
                </p>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="flex flex-col gap-[34px]">
            <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
              <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
                <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                  Review uploaded locations
                </DialogTitle>
                <DialogDescription className="max-w-[387px] text-base font-medium leading-normal text-op-card-subtitle-color">
                  Check the imported locations before continuing. Fix any rows
                  marked as needing attention.
                </DialogDescription>
              </DialogHeader>

              <div className="flex shrink-0 flex-col items-end gap-3">
                <p
                  className="m-0 text-base font-semibold leading-[22px] text-op-text-primary"
                  aria-live="polite"
                >
                  {readyCount}/{draftLocations.length}
                </p>
                <DialogClose asChild>
                  <Button
                    type="button"
                    variant="op-collapse"
                    className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                    disabled={isSubmitting}
                    aria-label="Close"
                  >
                    <XIcon aria-hidden />
                  </Button>
                </DialogClose>
              </div>
            </div>

            <Accordion
              type="multiple"
              value={expandedItems}
              onValueChange={setExpandedItems}
              className="max-h-[min(52vh,520px)] w-full overflow-y-auto"
            >
              {draftLocations.map((location, index) => {
                const status = getUploadedLocationStatus(
                  withResolvedCity(location)
                )
                const itemId = `location-${index}`
                const locationLabel = getGuestLoopLocationLabel(
                  location.locationName,
                  index
                )

                return (
                  <AccordionItem
                    key={itemId}
                    value={itemId}
                    className="border-op-border-default py-6 first:pt-0 last:border-b-0"
                  >
                    <AccordionTrigger className="items-center py-0 text-op-text-primary hover:no-underline [&>svg]:size-4 [&>svg]:text-op-text-primary">
                      <div className="flex items-center gap-3">
                        <span className="truncate text-lg font-semibold leading-5 tracking-[-0.36px] text-op-text-primary">
                          {locationLabel}
                        </span>
                        <Badge
                          variant={status === "ready" ? "ready" : "error"}
                        >
                          {getUploadedLocationStatusLabel(status)}
                        </Badge>
                      </div>
                    </AccordionTrigger>

                    <AccordionContent className="pt-[18px]">
                      <ReviewFields
                        location={location}
                        index={index}
                        canRemove={draftLocations.length > 1}
                        onChange={handleDraftChange}
                        onRemove={handleRemoveLocation}
                      />
                    </AccordionContent>
                  </AccordionItem>
                )
              })}
            </Accordion>

            <div className="flex flex-col gap-6">
              <Separator className="bg-op-border-default" />

              <DialogFooter className="flex-row gap-3 sm:justify-start">
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={!canConfirm || isSubmitting}
                  onClick={() => void handleConfirmReview()}
                >
                  Import locations
                </Button>

                <Button
                  type="button"
                  variant="op-tertiary"
                  disabled={isSubmitting}
                  onClick={() => handleOpenChange(false)}
                >
                  Cancel
                </Button>
              </DialogFooter>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
