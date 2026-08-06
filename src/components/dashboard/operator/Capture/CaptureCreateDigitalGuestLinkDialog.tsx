import { XIcon } from "lucide-react"
import { useEffect } from "react"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"

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
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
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
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_FIELD_TRIGGER_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
  CAPTURE_DIALOG_SELECT_ITEM_CLASS,
  CAPTURE_DIALOG_SELECT_MENU_CLASS,
  DIGITAL_GUEST_LINK_CHANNEL_OPTIONS,
  DIGITAL_GUEST_LINK_STATUS_OPTIONS,
  OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { CreateDigitalGuestLinkModuleInput } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  createDigitalGuestLinkFormSchemaWithLocation,
  type CreateDigitalGuestLinkFormValues,
} from "@/schemas/createDigitalGuestLink"
import type {
  CaptureDigitalGuestLinkChannel,
  CapturePlacementStatus,
} from "@/types/dashboard"

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

const fieldTriggerClass = CAPTURE_DIALOG_FIELD_TRIGGER_CLASS

const fieldInputClass =
  "h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-text-muted md:text-sm dark:bg-transparent"

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
  const showLocationSelect =
    locationOptions != null && locationOptions.length > 0
  const requireLocationSelect = showLocationSelect && !locationBound
  const boundLocationLabel =
    locationBound && selectedLocationId != null
      ? (locationOptions?.find((option) => option.id === selectedLocationId)
          ?.label ?? String(selectedLocationId))
      : null

  const form = useForm<CreateDigitalGuestLinkFormValues>({
    resolver: zodResolver(
      createDigitalGuestLinkFormSchemaWithLocation(requireLocationSelect)
    ),
    defaultValues: {
      linkName: "",
      internalDescription: "",
      channel: "",
      status: "Active",
      locationId: selectedLocationId,
    },
  })

  useEffect(() => {
    if (!open) {
      return
    }
    form.reset({
      linkName: prefill?.linkName ?? "",
      internalDescription: "",
      channel: prefill?.channel ?? "",
      status: prefill?.status ?? "Active",
      locationId: selectedLocationId,
    })
  }, [open, prefill, selectedLocationId, form])

  const handleOpenChange = (nextOpen: boolean) => {
    if (busy && !nextOpen) {
      return
    }
    onOpenChange(nextOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="max-h-[90vh] gap-[60px] overflow-y-auto bg-op-surface-secondary p-8 text-op-text-primary sm:max-w-[642px]"
      >
        <Form {...form}>
          <form
            className="flex flex-col gap-[60px]"
            onSubmit={form.handleSubmit(async (values) => {
              if (values.channel === "") {
                return
              }
              const result = await onSubmit({
                linkName: values.linkName,
                internalDescription:
                  values.internalDescription.trim().length > 0
                    ? values.internalDescription.trim()
                    : null,
                channel: values.channel,
                status: values.status,
                ...(values.locationId != null
                  ? { locationId: values.locationId }
                  : {}),
              })
              if (result === "created") {
                onOpenChange(false)
                return
              }
              if (result === "duplicate_link_name") {
                form.setError("linkName", {
                  type: "server",
                  message: copy.linkNameDuplicate,
                })
              }
            })}
          >
            <div className="flex flex-col gap-5">
              <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
                <DialogHeader className="min-w-0 flex-1 gap-3">
                  <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                    {copy.title}
                  </DialogTitle>
                  <DialogDescription className="max-w-[441px] text-sm font-medium leading-[18px] text-op-text-muted">
                    {copy.description}
                  </DialogDescription>
                </DialogHeader>
                <DialogClose asChild>
                  <Button
                    type="button"
                    variant="op-collapse"
                    disabled={busy}
                    aria-label="Close"
                    className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                  >
                    <XIcon aria-hidden />
                  </Button>
                </DialogClose>
              </div>

              {showLocationSelect ? (
                locationBound ? (
                  <div className="flex flex-col gap-2">
                    <span className="text-sm font-semibold leading-5 text-op-text-primary">
                      {copy.locationLabel}
                    </span>
                    <div className="flex min-h-[50px] items-center rounded border border-op-input-border bg-op-background-secondary px-[15px] py-[15px] text-sm text-op-text-primary">
                      {boundLocationLabel}
                    </div>
                  </div>
                ) : (
                  <FormField
                    control={form.control}
                    name="locationId"
                    render={({ field }) => (
                      <FormItem className="flex flex-col gap-2">
                        <FormLabel className="text-sm font-semibold leading-5 text-op-text-primary">
                          {copy.locationLabel}
                        </FormLabel>
                        <Select
                          value={
                            field.value != null ? String(field.value) : undefined
                          }
                          onValueChange={(value) => {
                            const nextId = Number(value)
                            if (!Number.isFinite(nextId)) {
                              return
                            }
                            field.onChange(nextId)
                            onLocationIdChange?.(nextId)
                          }}
                          disabled={busy}
                        >
                          <FormControl>
                            <SelectTrigger className={fieldTriggerClass}>
                              <SelectValue
                                placeholder={copy.locationPlaceholder}
                              />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent
                            position="popper"
                            align="start"
                            className={CAPTURE_DIALOG_SELECT_MENU_CLASS}
                          >
                            {locationOptions.map((option) => (
                              <SelectItem
                                key={option.id}
                                value={String(option.id)}
                                className={CAPTURE_DIALOG_SELECT_ITEM_CLASS}
                              >
                                {option.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                )
              ) : null}

              <FormField
                control={form.control}
                name="linkName"
                render={({ field }) => (
                  <FormItem className="flex flex-col gap-2">
                    <FormLabel className="text-sm font-semibold leading-5 text-op-text-primary">
                      {copy.linkNameLabel}
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        placeholder={copy.linkNamePlaceholder}
                        disabled={busy}
                        maxLength={copy.linkNameMaxLength}
                        className={fieldInputClass}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="internalDescription"
                render={({ field }) => (
                  <FormItem className="flex min-h-[176px] flex-col gap-2">
                    <FormLabel className="text-sm font-semibold leading-5 text-op-text-primary">
                      {copy.internalDescriptionLabel}
                    </FormLabel>
                    <FormControl>
                      <Textarea
                        {...field}
                        placeholder={copy.internalDescriptionPlaceholder}
                        disabled={busy}
                        maxLength={copy.internalDescriptionMaxLength}
                        className="min-h-[140px] flex-1 rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-text-muted focus-visible:border-ring md:text-sm dark:bg-transparent"
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="channel"
                render={({ field }) => (
                  <FormItem className="flex flex-col gap-2">
                    <FormLabel className="text-sm font-semibold leading-5 text-op-text-primary">
                      {copy.channelLabel}
                    </FormLabel>
                    <Select
                      value={field.value || undefined}
                      onValueChange={field.onChange}
                      disabled={busy}
                    >
                      <FormControl>
                        <SelectTrigger className={fieldTriggerClass}>
                          <SelectValue placeholder={copy.channelPlaceholder} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent
                        position="popper"
                        align="start"
                        className={CAPTURE_DIALOG_SELECT_MENU_CLASS}
                      >
                        {DIGITAL_GUEST_LINK_CHANNEL_OPTIONS.map((option) => (
                          <SelectItem
                            key={option.value}
                            value={option.value}
                            className={CAPTURE_DIALOG_SELECT_ITEM_CLASS}
                          >
                            {option.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="flex flex-col gap-2">
                <span className="text-sm font-semibold leading-5 text-op-text-primary">
                  {copy.guestFormLabel}
                </span>
                <div className="flex min-h-[50px] items-center rounded border border-op-input-border bg-op-background-secondary px-[15px] py-[15px] text-sm text-op-text-muted">
                  {copy.guestFormValue}
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <span className="text-sm font-semibold leading-5 text-op-text-primary">
                  {copy.connectedOfferLabel}
                </span>
                <div
                  aria-disabled
                  className="flex min-h-[50px] items-center justify-between rounded border border-op-input-border px-[15px] py-[15px] text-sm text-op-text-muted opacity-60"
                >
                  <span>{copy.connectedOfferPlaceholder}</span>
                </div>
              </div>

              <FormField
                control={form.control}
                name="status"
                render={({ field }) => (
                  <FormItem className="flex flex-col gap-2">
                    <FormLabel className="text-sm font-semibold leading-5 text-op-text-primary">
                      {copy.statusLabel}
                    </FormLabel>
                    <Select
                      value={field.value}
                      onValueChange={field.onChange}
                      disabled={busy}
                    >
                      <FormControl>
                        <SelectTrigger className={fieldTriggerClass}>
                          <SelectValue placeholder={copy.statusPlaceholder} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent
                        position="popper"
                        align="start"
                        className={CAPTURE_DIALOG_SELECT_MENU_CLASS}
                      >
                        {DIGITAL_GUEST_LINK_STATUS_OPTIONS.map((option) => (
                          <SelectItem
                            key={option.value}
                            value={option.value}
                            className={CAPTURE_DIALOG_SELECT_ITEM_CLASS}
                          >
                            {option.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <DialogFooter className="flex-row gap-3 sm:justify-start">
              <Button
                type="submit"
                variant="op-primary"
                disabled={
                  busy
                  || (requireLocationSelect
                    && form.watch("locationId") == null)
                }
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
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
