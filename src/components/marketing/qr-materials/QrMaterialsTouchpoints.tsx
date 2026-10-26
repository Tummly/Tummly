import {
  QR_MATERIALS_SECTION_BODY,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  QR_MATERIALS_TOUCHPOINTS,
  type QrMaterialsTouchpointCard,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

function TouchpointCard({ card }: { card: QrMaterialsTouchpointCard }) {
  return (
    <article className="flex min-w-0 flex-1 flex-col gap-4 bg-white p-[26px]">
      <h3 className="m-0 font-jakarta text-base font-bold leading-[normal] text-[#141414]">
        {card.title}
      </h3>
      <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
        {card.body}
      </p>
    </article>
  )
}

/**
 * Figma QR materials touchpoint context `5661:19347`.
 * Title + body, then three equal white cards (10px gap).
 */
export function QrMaterialsTouchpoints() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="qr-materials-touchpoints-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[70px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[742px] flex-col gap-[18px]">
          <h2
            id="qr-materials-touchpoints-heading"
            className={cn("m-0 w-full", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_TOUCHPOINTS.title}
          </h2>
          <p
            className={cn(
              "m-0 w-full max-w-[688px]",
              QR_MATERIALS_SECTION_BODY,
            )}
          >
            {QR_MATERIALS_TOUCHPOINTS.body}
          </p>
        </header>

        {/*
          Desktop: three equal cards in one row (Figma gap 10).
          Below lg: horizontal scroll so the strip stays one row.
        */}
        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div className="flex min-w-[720px] gap-2.5 lg:min-w-0">
            {QR_MATERIALS_TOUCHPOINTS.cards.map((card) => (
              <TouchpointCard key={card.id} card={card} />
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
