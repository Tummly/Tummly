import { ArrowLeftIcon, CheckIcon, Loader2Icon, XIcon } from "lucide-react"
import type { ReactNode } from "react"

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
import { cn } from "@/lib/utils"

/**
 * Shared chrome for the four Feedback recovery intent wizards: full-screen
 * header (back / close), stepper, loading status, footer action bar,
 * Preparing-AI-draft overlay, and the final send/record confirm dialog.
 *
 * Intent-specific step bodies are passed in as `children` / `footer`; only
 * the chrome that was previously copy-pasted across wizards lives here.
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
  title: ReactNode
  description: ReactNode
  descriptionSrOnly?: boolean
  descriptionClassName?: string
  /** Omit (null) to hide the stepper, e.g. on the success step. */
  steps?: readonly RecoveryWizardStepLabel[] | null
  activeStepIndex?: number
  isLoading: boolean
  loadingLabel?: string
  children?: ReactNode
  footer: ReactNode
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
  steps,
  activeStepIndex = 0,
  isLoading,
  loadingLabel = "Loading…",
  children,
  footer,
  preparingOverlay,
  confirmDialog,
}: RecoveryWizardShellProps) {
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
          <div className="flex w-full shrink-0 items-center justify-between gap-3 p-6">
            {showBackButton ? (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Back"
                disabled={backDisabled}
                className="rounded-[2px] bg-op-button-collapse-background text-op-text-primary hover:bg-op-button-collapse-hover hover:opacity-100"
                onClick={onBack}
              >
                <ArrowLeftIcon className="size-[18px]" aria-hidden />
              </Button>
            ) : (
              <span className="size-9" />
            )}
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
            <div className="mx-auto flex min-h-0 w-full max-w-[1440px] flex-1 flex-col overflow-y-auto px-6 pb-28 pt-[40px] md:px-[100px] xl:px-[200px]">
              {steps != null ? (
                <ol className="mb-8 flex flex-wrap gap-4">
                  {steps.map((step, index) => {
                    const done = index < activeStepIndex
                    const current = index === activeStepIndex
                    return (
                      <li
                        key={step.id}
                        className={cn(
                          "flex items-center gap-2 text-sm font-medium",
                          current || done
                            ? "text-op-text-primary"
                            : "text-op-text-muted"
                        )}
                      >
                        <span
                          className={cn(
                            "flex size-6 items-center justify-center rounded-full text-xs",
                            current || done
                              ? "bg-op-text-primary text-op-surface-secondary"
                              : "bg-op-card-border text-op-text-muted"
                          )}
                        >
                          {done ? (
                            <CheckIcon className="size-3.5" aria-hidden />
                          ) : (
                            index + 1
                          )}
                        </span>
                        {step.label}
                      </li>
                    )
                  })}
                </ol>
              ) : null}

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

              {children}
            </div>

            <div className="shrink-0 border-t border-op-card-border bg-[var(--op-color-gray-995)] px-6 py-4 md:px-[100px] xl:px-[200px]">
              <div className="mx-auto flex w-full max-w-[1440px] flex-wrap items-center justify-between gap-3">
                {footer}
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
