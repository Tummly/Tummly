import type { ReactNode } from "react"

import stepAccount from "@/assets/operator-home/step-account.svg"
import stepLogo from "@/assets/operator-home/step-logo.svg"
import type { OperatorHomeSetupStepId } from "@/types/operatorHome"

/** Matches guest-form filled green (`#59AE65`). */
const ICON_COMPLETE = "#59AE65"
const ICON_INCOMPLETE = "#6C6C6C"

type StepIconProps = {
  complete: boolean
}

function strokeProps(complete: boolean) {
  return {
    stroke: complete ? ICON_COMPLETE : ICON_INCOMPLETE,
    strokeWidth: 1.92593,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  }
}

/** Closed shapes fill green when the step is done (same treatment as guest-form). */
function fillFor(complete: boolean): string {
  return complete ? ICON_COMPLETE : "none"
}

function GuestFormIcon({ complete }: StepIconProps) {
  if (complete) {
    return (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
        <path
          d="M19.5 13C19.5 13.5746 19.2717 14.1257 18.8654 14.5321C18.4591 14.9384 17.908 15.1667 17.3333 15.1667H4.33333L0 19.5V2.16667C0 1.59203 0.228273 1.04093 0.634602 0.634602C1.04093 0.228273 1.59203 0 2.16667 0H17.3333C17.908 0 18.4591 0.228273 18.8654 0.634602C19.2717 1.04093 19.5 1.59203 19.5 2.16667V13Z"
          fill={ICON_COMPLETE}
        />
      </svg>
    )
  }

  return (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden>
      <path
        d="M19.5 13C19.5 13.5746 19.2717 14.1257 18.8654 14.5321C18.4591 14.9384 17.908 15.1667 17.3333 15.1667H4.33333L0 19.5V2.16667C0 1.59203 0.228273 1.04093 0.634602 0.634602C1.04093 0.228273 1.59203 0 2.16667 0H17.3333C17.908 0 18.4591 0.228273 18.8654 0.634602C19.2717 1.04093 19.5 1.59203 19.5 2.16667V13Z"
        stroke={ICON_INCOMPLETE}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function FirstResponseIcon({ complete }: StepIconProps) {
  const stroke = strokeProps(complete)
  const fill = fillFor(complete)

  return (
    <svg width="26" height="26" viewBox="0 0 26 26" fill="none" aria-hidden>
      <path
        d="M17.3334 22.75V20.5833C17.3334 19.4341 16.8769 18.3319 16.0642 17.5192C15.2516 16.7065 14.1494 16.25 13.0001 16.25H6.50008C5.35081 16.25 4.24861 16.7065 3.43595 17.5192C2.62329 18.3319 2.16675 19.4341 2.16675 20.5833V22.75"
        fill={fill}
        {...stroke}
      />
      <path
        d="M9.75008 11.9167C12.1433 11.9167 14.0834 9.97657 14.0834 7.58333C14.0834 5.1901 12.1433 3.25 9.75008 3.25C7.35685 3.25 5.41675 5.1901 5.41675 7.58333C5.41675 9.97657 7.35685 11.9167 9.75008 11.9167Z"
        fill={fill}
        {...stroke}
      />
      <path
        d="M23.8333 22.75V20.5833C23.8325 19.6232 23.513 18.6905 22.9247 17.9316C22.3365 17.1728 21.5129 16.6308 20.5833 16.3908"
        {...stroke}
      />
      <path
        d="M17.3333 3.39081C18.2654 3.62947 19.0915 4.17157 19.6815 4.93164C20.2715 5.69172 20.5917 6.62654 20.5917 7.58872C20.5917 8.55091 20.2715 9.48573 19.6815 10.2458C19.0915 11.0059 18.2654 11.548 17.3333 11.7866"
        {...stroke}
      />
    </svg>
  )
}

function QrPlacementIcon({ complete }: StepIconProps) {
  const stroke = strokeProps(complete)
  const fill = fillFor(complete)

  return (
    <svg width="26" height="26" viewBox="0 0 26 26" fill="none" aria-hidden>
      <path
        d="M7.58333 3.25H4.33333C3.73502 3.25 3.25 3.73502 3.25 4.33333V7.58333C3.25 8.18164 3.73502 8.66667 4.33333 8.66667H7.58333C8.18164 8.66667 8.66667 8.18164 8.66667 7.58333V4.33333C8.66667 3.73502 8.18164 3.25 7.58333 3.25Z"
        fill={fill}
        {...stroke}
      />
      <path
        d="M21.6667 3.25H18.4167C17.8184 3.25 17.3333 3.73502 17.3333 4.33333V7.58333C17.3333 8.18164 17.8184 8.66667 18.4167 8.66667H21.6667C22.265 8.66667 22.75 8.18164 22.75 7.58333V4.33333C22.75 3.73502 22.265 3.25 21.6667 3.25Z"
        fill={fill}
        {...stroke}
      />
      <path
        d="M7.58333 17.3333H4.33333C3.73502 17.3333 3.25 17.8184 3.25 18.4167V21.6667C3.25 22.265 3.73502 22.75 4.33333 22.75H7.58333C8.18164 22.75 8.66667 22.265 8.66667 21.6667V18.4167C8.66667 17.8184 8.18164 17.3333 7.58333 17.3333Z"
        fill={fill}
        {...stroke}
      />
      <path
        d="M22.75 17.3333H19.5C18.9254 17.3333 18.3743 17.5616 17.9679 17.9679C17.5616 18.3743 17.3333 18.9254 17.3333 19.5V22.75"
        {...stroke}
      />
      <path d="M22.75 22.75V22.7596" {...stroke} />
      <path
        d="M13 7.58333V10.8333C13 11.408 12.7717 11.9591 12.3654 12.3654C11.9591 12.7717 11.408 13 10.8333 13H7.58333"
        {...stroke}
      />
      <path d="M3.25 13H3.25963" {...stroke} />
      <path d="M13 3.25H13.0096" {...stroke} />
      <path d="M13 17.3333V17.343" {...stroke} />
      <path d="M17.3333 13H18.4167" {...stroke} />
      <path d="M22.75 13V13.0096" {...stroke} />
      <path d="M13 22.75V21.6667" {...stroke} />
    </svg>
  )
}

function FirstOfferIcon({ complete }: StepIconProps) {
  const stroke = strokeProps(complete)
  const fill = fillFor(complete)

  return (
    <svg width="26" height="26" viewBox="0 0 26 26" fill="none" aria-hidden>
      <path
        d="M13.6348 2.8015C13.2286 2.39514 12.6776 2.16679 12.103 2.16667H4.33333C3.7587 2.16667 3.2076 2.39494 2.80127 2.80127C2.39494 3.2076 2.16667 3.7587 2.16667 4.33333V12.103C2.16679 12.6776 2.39514 13.2286 2.8015 13.6348L12.2308 23.0642C12.7232 23.5534 13.3892 23.8281 14.0833 23.8281C14.7775 23.8281 15.4434 23.5534 15.9358 23.0642L23.0642 15.9358C23.5534 15.4434 23.8281 14.7775 23.8281 14.0833C23.8281 13.3892 23.5534 12.7232 23.0642 12.2308L13.6348 2.8015Z"
        fill={fill}
        {...stroke}
      />
      <path
        d="M8.125 8.66667C8.42415 8.66667 8.66667 8.42415 8.66667 8.125C8.66667 7.82585 8.42415 7.58333 8.125 7.58333C7.82585 7.58333 7.58333 7.82585 7.58333 8.125C7.58333 8.42415 7.82585 8.66667 8.125 8.66667Z"
        fill={complete ? ICON_COMPLETE : ICON_INCOMPLETE}
        {...stroke}
      />
    </svg>
  )
}

function FirstCampaignIcon({ complete }: StepIconProps) {
  const stroke = strokeProps(complete)
  const fill = fillFor(complete)

  return (
    <svg width="26" height="26" viewBox="0 0 26 26" fill="none" aria-hidden>
      <path
        d="M12.5667 18.2C12.4529 18.6126 12.259 18.9987 11.9959 19.3363C11.7329 19.6739 11.4059 19.9564 11.0337 20.1677C10.6615 20.3789 10.2513 20.5148 9.82663 20.5675C9.40191 20.6203 8.97096 20.5888 8.55839 20.475C8.14582 20.3612 7.75971 20.1673 7.4221 19.9042C7.08449 19.6412 6.80199 19.3142 6.59074 18.942C6.37949 18.5698 6.24361 18.1597 6.19088 17.7349C6.13814 17.3102 6.16958 16.8793 6.28339 16.4667"
        {...stroke}
      />
      <path
        d="M3.25 11.9167L22.75 6.5V19.5L3.25 15.1667V11.9167Z"
        fill={fill}
        {...stroke}
      />
    </svg>
  )
}

function RasterStepIcon({ src }: { src: string }) {
  return (
    <img src={src} alt="" className="max-h-full max-w-full object-contain" aria-hidden />
  )
}

const OUTLINE_STEP_ICONS: Partial<
  Record<OperatorHomeSetupStepId, (props: StepIconProps) => ReactNode>
> = {
  "guest-form": GuestFormIcon,
  "first-response": FirstResponseIcon,
  "qr-placement": QrPlacementIcon,
  "first-offer": FirstOfferIcon,
  "first-campaign": FirstCampaignIcon,
}

/** Renders the checklist step glyph; outline steps turn green-filled when complete. */
export function HomeSetupStepIcon({
  stepId,
  complete,
}: {
  stepId: OperatorHomeSetupStepId
  complete: boolean
}) {
  const OutlineIcon = OUTLINE_STEP_ICONS[stepId]
  if (OutlineIcon) {
    return <OutlineIcon complete={complete} />
  }

  if (stepId === "account-ready") {
    return <RasterStepIcon src={stepAccount} />
  }

  if (stepId === "upload-logo") {
    return <RasterStepIcon src={stepLogo} />
  }

  return null
}
