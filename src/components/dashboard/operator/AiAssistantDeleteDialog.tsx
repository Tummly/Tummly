import { OperatorDestructiveConfirmDialog } from "@/components/dashboard/operator/OperatorDestructiveConfirmDialog"
import {
  ASSISTANT_DELETE_BODY,
  ASSISTANT_DELETE_CONFIRM,
  ASSISTANT_DELETE_TITLE,
} from "@/lib/operatorAiAssistant/assistantListPresentation"

type AiAssistantDeleteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
}

/** Figma 3462:60077 — delete Assistant conversation confirm. */
export function AiAssistantDeleteDialog({
  open,
  onOpenChange,
  onConfirm,
}: AiAssistantDeleteDialogProps) {
  return (
    <OperatorDestructiveConfirmDialog
      open={open}
      title={ASSISTANT_DELETE_TITLE}
      description={ASSISTANT_DELETE_BODY}
      confirmLabel={ASSISTANT_DELETE_CONFIRM}
      onOpenChange={onOpenChange}
      onConfirm={onConfirm}
    />
  )
}
