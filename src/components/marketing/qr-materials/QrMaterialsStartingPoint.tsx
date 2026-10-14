import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  QR_MATERIALS_STARTING_POINT,
  type QrMaterialsLoopStage,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

function LoopStageCard({ stage }: { stage: QrMaterialsLoopStage }) {
  return (
    <article
      className={cn(
        "flex min-h-[158px] min-w-[240px] flex-1 flex-col gap-2.5 overflow-hidden p-[18px] text-[#111] lg:min-w-0",
        stage.highlighted ? "bg-[#eef8f1]" : "bg-[#f7f7f4]",
      )}
    >
      <h3 className="m-0 font-jakarta text-[17px] font-bold leading-[22px]">
        {stage.title}
      </h3>
      <p className="m-0 font-sans text-[13px] font-normal leading-[19px]">
        {stage.body}
      </p>
    </article>
  )
}

/**
 * Figma QR materials post-scan Guest Loop `5661:19419`.
 * Intro, five stage cards, CTA to Product.
 */
export function QrMaterialsStartingPoint() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="qr-materials-starting-point-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[900px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {QR_MATERIALS_STARTING_POINT.eyebrow}
          </p>
          <h2
            id="qr-materials-starting-point-heading"
            className={cn(
              "m-0 w-full max-w-[732px]",
              QR_MATERIALS_SECTION_HEADING,
            )}
          >
            {QR_MATERIALS_STARTING_POINT.title}
          </h2>
          <p className={cn("m-0 w-full", QR_MATERIALS_SECTION_BODY)}>
            {QR_MATERIALS_STARTING_POINT.body}
          </p>
        </header>

        {/*
          Desktop: five equal stage cards in one row (Figma gap 14).
          Below lg: horizontal scroll so the strip stays one row.
        */}
        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div
            className="flex min-w-[1280px] gap-3.5 lg:min-w-0"
            role="list"
            aria-label="Post-scan Guest Loop"
          >
            {QR_MATERIALS_STARTING_POINT.stages.map((stage) => (
              <div key={stage.id} role="listitem" className="flex min-w-0 flex-1">
                <LoopStageCard stage={stage} />
              </div>
            ))}
          </div>
        </div>

        <MarketingButton asChild variant="secondary">
          <Link to={QR_MATERIALS_STARTING_POINT.ctaTo}>
            {QR_MATERIALS_STARTING_POINT.ctaLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}
