import { Loader2Icon, XIcon } from "lucide-react"
import { useEffect, useState, type ReactNode } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Spinner } from "@/components/ui/spinner"
import { formatRecoveryLastSavedLabel } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import { cn } from "@/lib/utils"

/**
 * Shared chrome for the four Feedback recovery intent wizards: full-screen
 * close header, page title + meta, stepper, step heading, loading status,
 * mid-flow footer (Back · Last saved · Save and exit · primary), Preparing
 * AI draft overlay, and the final send/record confirm dialog.
 *
 * Intent-specific step bodies are passed in as `children`; primary CTA(s)
 * as `footer`. Mid-flow chrome is owned here so intents do not fork it.
 */

export type RecoveryWizardStepLabel = {
  id: string
  label: string
}

export type RecoveryWizardPreparingOverlayProps = {
  open: boolean
  onDismiss: () => void
  onWriteManually: () => void
  title?: string
  description?: string
}

export type RecoveryWizardConfirmDialogProps = {
  open: boolean
  busy: boolean
  onCancel: () => void
  onConfirm: () => void
  title: ReactNode
  description: ReactNode
  error?: string | null
  cancelLabel?: string
  confirmLabel: ReactNode
  confirmBusyLabel: ReactNode
}

export type RecoveryWizardShellProps = {
  isOpen: boolean
  /** Called when the main dialog is dismissed (X / overlay / Esc) and closing is allowed. */
  onRequestClose: () => void
  /** When true, the main dialog cannot be dismissed at all (e.g. a send is in flight). */
  closeDisabled?: boolean
  showBackButton: boolean
  onBack?: () => void
  backDisabled?: boolean
  /** Page title — mid-flow stays "Start recovery"; success uses the outcome title. */
  title: ReactNode
  description: ReactNode
  descriptionSrOnly?: boolean
  descriptionClassName?: string
  /** Step name under the stepper (mid-flow). Omit on success. */
  stepHeading?: ReactNode | null
  /** Helper under step heading (e.g. Response setup description). */
  stepDescription?: ReactNode | null
  /** Omit (null) to hide the stepper, e.g. on the success step. */
  steps?: readonly RecoveryWizardStepLabel[] | null
  activeStepIndex?: number
  isLoading: boolean
  loadingLabel?: string
  children?: ReactNode
  /**
   * Mid-flow: primary CTA only (Continue / Send). Success: Keep in progress
   * + Mark resolved. Shell owns Back / Last saved / Save and exit when
   * `footerLayout` is `wizard`.
   */
  footer: ReactNode
  /** `wizard` = Figma mid-flow footer; `end` = success actions only. */
  footerLayout?: "wizard" | "end"
  onSaveAndExit?: () => void
  saveAndExitDisabled?: boolean
  /** Override for Last saved; defaults to dialog open time. */
  lastSavedAt?: Date | null
  /** Omit (null) for wizards with no AI draft step, e.g. Record internal action only. */
  preparingOverlay?: RecoveryWizardPreparingOverlayProps | null
  confirmDialog: RecoveryWizardConfirmDialogProps
}

const PREPARING_OVERLAY_DEFAULT_TITLE = "Preparing AI Draft"
const PREPARING_OVERLAY_DEFAULT_DESCRIPTION =
  "We are preparing a draft response. You can write manually instead, or dismiss this dialog while preparation continues."

export function RecoveryWizardShell({
  isOpen,
  onRequestClose,
  closeDisabled = false,
  showBackButton,
  onBack,
  backDisabled = false,
  title,
  description,
  descriptionSrOnly = false,
  descriptionClassName = "max-w-[520px]",
  stepHeading = null,
  stepDescription = null,
  steps,
  activeStepIndex = 0,
  isLoading,
  loadingLabel = "Loading…",
  children,
  footer,
  footerLayout = "wizard",
  onSaveAndExit,
  saveAndExitDisabled = false,
  lastSavedAt = null,
  preparingOverlay,
  confirmDialog,
}: RecoveryWizardShellProps) {
  const [openedAt, setOpenedAt] = useState(() => new Date())

  useEffect(() => {
    if (isOpen) {
      setOpenedAt(new Date())
    }
  }, [isOpen])

  const lastSavedLabel = formatRecoveryLastSavedLabel(
    lastSavedAt ?? openedAt
  )

  return (
    <>
      <Dialog
        open={isOpen}
        onOpenChange={(open) => {
          if (!open) {
            if (closeDisabled) {
              return
            }
            onRequestClose()
          }
        }}
      >
        <DialogContent
          showCloseButton={false}
          className={cn(
            "fixed inset-0 top-0 left-0 z-[130] flex h-dvh max-h-dvh w-screen max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-none border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-none sm:max-w-none",
            "data-open:zoom-in-100 data-closed:zoom-out-100"
          )}
        >
          <div className="flex w-full shrink-0 items-center justify-end p-6">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Close"
              disabled={closeDisabled}
              className="rounded-[2px] bg-op-button-collapse-background text-op-text-primary hover:bg-op-button-collapse-hover hover:opacity-100"
              onClick={onRequestClose}
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
          </div>

          <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-t-[20px] border-t border-op-card-border bg-[var(--op-color-gray-995)]">
            <div className="mx-auto flex min-h-0 w-full max-w-[1440px] flex-1 flex-col overflow-y-auto px-6 pb-28 pt-[60px] md:px-[100px] xl:px-[200px]">
              <DialogTitle className="text-[32px] font-bold leading-normal tracking-normal text-op-text-primary">
                {title}
              </DialogTitle>
              <DialogDescription
                className={cn(
                  "mt-2 text-sm font-medium leading-5 text-op-text-muted",
                  descriptionClassName,
                  descriptionSrOnly && "sr-only"
                )}
              >
                {description}
              </DialogDescription>

              {steps != null ? (
                <ol className="mt-[52px] mb-0 flex flex-wrap items-center gap-3">
                  {steps.map((step, index) => {
                    const done = index < activeStepIndex
                    const current = index === activeStepIndex
                    const active = current || done
                    return (
                      <li
                        key={step.id}
                        className={cn(
                          "flex min-w-0 flex-1 items-center gap-3 text-sm font-medium",
                          index === 0 && "flex-none",
                          active
                            ? "text-op-text-primary"
                            : "text-op-text-muted",
                          current && "text-base"
                        )}
                      >
                        <span className="whitespace-nowrap">
                          {index + 1}. {step.label}
                        </span>
                        {index < steps.length - 1 ? (
                          <span
                            aria-hidden
                            className={cn(
                              "h-0 min-w-[24px] flex-1 border-t-2",
                              done
                                ? "border-op-text-primary"
                                : "border-op-card-border"
                            )}
                          />
                        ) : null}
                      </li>
                    )
                  })}
                </ol>
              ) : null}

              {stepHeading != null ? (
                <div className="mt-[52px] flex flex-col gap-2">
                  <h2 className="text-[22px] font-semibold leading-normal text-op-text-primary">
                    {stepHeading}
                  </h2>
                  {stepDescription != null ? (
                    <p className="max-w-[520px] text-sm font-medium leading-5 text-op-text-muted">
                      {stepDescription}
                    </p>
                  ) : null}
                </div>
              ) : null}

              {isLoading ? (
                <div className="mt-12 flex items-center gap-2">
                  <Spinner
                    size="sm"
                    aria-live="polite"
                    aria-label={loadingLabel}
                  />
                  <span className="text-sm text-op-text-muted" aria-hidden>
                    {loadingLabel}
                  </span>
                </div>
              ) : null}

              {children != null ? (
                <div className={cn(stepHeading != null ? "mt-7" : "mt-10")}>
                  {children}
                </div>
              ) : null}
            </div>

            <div className="shrink-0 border-t border-op-card-border bg-op-surface-secondary px-6 py-6 md:px-[100px] xl:px-[200px]">
              <div className="mx-auto flex w-full max-w-[1440px] flex-wrap items-center justify-between gap-3">
                {footerLayout === "wizard" ? (
                  <>
                    <div className="flex flex-wrap items-center gap-[18px]">
                      {showBackButton ? (
                        <Button
                          type="button"
                          variant="op-secondary"
                          disabled={backDisabled}
                          onClick={onBack}
                        >
                          Back
                        </Button>
                      ) : null}
                      <div className="flex items-center gap-3 text-sm font-medium text-op-text-muted">
                        <span
                          aria-hidden
                          className="size-3 shrink-0 rounded-full bg-op-text-muted"
                        />
                        <span>{lastSavedLabel}</span>
                      </div>
                    </div>
                    <div className="flex flex-wrap items-center justify-end gap-3">
                      {onSaveAndExit != null ? (
                        <Button
                          type="button"
                          variant="op-tertiary"
                          disabled={saveAndExitDisabled}
                          onClick={onSaveAndExit}
                        >
                          Save and exit
                        </Button>
                      ) : null}
                      {footer}
                    </div>
                  </>
                ) : (
                  <div className="ml-auto flex flex-wrap items-center justify-end gap-3">
                    {footer}
                  </div>
                )}
              </div>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {preparingOverlay != null ? (
        <Dialog
          open={preparingOverlay.open}
          onOpenChange={(open) => {
            if (!open) {
              preparingOverlay.onDismiss()
            }
          }}
        >
          <DialogContent
            showCloseButton={false}
            className="z-[150] max-w-md border-op-card-border bg-[var(--op-color-gray-995)] text-op-text-primary"
          >
            <div className="absolute top-4 right-4">
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Dismiss"
                className="rounded-[2px]"
                onClick={preparingOverlay.onDismiss}
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </div>
            <DialogHeader className="items-center text-center sm:text-center">
              <Spinner
                size="md"
                className="mb-2 text-op-text-primary"
                aria-live="polite"
                aria-label={
                  preparingOverlay.title ?? PREPARING_OVERLAY_DEFAULT_TITLE
                }
              />
              <DialogTitle>
                {preparingOverlay.title ?? PREPARING_OVERLAY_DEFAULT_TITLE}
              </DialogTitle>
              <DialogDescription className="text-op-text-muted">
                {preparingOverlay.description
                  ?? PREPARING_OVERLAY_DEFAULT_DESCRIPTION}
              </DialogDescription>
            </DialogHeader>
            <DialogFooter className="sm:justify-center">
              <Button
                type="button"
                variant="op-secondary"
                onClick={preparingOverlay.onWriteManually}
              >
                Write manually
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      ) : null}

      <Dialog
        open={confirmDialog.open}
        onOpenChange={(open) => {
          if (!open && !confirmDialog.busy) {
            confirmDialog.onCancel()
          }
        }}
      >
        <DialogContent
          showCloseButton={!confirmDialog.busy}
          className="z-[140] max-w-md border-op-card-border bg-[var(--op-color-gray-995)] text-op-text-primary"
        >
          <DialogHeader>
            <DialogTitle>{confirmDialog.title}</DialogTitle>
            <DialogDescription className="text-op-text-muted">
              {confirmDialog.description}
            </DialogDescription>
          </DialogHeader>
          {confirmDialog.error != null ? (
            <p className="text-sm text-destructive" role="alert">
              {confirmDialog.error}
            </p>
          ) : null}
          <DialogFooter className="gap-2 sm:justify-end">
            <Button
              type="button"
              variant="op-secondary"
              disabled={confirmDialog.busy}
              onClick={confirmDialog.onCancel}
            >
              {confirmDialog.cancelLabel ?? "Cancel"}
            </Button>
            <Button
              type="button"
              variant="op-primary"
              disabled={confirmDialog.busy}
              onClick={confirmDialog.onConfirm}
            >
              {confirmDialog.busy ? (
                <Loader2Icon className="size-4 animate-spin" aria-hidden />
              ) : null}
              {confirmDialog.busy
                ? confirmDialog.confirmBusyLabel
                : confirmDialog.confirmLabel}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
