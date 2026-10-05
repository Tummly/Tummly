/**
 * Guest feedback large-screen layout — fluid column + type scale.
 * Mobile keeps Figma 393 metrics; lg+ grows width, type, and spacing together.
 */

/** Shell content column — phone-first, widens on large viewports. */
export const GUEST_FEEDBACK_SHELL_CONTENT_CLASS = [
  "relative z-1 mx-auto flex w-full min-h-0 flex-1 flex-col",
  "max-w-[min(100%,clamp(22rem,94vw,26rem))] px-2.5 pt-2.5 pb-8",
  "sm:max-w-[min(100%,clamp(24rem,72vw,32rem))] sm:px-3 sm:pt-3 sm:pb-10",
  "md:max-w-[min(100%,clamp(26rem,52vw,36rem))] md:px-4 md:pt-5 md:pb-12",
  "lg:max-w-[min(100%,clamp(28rem,42vw,40rem))] lg:px-5 lg:pt-6 lg:pb-14",
  "xl:max-w-[min(100%,clamp(30rem,36vw,44rem))] xl:px-6",
].join(" ")

/** Form stack gap between major blocks. */
export const GUEST_FEEDBACK_FORM_STACK_CLASS =
  "flex w-full flex-col gap-[29px] lg:gap-9"

/** Brand / intro header pad. */
export const GUEST_FEEDBACK_FORM_HEADER_CLASS =
  "flex flex-col gap-6 rounded-[28px] p-5 lg:gap-7 lg:p-7"

/** Restaurant display name — *headline*. */
export const GUEST_FEEDBACK_RESTAURANT_NAME_CLASS =
  "truncate font-heading text-[clamp(1.375rem,2.4vw,1.75rem)] font-semibold leading-normal text-white"

/** Form H1 — *headline*. */
export const GUEST_FEEDBACK_FORM_TITLE_CLASS =
  "m-0 max-w-[14.5rem] font-heading text-[clamp(1.5rem,2.8vw,2rem)] font-bold leading-[1.2] text-white lg:max-w-[22rem]"

/** Intro / body copy — *functional*. */
export const GUEST_FEEDBACK_BODY_CLASS =
  "m-0 text-sm leading-5 text-[#9e9e9e] lg:text-base lg:leading-6"

/** Comment composer min height. */
export const GUEST_FEEDBACK_COMPOSER_CLASS =
  "relative flex min-h-[clamp(207px,32vh,280px)] flex-col overflow-hidden rounded-[28px] border border-guest-feedback-border-soft bg-[rgba(40,40,40,0.15)] backdrop-blur-[16px] sm:min-h-[clamp(230px,34vh,320px)] lg:min-h-[clamp(260px,36vh,380px)]"

/** Your-details glass panel. */
export const GUEST_FEEDBACK_DETAILS_PANEL_CLASS =
  "flex flex-col gap-6 overflow-hidden rounded-[28px] bg-guest-feedback-surface p-5 backdrop-blur-[18px] lg:gap-7 lg:p-7"

/** Name / contact glass inputs. */
export const GUEST_FEEDBACK_FIELD_INPUT_CLASS =
  "h-auto min-h-[50px] rounded-[18px] border-guest-feedback-border bg-guest-feedback-glass px-[15px] py-[15px] text-xs font-normal leading-normal text-guest-feedback-text shadow-none backdrop-blur-[12px] placeholder:text-guest-feedback-placeholder focus-visible:border-guest-feedback-border focus-visible:ring-1 focus-visible:ring-white/10 disabled:bg-guest-feedback-glass dark:bg-guest-feedback-glass dark:disabled:bg-guest-feedback-glass lg:min-h-14 lg:text-sm"

/** Primary CTA. */
export const GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS =
  "h-auto min-h-12.5 w-full rounded-[54px] px-4 py-4 text-sm font-medium leading-normal shadow-none lg:min-h-14 lg:py-[1.125rem] lg:text-base"

/** Thank-you / unlock page H1. */
export const GUEST_FEEDBACK_THANK_YOU_TITLE_CLASS =
  "m-0 font-heading text-[clamp(1.75rem,3vw,2.25rem)] font-bold leading-normal text-guest-feedback-text"

/** Thank-you supporting body. */
export const GUEST_FEEDBACK_THANK_YOU_BODY_CLASS =
  "m-0 max-w-[min(100%,18.7rem)] text-sm font-medium leading-5 text-[#888] lg:max-w-md lg:text-base lg:leading-6"

/** Ticket card chrome. */
export const GUEST_FEEDBACK_TICKET_CARD_CLASS =
  "relative flex w-full flex-col items-center gap-[22px] overflow-visible rounded-[28px] bg-guest-feedback-surface px-5 py-[30px] shadow-[inset_0_0_0_1px_rgba(74,74,76,0.22)] backdrop-blur-[18px] lg:gap-7 lg:px-8 lg:py-10 lg:rounded-[32px]"
