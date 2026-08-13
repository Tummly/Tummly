import { XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent } from "@/components/ui/dialog"
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
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="flex flex-col justify-between gap-[60px] bg-[var(--main-bg\/colour,#1b1b1b)] p-8 text-foreground sm:max-w-[560px]"
      >
        <div className="flex w-full items-start justify-center gap-[22px]">
          <div className="flex min-w-px flex-1 flex-col gap-3">
            <p className="text-2xl font-bold text-[color:var(--main-bg\/title,white)]">
              {ASSISTANT_DELETE_TITLE}
            </p>
            <p className="text-sm font-medium text-[color:var(--main-bg\/subtitle,#7c7c7c)]">
              {ASSISTANT_DELETE_BODY}
            </p>
          </div>
          <Button
            type="button"
            variant="op-collapse"
            aria-label="Close"
            className="shrink-0 bg-[var(--button\/collaps\/bg-colour,#2c2c2c)] p-3"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            <XIcon className="size-[18px]" aria-hidden />
          </Button>
        </div>
        <div className="flex items-center gap-3">
          <Button
            type="button"
            variant="destructive"
            className="h-[42px] rounded-[var(--button\/radius,2px)] px-4"
            onClick={onConfirm}
          >
            {ASSISTANT_DELETE_CONFIRM}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className="h-[42px] rounded-[var(--button\/radius,2px)] border border-[var(--button\/tertiary\/colour-default,#4e4e4e)] px-[17px]"
            onClick={() => {
              onOpenChange(false)
            }}
          >
            Cancel
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
