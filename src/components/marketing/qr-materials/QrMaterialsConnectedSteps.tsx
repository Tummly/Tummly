import {
  QR_MATERIALS_CONNECTED,
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  type QrMaterialsConnectedStep,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

function ConnectedStep({ step }: { step: QrMaterialsConnectedStep }) {
  return (
    <div className="flex w-full flex-col gap-2 text-[#141414]">
      <p className="m-0 font-sans text-xs font-bold leading-normal">
        {step.stepLabel}
      </p>
      <h3 className="m-0 font-jakarta text-lg font-bold leading-normal">
        {step.title}
      </h3>
      <p className="m-0 max-w-[520px] font-sans text-sm font-normal leading-5">
        {step.body}
      </p>
    </div>
  )
}

/**
 * Figma QR materials connected placements `5661:19391`.
 * Intro + two-column steps (1/3 left, 2/4 right) with column dividers.
 */
export function QrMaterialsConnectedSteps() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="qr-materials-connected-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[66px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[776px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {QR_MATERIALS_CONNECTED.eyebrow}
          </p>
          <h2
            id="qr-materials-connected-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_CONNECTED.title}
          </h2>
          <p className={cn("m-0 w-full", QR_MATERIALS_SECTION_BODY)}>
            {QR_MATERIALS_CONNECTED.body}
          </p>
        </header>

        <div className="flex w-full flex-col gap-10 lg:flex-row lg:gap-[26px]">
          {QR_MATERIALS_CONNECTED.columns.map((column, columnIndex) => (
            <div
              key={columnIndex}
              className="flex min-w-0 flex-1 flex-col gap-4"
            >
              {column.map((step, stepIndex) => (
                <div key={step.id} className="contents">
                  {stepIndex > 0 ? (
                    <div
                      className="h-px w-full bg-[#cacaca]"
                      aria-hidden
                    />
                  ) : null}
                  <ConnectedStep step={step} />
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
