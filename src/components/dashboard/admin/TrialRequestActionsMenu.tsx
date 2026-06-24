import {
  CheckIcon,
  MessageCircleQuestionIcon,
  MoreHorizontalIcon,
  RotateCcwIcon,
  Trash2Icon,
  XIcon,
} from "lucide-react"

import { canReviewTrialRequest } from "@/components/dashboard/admin/adminTrialRequestStatus"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { AdminTrialRequest } from "@/types/admin"

type TrialRequestActionsMenuProps = {
  request: AdminTrialRequest
  showDelete: boolean
  onApprove: (request: AdminTrialRequest) => void
  onDecline: (request: AdminTrialRequest) => void
  onRequestMoreInfo: (request: AdminTrialRequest) => void
  onResendInvite: (request: AdminTrialRequest) => void
  onDelete: (request: AdminTrialRequest) => void
  disabled?: boolean
}

export function TrialRequestActionsMenu({
  request,
  showDelete,
  onApprove,
  onDecline,
  onRequestMoreInfo,
  onResendInvite,
  onDelete,
  disabled = false,
}: TrialRequestActionsMenuProps) {
  const canReview = canReviewTrialRequest(request)
  const canResendInvite = request.isApproved && !request.isAccountCreated
  const hasActions = canReview || canResendInvite || showDelete

  if (!hasActions) {
    return <span className="text-sm text-muted-foreground">—</span>
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={`Actions for ${request.businessName}`}
          disabled={disabled}
        >
          <MoreHorizontalIcon />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-52 rounded-xl">
        <DropdownMenuLabel className="font-normal">
          <span className="block truncate font-medium text-foreground">
            {request.businessName}
          </span>
          <span className="block truncate text-xs text-muted-foreground">
            {request.email}
          </span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />

        {canReview && (
          <DropdownMenuGroup>
            <DropdownMenuItem variant="success" onClick={() => onApprove(request)}>
              <CheckIcon />
              Approve trial request
            </DropdownMenuItem>
            <DropdownMenuItem
              variant="warning"
              onClick={() => onRequestMoreInfo(request)}
            >
              <MessageCircleQuestionIcon />
              Request more info
            </DropdownMenuItem>
            <DropdownMenuItem
              variant="destructive"
              onClick={() => onDecline(request)}
            >
              <XIcon />
              Decline
            </DropdownMenuItem>
          </DropdownMenuGroup>
        )}

        {canResendInvite && (
          <DropdownMenuGroup>
            {canReview && <DropdownMenuSeparator />}
            <DropdownMenuItem onClick={() => onResendInvite(request)}>
              <RotateCcwIcon />
              Resend Operator Setup invitation
            </DropdownMenuItem>
          </DropdownMenuGroup>
        )}

        {showDelete && (
          <>
            {(canReview || canResendInvite) && <DropdownMenuSeparator />}
            <DropdownMenuItem
              variant="destructive"
              onClick={() => onDelete(request)}
            >
              <Trash2Icon />
              Delete trial request
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
