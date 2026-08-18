/** Shared Guest response chooser chrome for messaging recovery intents (Figma U-06). */

import { OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS } from "@/lib/operatorUi/operatorWizardChromePresentation"

export const GUEST_RESPONSE_STEP_HEADING = "Guest response"

export const GUEST_RESPONSE_STEP_DESCRIPTION =
  "Prepare and edit the private response that will be sent to the guest."

export const GUEST_RESPONSE_PREPARE_TITLE = "Prepare with AI"

export const GUEST_RESPONSE_PREPARE_DESCRIPTION =
  "Use the feedback and your confirmed information to prepare an editable response."

export const GUEST_RESPONSE_PREPARE_ACTION_LABEL = "Prepare response draft"

/** Display-only metering chrome — actual AI credit metering remains fog. */
export const GUEST_RESPONSE_AI_ACTION_METERING_LABEL = "Uses 1 AI action"

export const GUEST_RESPONSE_WRITE_MANUAL_TITLE = "Write manually"

export const GUEST_RESPONSE_WRITE_MANUAL_DESCRIPTION =
  "Write the complete response yourself without using an AI action."

export const GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL =
  "Write response manually"

export const GUEST_RESPONSE_REWRITE_AI_LABEL = "Rewrite with AI"

export const GUEST_RESPONSE_REWRITE_RETRY_LABEL = "Try Again"

/** Prepare / Write-manually cards — same fill and idle border as other wizard cards. */
export const GUEST_RESPONSE_CHOOSER_CARD_CLASS =
  `flex w-full flex-col gap-[22px] rounded-[4px] border p-[18px] ${OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS}`

/** Step heading when the operator is on the manual/AI editor path. */
export const GUEST_RESPONSE_WRITE_MANUAL_STEP_HEADING =
  GUEST_RESPONSE_WRITE_MANUAL_ACTION_LABEL

/** Figma Preparing response draft overlay (U-06). */
export const GUEST_RESPONSE_PREPARING_OVERLAY_TITLE =
  "Preparing response draft…"

export const GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION =
  "Tummly is using the feedback and confirmed information to prepare a private response."
