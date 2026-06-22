import { useRef, useState } from "react"

import { AddressPostcodeFields } from "@/components/form/AddressPostcodeFields"

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
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelInput } from "@/components/ui/floating-label-input"
import { Separator } from "@/components/ui/separator"
import { cn } from "@/lib/utils"
import { LOCATION_UPLOAD_ACCEPT } from "@/lib/locationUpload/locationUploadConstants"
import {
  downloadLocationUploadTemplate,
  parseLocationUploadFile,
} from "@/lib/locationUpload/parseLocationUploadFile"
import {
  areAllUploadedLocationsReady,
  countReadyUploadedLocations,
  getUploadedLocationStatus,
  getUploadedLocationStatusLabel,
  type UploadedLocationDraft,
} from "@/lib/locationUpload/locationUploadValidation"
import { emptyLocationItem, type LocationFormItem } from "@/schemas/accountSetupMulti"

type GuestLoopUploadLocationsDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (locations: LocationFormItem[]) => void | Promise<void>
  isSubmitting?: boolean
}

type DialogView = "upload" | "review"

function toLocationFormItems(locations: UploadedLocationDraft[]): LocationFormItem[] {
  return locations.map((location) => ({
    ...emptyLocationItem,
    locationName: location.locationName.trim(),
    address: location.address.trim(),
    postcode: location.postcode.trim(),
    addressOverridden: location.addressOverridden,
    locationPhone: location.locationPhone.trim(),
    localContact: location.localContact.trim(),
  }))
}

type UploadedLocationReviewFieldsProps = {
  location: UploadedLocationDraft
  index: number
  canRemove: boolean
  onChange: (index: number, nextLocation: UploadedLocationDraft) => void
  onRemove: (index: number) => void
}

function UploadedLocationReviewFields({
  location,
  index,
  canRemove,
  onChange,
  onRemove,
}: UploadedLocationReviewFieldsProps) {
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
    <div className="flex flex-col gap-[18px]">
      <FloatingLabelInput
        label="Location name"
        value={location.locationName}
        onChange={(event) => updateField("locationName", event.target.value)}
        required
      />

      <AddressPostcodeFields
        address={location.address}
        postcode={location.postcode}
        addressOverridden={location.addressOverridden}
        onAddressChange={(value) => updateField("address", value)}
        onPostcodeChange={(value) => updateField("postcode", value)}
        onAddressOverriddenChange={(value) =>
          updateField("addressOverridden", value)
        }
      />

      <FloatingLabelInput
        label="Location phone"
        optional
        type="tel"
        autoComplete="tel"
        value={location.locationPhone}
        onChange={(event) => updateField("locationPhone", event.target.value)}
      />

      <FloatingLabelInput
        label="Local contact"
        optional
        value={location.localContact}
        onChange={(event) => updateField("localContact", event.target.value)}
      />

      {canRemove ? (
        <Button
          type="button"
          variant="ghost"
          size="link-sm"
          onClick={() => onRemove(index)}
          className="justify-start p-0 text-sm font-medium text-[#141414] hover:bg-transparent hover:opacity-80"
        >
          Remove
        </Button>
      ) : null}
    </div>
  )
}

export function GuestLoopUploadLocationsDialog({
  open,
  onOpenChange,
  onConfirm,
  isSubmitting = false,
}: GuestLoopUploadLocationsDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [view, setView] = useState<DialogView>("upload")
  const [draftLocations, setDraftLocations] = useState<UploadedLocationDraft[]>([])
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [expandedItems, setExpandedItems] = useState<string[]>(["location-0"])

  const resetDialogState = () => {
    setView("upload")
    setDraftLocations([])
    setUploadError(null)
    setExpandedItems(["location-0"])
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      resetDialogState()
    }

    onOpenChange(nextOpen)
  }

  const handleDownloadTemplate = () => {
    downloadLocationUploadTemplate()
  }

  const handleUploadClick = () => {
    fileInputRef.current?.click()
  }

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
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

    setDraftLocations(result.locations)
    setExpandedItems(["location-0"])
    setView("review")
  }

  const handleDraftChange = (index: number, nextLocation: UploadedLocationDraft) => {
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

  const handleCancelReview = () => {
    resetDialogState()
    onOpenChange(false)
  }

  const handleConfirmReview = async () => {
    if (!areAllUploadedLocationsReady(draftLocations)) {
      return
    }

    await onConfirm(toLocationFormItems(draftLocations))
    resetDialogState()
  }

  const readyCount = countReadyUploadedLocations(draftLocations)
  const canConfirm = areAllUploadedLocationsReady(draftLocations)

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className={cn(view === "review" && "gap-[34px]")}>
        <input
          ref={fileInputRef}
          type="file"
          accept={LOCATION_UPLOAD_ACCEPT}
          className="sr-only"
          onChange={handleFileChange}
        />

        {view === "upload" ? (
          <>
            <DialogHeader>
              <DialogTitle>Upload locations</DialogTitle>
              <DialogDescription className="max-w-[493px]">
                Use our template to add several locations at once. After upload,
                you can review and edit each location before continuing.
              </DialogDescription>
            </DialogHeader>

            <div className="flex flex-col gap-[18px]">
              <div className="flex flex-col gap-4 sm:flex-row">
                <Button
                  type="button"
                  className="flex-1"
                  onClick={handleDownloadTemplate}
                >
                  Download template
                </Button>

                <Button
                  type="button"
                  variant="outline"
                  className="flex-1 border-[#14a74a] text-[#141414] hover:bg-transparent"
                  onClick={handleUploadClick}
                >
                  Upload file
                </Button>
              </div>

              <p className="text-sm font-medium text-[#7d7d7d]">
                CSV or XLSX only.
              </p>

              {uploadError ? (
                <p className="text-sm text-[#f1292d]" role="alert">
                  {uploadError}
                </p>
              ) : null}
            </div>
          </>
        ) : (
          <>
            <div className="relative">
              <DialogHeader className="pr-16">
                <DialogTitle>Review uploaded locations</DialogTitle>
                <DialogDescription className="max-w-[387px]">
                  Check the imported locations before continuing. Fix any rows
                  marked as needing attention.
                </DialogDescription>
              </DialogHeader>

              <p
                className="absolute top-0 right-10 text-base font-semibold leading-[22px] text-[#232323]"
                aria-live="polite"
              >
                {readyCount}/{draftLocations.length}
              </p>
            </div>

            <Accordion
              type="multiple"
              value={expandedItems}
              onValueChange={setExpandedItems}
              className="max-h-[min(52vh,520px)] w-full overflow-y-auto"
            >
              {draftLocations.map((location, index) => {
                const status = getUploadedLocationStatus(location)
                const itemId = `location-${index}`

                return (
                  <AccordionItem
                    key={itemId}
                    value={itemId}
                    className="border-[#dfdfdf] py-6 first:pt-0 last:border-b-0"
                  >
                    <AccordionTrigger className="items-center py-0 hover:no-underline [&>svg]:size-4 [&>svg]:text-[#232323]">
                      <div className="flex items-center gap-3">
                        <span className="text-lg font-semibold leading-5 tracking-[-0.36px] text-[#232323]">
                          Location {index + 1}
                        </span>
                        <Badge
                          variant={status === "ready" ? "ready" : "error"}
                        >
                          {getUploadedLocationStatusLabel(status)}
                        </Badge>
                      </div>
                    </AccordionTrigger>

                    <AccordionContent className="pt-[18px]">
                      <UploadedLocationReviewFields
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
              <Separator className="bg-[#dfdfdf]" />

              <div className="flex flex-col gap-3.5 sm:flex-row">
                <Button
                  type="button"
                  className="flex-1"
                  disabled={!canConfirm || isSubmitting}
                  onClick={() => void handleConfirmReview()}
                >
                  Continue to rollout
                </Button>

                <Button
                  type="button"
                  variant="muted"
                  className="flex-1 bg-[#e4e4e4] text-[#141414] hover:bg-[#e4e4e4]/90"
                  disabled={isSubmitting}
                  onClick={handleCancelReview}
                >
                  Cancel
                </Button>
              </div>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
