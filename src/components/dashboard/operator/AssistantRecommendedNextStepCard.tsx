import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import type { OperatorAiAssistantAction } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"

type AssistantRecommendedNextStepCardProps = {
  text: string
  actions: OperatorAiAssistantAction[]
  onActivateAction: (action: OperatorAiAssistantAction) => void
}

/**
 * Recommended next step card for the dark AI Assistant drawer.
 * Primary CTA uses op-primary; secondary is outline; both pill-rounded.
 */
export function AssistantRecommendedNextStepCard({
  text,
  actions,
  onActivateAction,
}: AssistantRecommendedNextStepCardProps) {
  return (
    <div
      className="flex w-full flex-col gap-3 rounded-[8px] border border-op-assistant-list-border bg-op-assistant-list-row-active/60 p-4"
      data-assistant-recommended-next-step=""
      data-assistant-answer-ctas=""
    >
      <div className="flex items-center gap-2">
        <AiIcon
          size={16}
          className="shrink-0 text-op-assistant-list-subtitle"
        />
        <p className="m-0 text-xs font-medium leading-4 text-op-assistant-list-subtitle">
          Recommended next step
        </p>
      </div>
      <p className="m-0 text-sm leading-5 text-op-assistant-list-title">
        {text}
      </p>
      {actions.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {actions.map((action, index) => {
            const isPrimary =
              action.type === "view-feedback-set" || index === 0
            return (
              <Button
                key={action.type}
                type="button"
                size="sm"
                variant={isPrimary ? "op-primary" : "op-tertiary"}
                className="!rounded-[8px] px-3 !font-normal"
                disabled={action.clickable === false}
                onClick={() => {
                  onActivateAction(action)
                }}
              >
                {action.label}
              </Button>
            )
          })}
        </div>
      ) : null}
    </div>
  )
}
