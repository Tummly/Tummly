import { useState } from "react"

import stepAccount from "@/assets/operator-home/step-account.png"
import stepCampaign from "@/assets/operator-home/step-campaign.png"
import stepGuestForm from "@/assets/operator-home/step-guest-form.png"
import stepQr from "@/assets/operator-home/step-qr.png"
import stepResponse from "@/assets/operator-home/step-response.png"
import { Button } from "@/components/ui/button"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  readSetupChecklistOpen,
  writeSetupChecklistOpen,
} from "@/lib/operatorHome/setupChecklistOpen"
import { cn } from "@/lib/utils"
import type {
  OperatorHomeSetupStep,
  OperatorHomeSetupStepId,
} from "@/types/operatorHome"

const STEP_IMAGES: Record<OperatorHomeSetupStepId, string> = {
  "account-ready": stepAccount,
  "guest-form": stepGuestForm,
  "qr-placement": stepQr,
  "first-response": stepResponse,
  "first-offer": stepResponse,
  "first-campaign": stepCampaign,
}

const SETUP_ACCORDION_VALUE = "setup"

type OperatorHomeSetupChecklistProps = {
  steps: OperatorHomeSetupStep[]
  onPreviewGuestForm?: () => void
  previewBusy?: boolean
}

const outlineButtonClassName =
  "h-auto min-h-0 rounded-lg border border-foreground bg-transparent px-[17px] py-[11px] text-sm font-medium leading-5 text-foreground hover:bg-black/5 dark:hover:bg-white/10 disabled:opacity-50"

const primaryButtonClassName =
  "h-auto min-h-0 rounded-lg border-transparent bg-primary px-[17px] py-[11px] text-sm font-medium leading-5 text-primary-foreground hover:bg-primary/90 disabled:opacity-50"

/** Figma “Finish setting up Guest Loop” collapsible checklist. */
export function OperatorHomeSetupChecklist({
  steps,
  onPreviewGuestForm,
  previewBusy = false,
}: OperatorHomeSetupChecklistProps) {
  const [openValues, setOpenValues] = useState<string[]>(() =>
    readSetupChecklistOpen() ? [SETUP_ACCORDION_VALUE] : []
  )

  return (
    <section className="rounded-lg bg-[#e4f3e9] p-6 dark:bg-primary/15">
      <Accordion
        type="multiple"
        value={openValues}
        onValueChange={(next) => {
          setOpenValues(next)
          writeSetupChecklistOpen(next.includes(SETUP_ACCORDION_VALUE))
        }}
      >
        <AccordionItem value={SETUP_ACCORDION_VALUE} className="border-none">
          <AccordionTrigger
            className={cn(
              "items-center gap-4 py-0 hover:no-underline",
              "**:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:hidden"
            )}
          >
            <div className="flex min-w-0 flex-1 flex-col gap-3 text-left">
              <h2 className="text-xl font-bold text-foreground">
                Finish setting up Guest Loop
              </h2>
              <p className="text-sm font-normal text-foreground">
                Complete the remaining setup steps below. Offers and campaigns
                are optional next steps.
              </p>
            </div>
            <span
              className="flex size-[42px] shrink-0 items-center justify-center rounded-xl bg-white text-foreground dark:bg-white/10"
              aria-hidden
            >
              <svg
                viewBox="0 0 18 18"
                className={cn(
                  "size-[18px] transition-transform duration-200 motion-reduce:transition-none",
                  openValues.includes(SETUP_ACCORDION_VALUE) && "rotate-180"
                )}
                fill="none"
              >
                <path
                  d="M4.5 6.75L9 11.25L13.5 6.75"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
          </AccordionTrigger>

          <AccordionContent className="pt-[30px] pb-0">
            <ol className="flex flex-col gap-2.5">
              {steps.map((step) => {
                const isIncomplete = step.status === "incomplete"
                const isComplete = step.status === "complete"

                return (
                  <li
                    key={step.id}
                    className={cn(
                      "relative flex items-center gap-3.5 rounded-lg py-5 pr-5 pl-[30px]",
                      isIncomplete
                        ? "border-b border-[#e3e3e3] bg-white dark:border-white/10 dark:bg-white/8"
                        : "bg-white/30 dark:bg-white/10"
                    )}
                  >
                    <StatusMarker complete={isComplete} />

                    <img
                      src={STEP_IMAGES[step.id]}
                      alt=""
                      className="h-10 w-12 shrink-0 object-contain"
                    />

                    <div className="flex min-w-0 flex-1 flex-col gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
                      <div className="min-w-0 flex-1 pl-0 sm:pl-3">
                        <p className="text-base font-semibold tracking-[-0.4px] text-foreground">
                          {step.title}
                        </p>
                        <p className="text-xs leading-4 text-foreground/75">
                          {step.description}
                        </p>
                      </div>

                      {step.actions.length > 0 ? (
                        <div className="flex flex-wrap items-center gap-[18px]">
                          {step.actions.map((action) => {
                            const isPrimary =
                              action.id === "create-offer" ||
                              action.id === "create-campaign"
                            const isPreview =
                              action.id === "preview-guest-form"
                            const available = isPreview
                              ? action.available && !previewBusy
                              : action.available

                            return (
                              <Button
                                key={action.id}
                                type="button"
                                className={
                                  isPrimary
                                    ? primaryButtonClassName
                                    : outlineButtonClassName
                                }
                                disabled={!available}
                                aria-disabled={!available}
                                aria-label={
                                  available
                                    ? action.label
                                    : `${action.label} (unavailable)`
                                }
                                onClick={
                                  isPreview ? onPreviewGuestForm : undefined
                                }
                              >
                                {action.label}
                              </Button>
                            )
                          })}
                        </div>
                      ) : null}
                    </div>
                  </li>
                )
              })}
            </ol>
          </AccordionContent>
        </AccordionItem>
      </Accordion>
    </section>
  )
}

/**
 * Figma setup status marker — checked [`2912:16600`](https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=2912-16600),
 * unchecked [`2912:16636`](https://www.figma.com/design/IQfpCZBNsQLbRAaPhnfmul/Guest-Loop-MVP?node-id=2912-16636).
 * Complete: 8-segment primary ring + solid center. Incomplete/partial: empty dashed ring.
 * Partial only changes the row background — not this marker.
 */
function StatusMarker({ complete }: { complete: boolean }) {
  return (
    <span
      className={cn(
        "relative size-4 shrink-0",
        complete ? "text-primary" : "text-[#cfcfcf] dark:text-white/35"
      )}
      aria-hidden
    >
      <svg viewBox="0 0 16 16" className="size-full" fill="none">
        <circle
          cx="8"
          cy="8"
          r="6.5"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeDasharray="3.2 1.9"
          strokeLinecap="butt"
        />
      </svg>
      {complete ? (
        <span className="absolute top-1/2 left-1/2 size-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary" />
      ) : null}
    </span>
  )
}
