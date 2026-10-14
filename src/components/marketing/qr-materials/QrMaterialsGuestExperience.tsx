import {
  QR_MATERIALS_GUEST_EXPERIENCE,
  QR_MATERIALS_SECTION_HEADING,
  QR_MATERIALS_SECTION_INSET,
  QR_MATERIALS_SECTION_Y,
  type QrMaterialsPrincipleCard,
} from "@/content/marketing/qrMaterialsPage"
import { cn } from "@/lib/utils"

function PrincipleCard({ card }: { card: QrMaterialsPrincipleCard }) {
  return (
    <article className="flex min-w-0 flex-1 flex-col gap-2 text-[#141414]">
      <h3 className="m-0 font-jakarta text-lg font-bold leading-normal">
        {card.title}
      </h3>
      <p className="m-0 max-w-[498px] font-sans text-sm font-normal leading-5">
        {card.body}
      </p>
    </article>
  )
}

/**
 * Figma QR materials guest-experience principles `5661:19470`.
 * Eyebrow + title, then three equal principle columns.
 */
export function QrMaterialsGuestExperience() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="qr-materials-guest-experience-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[60px]",
          QR_MATERIALS_SECTION_INSET,
          QR_MATERIALS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[748px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {QR_MATERIALS_GUEST_EXPERIENCE.eyebrow}
          </p>
          <h2
            id="qr-materials-guest-experience-heading"
            className={cn("m-0 w-full text-[#141414]", QR_MATERIALS_SECTION_HEADING)}
          >
            {QR_MATERIALS_GUEST_EXPERIENCE.title}
          </h2>
        </header>

        <div className="flex w-full flex-col gap-10 lg:flex-row lg:gap-[26px]">
          {QR_MATERIALS_GUEST_EXPERIENCE.cards.map((card) => (
            <PrincipleCard key={card.id} card={card} />
          ))}
        </div>
      </div>
    </section>
  )
}
