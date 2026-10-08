import type { LucideIcon } from "lucide-react"
import {
  Megaphone,
  MessageSquare,
  QrCode,
  Tag,
  UploadCloud,
  User,
  Users,
} from "lucide-react"

import { cn } from "@/lib/utils"
import type { OperatorHomeSetupStepId } from "@/types/operatorHome"

/**
 * Figma setup checklist step glyphs (node 3353:42471) — Lucide outlines.
 * Complete → primary green; incomplete → muted gray.
 * Guest-form fills when complete (Figma filled chat).
 */
const STEP_ICONS: Record<OperatorHomeSetupStepId, LucideIcon> = {
  "account-ready": User,
  "upload-logo": UploadCloud,
  "guest-form": MessageSquare,
  "first-response": Users,
  "qr-placement": QrCode,
  "first-offer": Tag,
  "first-campaign": Megaphone,
}

/** Renders the checklist step glyph; outline steps turn primary when complete. */
export function HomeSetupStepIcon({
  stepId,
  complete,
}: {
  stepId: OperatorHomeSetupStepId
  complete: boolean
}) {
  const Icon = STEP_ICONS[stepId]
  const fillComplete = complete && stepId === "guest-form"

  return (
    <Icon
      className={cn(
        "size-6.5 shrink-0",
        complete
          ? "text-primary"
          : "text-[#6C6C6C] dark:text-[#7c7c7c]"
      )}
      strokeWidth={1.75}
      fill={fillComplete ? "currentColor" : "none"}
      aria-hidden
    />
  )
}
