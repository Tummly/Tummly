import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantDraftPreview,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import { assistantDraftPreviewEditAction } from "@/lib/operatorAiAssistant/assistantDraftPreviewEditAction"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-[10px] leading-3 text-op-assistant-list-subtitle">
        {label}
      </span>
      <span className="text-sm leading-5 text-op-text-primary">{value}</span>
    </div>
  )
}

export function AssistantDraftPreviewCard({
  draft,
  onEditDraft,
  pendingRecoveryDraft = null,
}: {
  draft: OperatorAiAssistantDraftPreview
  onEditDraft: (action: OperatorAiAssistantAction) => void
  pendingRecoveryDraft?: RecoveryDraftActionPayload | null
}) {
  const editAction = assistantDraftPreviewEditAction(
    draft,
    pendingRecoveryDraft
  )
  const heading =
    draft.kind === "campaign"
      ? (draft.name ?? "Campaign Draft")
      : draft.kind === "offer"
        ? (draft.title ?? "Offer Draft")
        : "Recovery Draft"

  return (
    <div
      className="flex flex-col gap-3 rounded-[8px] border border-op-assistant-list-border bg-op-assistant-list-row-active/40 p-3"
      data-assistant-draft-preview={draft.kind}
    >
      <div className="flex flex-wrap items-center gap-2">
        <p className="m-0 text-sm font-semibold leading-5 text-op-text-primary">
          {heading}
        </p>
        <Badge variant="soft">{draft.statusLabel}</Badge>
      </div>

      {draft.kind === "campaign" ? (
        <div className="flex flex-col gap-2">
          {draft.audienceLabel ? (
            <MetaRow label="Audience" value={draft.audienceLabel} />
          ) : null}
          {draft.channel ? (
            <MetaRow label="Channel" value={draft.channel} />
          ) : null}
          {draft.timingLabel ? (
            <MetaRow label="Timing" value={draft.timingLabel} />
          ) : null}
          {draft.conditionsSummary ? (
            <MetaRow label="Offer" value={draft.conditionsSummary} />
          ) : null}
          {draft.validityLabel ? (
            <MetaRow label="Validity" value={draft.validityLabel} />
          ) : null}
          {draft.subject ? (
            <MetaRow label="Subject" value={draft.subject} />
          ) : null}
          {draft.messageBody ? (
            <MetaRow label="Message" value={draft.messageBody} />
          ) : null}
        </div>
      ) : null}

      {draft.kind === "offer" ? (
        <div className="flex flex-col gap-2">
          {draft.conditionsSummary ? (
            <MetaRow label="Conditions" value={draft.conditionsSummary} />
          ) : null}
          {draft.validityLabel ? (
            <MetaRow label="Validity" value={draft.validityLabel} />
          ) : null}
        </div>
      ) : null}

      {draft.kind === "recovery" ? (
        <div className="flex flex-col gap-2">
          {draft.channel ? (
            <MetaRow label="Channel" value={draft.channel} />
          ) : null}
          {draft.purpose ? (
            <MetaRow label="Purpose" value={draft.purpose} />
          ) : null}
          {draft.subject ? (
            <MetaRow label="Subject" value={draft.subject} />
          ) : null}
          {draft.messageBody ? (
            <MetaRow label="Message" value={draft.messageBody} />
          ) : null}
        </div>
      ) : null}

      {editAction ? (
        <Button
          type="button"
          variant="op-primary"
          className="h-9 self-start rounded-[8px] px-3 text-sm"
          onClick={() => {
            onEditDraft(editAction)
          }}
        >
          {editAction.label}
        </Button>
      ) : null}
    </div>
  )
}
