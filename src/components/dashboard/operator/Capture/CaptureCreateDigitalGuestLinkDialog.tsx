"use client"

import { useEffect, useState } from "react"

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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import {
  DIGITAL_GUEST_LINK_CHANNEL_OPTIONS,
  DIGITAL_GUEST_LINK_STATUS_OPTIONS,
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CreateDigitalGuestLinkModuleInput } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import type {
  CaptureDigitalGuestLinkChannel,
  CapturePlacementStatus,
} from "@/types/dashboard"

type FieldErrors = {
  linkName?: string
  internalDescription?: string
  channel?: string
  location?: string
}

type CaptureCreateDigitalGuestLinkDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  busy?: boolean
  prefill?: {
    linkName: string
    channel: CaptureDigitalGuestLinkChannel
    status: CapturePlacementStatus
  }
  /** Multi-root Locations single-select options. */
  locationOptions?: readonly { id: number; label: string }[]
  /** When true, Locations is pre-bound (hidden or read-only). */
  locationBound?: boolean
  selectedLocationId?: number | null
  onLocationIdChange?: (locationId: number | null) => void
  onSubmit: (
    input: CreateDigitalGuestLinkModuleInput
  ) => Promise<"created" | "duplicate_link_name" | "failed" | "noop">
}

function validate(
  linkName: string,
  internalDescription: string,
  channel: CaptureDigitalGuestLinkChannel | "",
  options?: {
    requireLocation?: boolean
    locationId?: number | null
  }
): FieldErrors {
  const copy = OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY
  const errors: FieldErrors = {}
  const trimmedName = linkName.trim()
  if (trimmedName.length === 0) {
    errors.linkName = copy.linkNameRequired
  } else if (trimmedName.length > copy.linkNameMaxLength) {
    errors.linkName = copy.linkNameMax
  }
  if (internalDescription.trim().length > copy.internalDescriptionMaxLength) {
    errors.internalDescription = copy.internalDescriptionMax
  }
  if (channel === "") {
    errors.channel = copy.channelRequired
  }
  if (options?.requireLocation && options.locationId == null) {
    errors.location = copy.locationRequired
  }
  return errors
}

/** Create digital guest link dialog — Figma `4252:60713`. */
export function CaptureCreateDigitalGuestLinkDialog({
  open,
  onOpenChange,
  busy = false,
  prefill,
  locationOptions,
  locationBound = false,
  selectedLocationId = null,
  onLocationIdChange,
  onSubmit,
}: CaptureCreateDigitalGuestLinkDialogProps) {
  const copy = OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY
  const [linkName, setLinkName] = useState("")
  const [internalDescription, setInternalDescription] = useState("")
  const [channel, setChannel] = useState<CaptureDigitalGuestLinkChannel | "">(
    ""
  )
  const [status, setStatus] = useState<CapturePlacementStatus>("Active")
  const [errors, setErrors] = useState<FieldErrors>({})
  const showLocationSelect =
    locationOptions != null && locationOptions.length > 0
  const requireLocationSelect = showLocationSelect && !locationBound
  const boundLocationLabel =
    locationBound && selectedLocationId != null
      ? (locationOptions?.find((option) => option.id === selectedLocationId)
          ?.label ?? String(selectedLocationId))
      : null

  useEffect(() => {
    if (!open) {
      return
    }
    setLinkName(prefill?.linkName ?? "")
    setInternalDescription("")
    setChannel(prefill?.channel ?? "")
    setStatus(prefill?.status ?? "Active")
    setErrors({})
  }, [open, prefill])

  const handleOpenChange = (nextOpen: boolean) => {
    if (busy && !nextOpen) {
      return
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        showCloseButton
        className="max-h-[min(90vh,900px)] gap-[60px] overflow-y-auto bg-op-surface-secondary p-8 text-foreground sm:max-w-[560px]"
      >
        <div className="flex flex-col gap-5">
          <DialogHeader className="gap-3 pr-10">
            <DialogTitle className="text-2xl font-bold tracking-normal text-foreground">
              {copy.title}
            </DialogTitle>
            <DialogDescription className="max-w-[441px] text-sm font-medium leading-[18px] text-op-text-muted">
              {copy.description}
            </DialogDescription>
          </DialogHeader>

          {showLocationSelect ? (
            <div className="flex flex-col gap-2">
              <span className="text-sm font-semibold leading-5 text-op-text-primary">
                {copy.locationLabel}
              </span>
              {locationBound ? (
                <div className="flex min-h-[50px] items-center rounded border border-op-input-border bg-op-background-secondary px-[15px] py-[15px] text-sm text-op-text-primary">
                  {boundLocationLabel}
                </div>
              ) : (
                <Select
                  value={
                    selectedLocationId != null
                      ? String(selectedLocationId)
                      : undefined
                  }
                  onValueChange={(value) => {
                    const nextId = Number(value)
                    if (!Number.isFinite(nextId)) {
                      return
                    }
                    onLocationIdChange?.(nextId)
                    if (errors.location) {
                      setErrors((prev) => ({ ...prev, location: undefined }))
                    }
                  }}
                  disabled={busy}
                >
                  <SelectTrigger
                    aria-invalid={errors.location ? true : undefined}
                    className="h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none data-placeholder:text-op-text-muted dark:bg-transparent dark:hover:bg-transparent"
                  >
                    <SelectValue placeholder={copy.locationPlaceholder} />
                  </SelectTrigger>
                  <SelectContent>
                    {locationOptions.map((option) => (
                      <SelectItem key={option.id} value={String(option.id)}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              {errors.location ? (
                <p className="text-sm text-destructive" role="alert">
                  {errors.location}
                </p>
              ) : null}
            </div>
          ) : null}

          <div className="flex flex-col gap-2">
            <label
              htmlFor="create-dgl-link-name"
              className="text-sm font-semibold leading-5 text-foreground"
            >
              {copy.linkNameLabel}
            </label>
            <Input
              id="create-dgl-link-name"
              value={linkName}
              onChange={(event) => {
                setLinkName(event.target.value)
                if (errors.linkName) {
                  setErrors((prev) => ({ ...prev, linkName: undefined }))
                }
              }}
              placeholder={copy.linkNamePlaceholder}
              disabled={busy}
              maxLength={copy.linkNameMaxLength}
              aria-invalid={errors.linkName ? true : undefined}
              className="h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm shadow-none placeholder:text-guest-feedback-placeholder md:text-sm dark:bg-transparent"
            />
            {errors.linkName ? (
              <p className="text-sm text-destructive" role="alert">
                {errors.linkName}
              </p>
            ) : null}
          </div>

          <div className="flex min-h-[176px] flex-col gap-2">
            <label
              htmlFor="create-dgl-description"
              className="text-sm font-semibold leading-5 text-foreground"
            >
              {copy.internalDescriptionLabel}
            </label>
            <Textarea
              id="create-dgl-description"
              value={internalDescription}
              onChange={(event) => {
                setInternalDescription(event.target.value)
                if (errors.internalDescription) {
                  setErrors((prev) => ({
                    ...prev,
                    internalDescription: undefined,
                  }))
                }
              }}
              placeholder={copy.internalDescriptionPlaceholder}
              disabled={busy}
              maxLength={copy.internalDescriptionMaxLength}
              aria-invalid={errors.internalDescription ? true : undefined}
              className="min-h-[140px] flex-1 rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm shadow-none placeholder:text-guest-feedback-placeholder focus-visible:border-ring md:text-sm dark:bg-transparent"
            />
            {errors.internalDescription ? (
              <p className="text-sm text-destructive" role="alert">
                {errors.internalDescription}
              </p>
            ) : null}
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-sm font-semibold leading-5 text-foreground">
              {copy.channelLabel}
            </span>
            <Select
              value={channel || undefined}
              onValueChange={(value) => {
                setChannel(value as CaptureDigitalGuestLinkChannel)
                if (errors.channel) {
                  setErrors((prev) => ({ ...prev, channel: undefined }))
                }
              }}
              disabled={busy}
            >
              <SelectTrigger
                aria-invalid={errors.channel ? true : undefined}
                className="h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm shadow-none data-placeholder:text-guest-feedback-placeholder dark:bg-transparent dark:hover:bg-transparent"
              >
                <SelectValue placeholder={copy.channelPlaceholder} />
              </SelectTrigger>
              <SelectContent>
                {DIGITAL_GUEST_LINK_CHANNEL_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.channel ? (
              <p className="text-sm text-destructive" role="alert">
                {errors.channel}
              </p>
            ) : null}
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-sm font-semibold leading-5 text-foreground">
              {copy.guestFormLabel}
            </span>
            <div className="flex min-h-[50px] items-center rounded border border-op-input-border bg-black/20 px-[15px] py-[15px] text-sm text-guest-feedback-placeholder">
              {copy.guestFormValue}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-sm font-semibold leading-5 text-foreground">
              {copy.connectedOfferLabel}
            </span>
            <div
              aria-disabled
              className="flex min-h-[50px] items-center justify-between rounded border border-op-input-border px-[15px] py-[15px] text-sm text-guest-feedback-placeholder opacity-60"
            >
              <span>{copy.connectedOfferPlaceholder}</span>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-sm font-semibold leading-5 text-foreground">
              {copy.statusLabel}
            </span>
            <Select
              value={status}
              onValueChange={(value) => {
                setStatus(value as CapturePlacementStatus)
              }}
              disabled={busy}
            >
              <SelectTrigger className="h-auto min-h-[50px] w-full rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm shadow-none dark:bg-transparent dark:hover:bg-transparent">
                <SelectValue placeholder={copy.statusPlaceholder} />
              </SelectTrigger>
              <SelectContent>
                {DIGITAL_GUEST_LINK_STATUS_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <DialogFooter className="flex-row gap-3 sm:justify-start">
          <Button
            type="button"
            variant="op-primary"
            disabled={
              busy
              || (requireLocationSelect && selectedLocationId == null)
            }
            onClick={() => {
              const nextErrors = validate(
                linkName,
                internalDescription,
                channel,
                {
                  requireLocation: requireLocationSelect,
                  locationId: selectedLocationId,
                }
              )
              if (Object.keys(nextErrors).length > 0) {
                setErrors(nextErrors)
                return
              }
              if (channel === "") {
                return
              }
              void (async () => {
                const result = await onSubmit({
                  linkName: linkName.trim(),
                  internalDescription:
                    internalDescription.trim().length > 0
                      ? internalDescription.trim()
                      : null,
                  channel,
                  status,
                  ...(selectedLocationId != null
                    ? { locationId: selectedLocationId }
                    : {}),
                })
                if (result === "created") {
                  onOpenChange(false)
                  return
                }
                if (result === "duplicate_link_name") {
                  setErrors({
                    linkName: copy.linkNameDuplicate,
                  })
                }
              })()
            }}
          >
            {copy.submitCta}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            disabled={busy}
            onClick={() => {
              handleOpenChange(false)
            }}
          >
            {copy.cancelCta}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
