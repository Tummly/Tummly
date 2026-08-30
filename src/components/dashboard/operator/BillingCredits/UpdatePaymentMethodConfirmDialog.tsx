import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"

type UpdatePaymentMethodConfirmDialogProps = {
  open: boolean
  title: string
  body: string
  continueLabel: string
  busy?: boolean
  onOpenChange: (open: boolean) => void
  onContinue: () => void
  onCancel: () => void
}

export function UpdatePaymentMethodConfirmDialog({
  open,
  title,
  body,
  continueLabel,
  busy = false,
  onOpenChange,
  onContinue,
  onCancel,
}: UpdatePaymentMethodConfirmDialogProps) {
  return (
    <AccountWorkspaceConfirmDialog
      open={open}
      title={title}
      body={body}
      primaryLabel={continueLabel}
      busy={busy}
      onOpenChange={onOpenChange}
      onPrimary={onContinue}
      onCancel={onCancel}
    />
  )
}
