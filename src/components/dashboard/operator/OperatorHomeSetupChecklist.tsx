import { useState } from "react"

import stepAccount from "@/assets/operator-home/step-account.png"
import stepCampaign from "@/assets/operator-home/step-campaign.png"
import stepGuestForm from "@/assets/operator-home/step-guest-form.png"
import stepLogo from "@/assets/operator-home/step-logo.png"
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
  countCompleteSetupSteps,
  getSetupStepIllustration,
  hasSetupStepTintedRow,
  resolveSetupActionButtonVariant,
  SETUP_STEP_COPY_GAP_CLASS,
  SETUP_STEP_DESCRIPTION_CLASS,
  SETUP_STEP_TITLE_CLASS,
  shouldShowSetupStatusMarker,
  shouldSpreadSetupStepActions,
} from "@/lib/operatorHome/setupChecklistPresentation"
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
  "upload-logo": stepLogo,
  "guest-form": stepGuestForm,
  "first-response": stepResponse,
  "qr-placement": stepQr,
  "first-offer": stepResponse,
  "first-campaign": stepCampaign,
}

const SETUP_ACCORDION_VALUE = "setup"

type OperatorHomeSetupChecklistProps = {
  steps: OperatorHomeSetupStep[]
  onPreviewGuestForm?: () => void
  previewBusy?: boolean
}

/** Figma “Make your Guest Loop ready for guests” collapsible checklist. */
export function OperatorHomeSetupChecklist({
  steps,
  onPreviewGuestForm,
  previewBusy = false,
}: OperatorHomeSetupChecklistProps) {
  const [openValues, setOpenValues] = useState<string[]>(() =>
    readSetupChecklistOpen() ? [SETUP_ACCORDION_VALUE] : []
  )
  const { completeCount, totalSteps } = countCompleteSetupSteps(steps)

  return (
    <section className="rounded-md border border-[#dcdcdc] bg-white p-[25px] shadow-[0px_1px_1.5px_rgba(19,29,43,0.04),0px_8px_12px_rgba(19,29,43,0.08)] dark:border-[#262626] dark:bg-[#171717] dark:shadow-none">
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
            <div className="flex min-w-0 flex-1 flex-col gap-2 text-left">
              <h2 className="text-xl font-bold text-foreground">
                Make your Guest Loop ready for guests
              </h2>
              <p className="text-sm font-normal text-muted-foreground dark:text-[#7c7c7c]">
                {completeCount} of {totalSteps} steps complete
              </p>
            </div>
            <span
              className="flex size-[42px] shrink-0 items-center justify-center rounded-sm bg-[#f5f5f5] text-foreground dark:bg-[#2c2c2c]"
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
                const showMarker = shouldShowSetupStatusMarker(step.status)
                const tintedRow = hasSetupStepTintedRow(step.status)
                const isComplete = step.status === "complete"
                const hasActions = step.actions.length > 0
                const spreadActions = shouldSpreadSetupStepActions(
                  step.status,
                  step.actions.length
                )
                const illustration = getSetupStepIllustration(step.id)

                return (
                  <li
                    key={step.id}
                    className={cn(
                      "relative flex items-center px-[30px] py-5",
                      showMarker && "gap-[14px]",
                      tintedRow
                        ? "rounded bg-[#f8f8f8] dark:bg-[#202020]"
                        : "rounded"
                    )}
                  >
                    {showMarker ? <StatusMarker complete={isComplete} /> : null}

                    <div className="flex min-w-0 flex-1 items-center">
                      <SetupStepIllustration
                        src={STEP_IMAGES[step.id]}
                        config={illustration}
                      />

                      <div
                        className={cn(
                          "flex min-w-0 flex-1 items-center pl-3",
                          hasActions && "pb-1",
                          spreadActions && "justify-between"
                        )}
                      >
                        <div
                          className={cn(
                            "flex min-w-0 flex-col",
                            SETUP_STEP_COPY_GAP_CLASS,
                            hasActions && "flex-1"
                          )}
                        >
                          <div className="leading-[0]">
                            <p className={SETUP_STEP_TITLE_CLASS}>{step.title}</p>
                          </div>
                          <div className="leading-[0]">
                            <p className={SETUP_STEP_DESCRIPTION_CLASS}>
                              {step.description}
                            </p>
                          </div>
                        </div>

                        {hasActions ? (
                          <div className="flex shrink-0 flex-wrap items-center gap-[18px]">
                            {step.actions.map((action) => {
                              const isPreview =
                                action.id === "preview-guest-form"
                              const available = isPreview
                                ? action.available && !previewBusy
                                : action.available

                              return (
                                <Button
                                  key={action.id}
                                  type="button"
                                  variant={resolveSetupActionButtonVariant(
                                    action.id
                                  )}
                                  size="sm"
                                  className="h-auto min-h-0 disabled:opacity-50"
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
 * Complete: 16px dashed ring + 10px solid center. Partial: 16px dashed ring only.
 */
function StatusMarker({ complete }: { complete: boolean }) {
  return (
    <span className="relative size-4 shrink-0" aria-hidden>
      <svg
        viewBox="0 0 16 16"
        className={cn(
          "size-full",
          complete ? "text-primary" : "text-[#cfcfcf] dark:text-white/35"
        )}
        fill="none"
      >
        <circle
          cx="8"
          cy="8"
          r="6.25"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeDasharray="2.5 2.5"
          strokeLinecap="round"
        />
      </svg>
      {complete ? (
        <span className="absolute top-[3px] left-[3px] size-[10px] rounded-full bg-primary" />
      ) : null}
    </span>
  )
}

function SetupStepIllustration({
  src,
  config,
}: {
  src: string
  config: ReturnType<typeof getSetupStepIllustration>
}) {
  return (
    <div
      className="relative w-[49px] shrink-0 overflow-hidden"
      style={{ height: `${config.height}px` }}
    >
      <img
        src={src}
        alt=""
        className={cn(
          "absolute max-w-none",
          config.crop === "cover"
            ? "inset-0 size-full object-cover"
            : "pointer-events-none"
        )}
        style={
          config.crop === "cover"
            ? undefined
            : {
                width: config.crop.width,
                height: config.crop.height,
                left: config.crop.left,
                top: config.crop.top,
              }
        }
      />
    </div>
  )
}
