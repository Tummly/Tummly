import { Loader2Icon, XIcon } from "lucide-react"
import { useEffect, useState, type ReactNode } from "react"

import { AiIcon } from "@/components/ui/ai-icon"
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
import {
  OPERATOR_WIZARD_SHELL_BODY_CLASS,
  formatOperatorWizardLastSavedLabel,
} from "@/lib/operatorUi/operatorWizardChromePresentation"
import { cn } from "@/lib/utils"

/** Send / record confirm — Figma `4577:39066` (Main Bg #171717 dark / surface-secondary light). */
const SEND_CONFIRM_CONTENT_CLASS =
  "z-[140] gap-[60px] rounded-op-md border-0 bg-op-surface-secondary p-8 text-op-text-primary shadow-lg sm:max-w-[520px] dark:bg-[var(--op-color-gray-1000)]"

const SEND_CONFIRM_HEADER_ROW_CLASS = "flex items-start gap-[22px]"

const SEND_CONFIRM_TITLE_CLASS =
  "pr-0 text-2xl font-bold leading-normal tracking-normal text-op-text-primary"

const SEND_CONFIRM_DESCRIPTION_CLASS =
  "max-w-[395px] text-sm font-medium leading-[18px] tracking-normal text-[var(--op-color-gray-550)]"

const SEND_CONFIRM_FOOTER_CLASS =
  "flex flex-row flex-wrap items-center justify-start gap-3"

/**
 * Domain-neutral Operator wizard shell: full-screen close header, page title +
 * meta, stepper, step heading, loading status, mid-flow footer (Back · Last
 * saved · Save and exit · primary), optional Preparing AI overlay, and
 * optional send/schedule/record confirm dialog.
 *
 * Feature-owned step bodies are `children`; primary CTA(s) are `footer`.
 * Callers supply preparing and confirm copy when those slots are used.
 */

export type OperatorWizardStepLabel = {
  id: string
  label: string
}

export type OperatorWizardPreparingOverlayProps = {
  open: boolean
  onDismiss: () => void
  onWriteManually: () => void
  /** Top-left meta (e.g. feedback · location · touchpoint). */
  subtitle?: string | null
  /** Caller-owned — shell has no domain preparing defaults. */
  title: string
  /** Caller-owned — shell has no domain preparing defaults. */
  description: string
}

export type OperatorWizardConfirmDialogProps = {
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

export type OperatorWizardShellProps = {
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
  /** Step name under the stepper (mid-flow). Omit on success / goal step 0. */
  stepHeading?: ReactNode | null
  /** Helper under step heading (e.g. Response setup description). */
  stepDescription?: ReactNode | null
  /** Omit (null) to hide the stepper — Goal-style step 0 and success. */
  steps?: readonly OperatorWizardStepLabel[] | null
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
  /** Omit when unused (no AI draft step). */
  preparingOverlay?: OperatorWizardPreparingOverlayProps | null
  /** Omit when unused (no send/schedule/record confirm). */
  confirmDialog?: OperatorWizardConfirmDialogProps | null
}

export function OperatorWizardShell({
  isOpen,
  onRequestClose,
  closeDisabled = false,
  showBackButton,
  onBack,
  backDisabled = false,
  title,
  description,
  descriptionSrOnly = false,
  descriptionClassName,
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
  preparingOverlay = null,
  confirmDialog = null,
}: OperatorWizardShellProps) {
  const [openedAt, setOpenedAt] = useState(() => new Date())

  useEffect(() => {
    if (isOpen) {
      setOpenedAt(new Date())
    }
  }, [isOpen])

  const lastSavedLabel = formatOperatorWizardLastSavedLabel(
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

          {/* Full-bleed scroll: content + footer share one track at the screen edge. */}
          <div className={OPERATOR_WIZARD_SHELL_BODY_CLASS}>
            <div className="flex min-h-full flex-col">
              {/* Figma 1728 frame uses 200px side inset; scale down on narrower viewports. */}
              <div className="flex flex-1 flex-col px-4 pb-24 pt-10 sm:px-6 sm:pt-[60px] md:px-[100px] min-[1728px]:px-[200px]">
                <DialogTitle className="text-[28px] font-bold leading-normal tracking-normal text-op-text-primary sm:text-[32px]">
                  {title}
                </DialogTitle>
                <DialogDescription
                  className={cn(
                    "mt-2 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]",
                    descriptionClassName,
                    descriptionSrOnly && "sr-only"
                  )}
                >
                  {description}
                </DialogDescription>

                {steps != null ? (
                  <ol
                    className="mt-[52px] mb-0 flex w-full list-none items-center gap-3 p-0"
                    aria-label={`Step ${activeStepIndex + 1} of ${steps.length}`}
                  >
                    {steps.map((step, index) => {
                      const done = index < activeStepIndex
                      const current = index === activeStepIndex
                      const reached = done || current
                      /** Figma: connector after the current step stays white. */
                      const lineReached = index <= activeStepIndex

                      return (
                        <li key={step.id} className="contents">
                          <span
                            aria-current={current ? "step" : undefined}
                            className={cn(
                              "shrink-0 whitespace-nowrap font-medium leading-5",
                              current
                                ? "text-base text-op-text-primary"
                                : "text-sm",
                              !current
                                && reached
                                && "text-op-text-primary",
                              !reached
                                && "text-[var(--op-color-gray-550)]"
                            )}
                          >
                            {index + 1}. {step.label}
                          </span>
                          {index < steps.length - 1 ? (
                            <span
                              aria-hidden
                              className={cn(
                                "h-0.5 min-w-px flex-1",
                                lineReached
                                  ? "bg-op-text-primary"
                                  : "bg-op-card-border"
                              )}
                            />
                          ) : null}
                        </li>
                      )
                    })}
                  </ol>
                ) : null}

                {stepHeading != null && !isLoading ? (
                  <div className="mt-[52px] flex flex-col gap-2">
                    <h2 className="text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
                      {stepHeading}
                    </h2>
                    {stepDescription != null ? (
                      <p className="text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
                        {stepDescription}
                      </p>
                    ) : null}
                  </div>
                ) : null}

                {isLoading ? (
                  <div
                    className="flex flex-1 items-center justify-center py-24"
                    role="status"
                    aria-live="polite"
                    aria-label={loadingLabel}
                  >
                    <Spinner size="md" aria-hidden />
                  </div>
                ) : null}

                {!isLoading && children != null ? (
                  <div className={cn(stepHeading != null ? "mt-7" : "mt-10")}>
                    {children}
                  </div>
                ) : null}
              </div>

              <div className="border-t border-op-card-border bg-op-surface-secondary px-4 py-6 sm:px-6 md:px-[100px] min-[1728px]:px-[200px]">
                <div className="flex flex-wrap items-center justify-between gap-3">
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
                        <div className="flex items-center gap-3 text-sm font-medium text-[var(--op-color-gray-550)]">
                          <span
                            aria-hidden
                            className="size-3 shrink-0 rounded-full bg-[var(--op-color-gray-550)]"
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
            overlayClassName="z-[150]"
            className="z-[150] w-full max-w-[min(100%-2rem,520px)] gap-0 rounded-[4px] border-op-card-border bg-op-surface-secondary p-0 text-op-text-primary shadow-none sm:max-w-[520px]"
          >
            <div className="flex flex-col items-center py-[22px]">
              <div className="flex w-full flex-col items-center gap-16">
                <div className="flex w-full items-center justify-between px-[22px] pb-[9px]">
                  <p className="min-w-0 flex-1 text-sm leading-5 text-[var(--op-color-gray-550)]">
                    {preparingOverlay.subtitle ?? "\u00a0"}
                  </p>
                  <Button
                    type="button"
                    variant="op-collapse"
                    aria-label="Dismiss"
                    onClick={preparingOverlay.onDismiss}
                  >
                    <XIcon className="size-[18px]" aria-hidden />
                  </Button>
                </div>

                <div className="flex w-full flex-col items-center justify-center gap-6">
                  <div className="flex flex-col items-center gap-4">
                    <div
                      role="status"
                      aria-live="polite"
                      aria-label={preparingOverlay.title}
                    >
                      <AiIcon size={48} className="animate-spin" />
                    </div>
                    <DialogTitle
                      className="bg-gradient-to-r from-[#14a946] to-[#135acc] bg-clip-text pr-0 text-center text-2xl font-medium tracking-normal text-transparent dark:text-transparent"
                    >
                      {preparingOverlay.title}
                    </DialogTitle>
                  </div>
                  <DialogDescription className="max-w-[365px] px-[7px] text-center text-base leading-[22px] font-normal tracking-normal text-[var(--op-color-gray-550)] dark:text-[var(--op-color-gray-550)]">
                    {preparingOverlay.description}
                  </DialogDescription>
                </div>

                <div className="flex items-start justify-center">
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="h-[42px] min-h-[42px] px-[17px]"
                    onClick={preparingOverlay.onWriteManually}
                  >
                    Write manually
                  </Button>
                </div>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      ) : null}

      {confirmDialog != null ? (
        <Dialog
          open={confirmDialog.open}
          onOpenChange={(open) => {
            if (!open && !confirmDialog.busy) {
              confirmDialog.onCancel()
            }
          }}
        >
          <DialogContent
            showCloseButton={false}
            overlayClassName="z-[140]"
            className={SEND_CONFIRM_CONTENT_CLASS}
          >
            <div className={SEND_CONFIRM_HEADER_ROW_CLASS}>
              <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
                <DialogTitle className={SEND_CONFIRM_TITLE_CLASS}>
                  {confirmDialog.title}
                </DialogTitle>
                <DialogDescription className={SEND_CONFIRM_DESCRIPTION_CLASS}>
                  {confirmDialog.description}
                </DialogDescription>
              </DialogHeader>
              {!confirmDialog.busy ? (
                <Button
                  type="button"
                  variant="op-collapse"
                  aria-label="Close"
                  className="shrink-0"
                  onClick={confirmDialog.onCancel}
                >
                  <XIcon className="size-[18px]" aria-hidden />
                </Button>
              ) : null}
            </div>
            {confirmDialog.error != null ? (
              <p
                className="text-sm font-medium text-[var(--op-color-red-550)]"
                role="alert"
              >
                {confirmDialog.error}
              </p>
            ) : null}
            <DialogFooter className={SEND_CONFIRM_FOOTER_CLASS}>
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
              <Button
                type="button"
                variant="op-tertiary"
                disabled={confirmDialog.busy}
                onClick={confirmDialog.onCancel}
              >
                {confirmDialog.cancelLabel ?? "Cancel"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      ) : null}
    </>
  )
}
