import { ChevronRight } from "lucide-react"

import chooseLocationImage from "@/assets/images/marketing/qr-materials/step-choose-location.png"
import placeAndTestImage from "@/assets/images/marketing/qr-materials/step-place-and-test-qr.png"
import previewGuestFormImage from "@/assets/images/marketing/qr-materials/step-preview-guest-form.png"
import reviewFeedbackImage from "@/assets/images/marketing/qr-materials/step-review-feedback.png"
import {
  QR_MATERIALS_FEEDBACK_STEPS,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  type QrMaterialsFeedbackStep,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

/** Figma illustrations `6944:25335`, `6944:25307`, `6944:25439`, `6944:25480` (186×186). */
const STEP_IMAGES: Record<QrMaterialsFeedbackStep["id"], string> = {
  location: chooseLocationImage,
  preview: previewGuestFormImage,
  place: placeAndTestImage,
  review: reviewFeedbackImage,
}

/**
 * Figma chevron chip `6944:25554`: 16×112 pill that sits on the border
 * between two cards.
 */
function StepJoint() {
  return (
    <span
      aria-hidden
      className="absolute top-1/2 left-0 z-10 hidden h-28 w-4 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed] lg:flex"
    >
      <ChevronRight className="size-3.5 text-[#727272]" strokeWidth={2} />
    </span>
  )
}

/**
 * Figma QR materials steps `5661:19391`.
 * Grey card with a centred title and four dashed step cards. Cards share
 * borders on desktop (chevron chips join them) and stack on mobile.
 */
export function QrMaterialsFeedbackSteps() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="qr-materials-steps-heading"
    >
      <div
        className={cn(
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-center gap-10 rounded-[12px] bg-[#f0f0f0] px-5 py-10 lg:gap-[66px] lg:px-10 lg:py-[60px]">
          <h2
            id="qr-materials-steps-heading"
            className={cn(
              "m-0 w-full max-w-[484px] text-center",
              QR_MATERIALS_SECTION_HEADING,
            )}
          >
            {QR_MATERIALS_FEEDBACK_STEPS.title}
          </h2>

          <div className="w-full rounded-[12px] bg-[#f7f7f7] p-4 lg:p-[30px]">
            <ol className="m-0 grid list-none grid-cols-1 gap-4 p-0 sm:grid-cols-2 lg:grid-cols-4 lg:gap-0">
              {QR_MATERIALS_FEEDBACK_STEPS.steps.map((step, index) => (
                <li
                  key={step.id}
                  className={cn(
                    "relative m-0 flex min-w-0 flex-col items-center gap-2 rounded-[12px] border border-dashed border-[#a2a2a2] bg-white p-5 xl:p-[30px]",
                    index > 0 && "lg:border-l-0",
                  )}
                >
                  {index > 0 ? <StepJoint /> : null}
                  <span className="absolute top-[19px] right-5 inline-flex items-center justify-center rounded-[30px] bg-[#14a946] px-2.5 py-1.5 font-sans text-xs leading-[11px] font-normal text-white">
                    {step.stepLabel}
                  </span>
                  <img
                    src={STEP_IMAGES[step.id]}
                    alt=""
                    width={186}
                    height={186}
                    className="size-[186px] max-w-full shrink-0 select-none"
                    decoding="async"
                    draggable={false}
                  />
                  <div className="flex w-full flex-col gap-2 text-[#141414]">
                    <h3 className="m-0 font-jakarta text-lg font-semibold leading-[normal]">
                      {step.title}
                    </h3>
                    <p className="m-0 font-sans text-sm font-normal leading-5">
                      {step.body}
                    </p>
                  </div>
                </li>
              ))}
            </ol>
          </div>
        </div>
      </div>
    </section>
  )
}
