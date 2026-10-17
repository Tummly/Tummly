import setupGuestFormImage from "@/assets/images/marketing/pilot/setup-guest-form.png"
import setupLocationImage from "@/assets/images/marketing/pilot/setup-location.png"
import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  PILOT_SECTION_Y,
  PILOT_SETUP,
  type PilotSetupCard,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

const SETUP_IMAGES = {
  location: {
    src: setupLocationImage,
    /** Figma `5661:19957` — 357×376; export is the clipped visible slab. */
    width: 357,
    height: 351,
    /** Offset from card top — Figma y=209 on 560px Product. */
    topClass: "top-[209px]",
    widthClass: "w-[357px]",
  },
  "guest-loop": {
    src: setupGuestFormImage,
    /** Figma `5661:19983` — 392×445; export is the clipped visible slab. */
    width: 392,
    height: 352,
    /** Offset from card top — Figma y=208 on 560px Product. */
    topClass: "top-[208px]",
    widthClass: "w-[392px]",
  },
} as const

function SetupCard({ card }: { card: PilotSetupCard }) {
  const visual = card.id === "go-live" ? null : SETUP_IMAGES[card.id]

  return (
    <article
      className={cn(
        "relative w-full min-w-0 overflow-hidden bg-[#f0f0f0] p-[38px]",
        "min-h-[420px] lg:h-[560px]",
      )}
    >
      <div className="relative z-10 flex w-full flex-col items-start gap-1.5">
        <p className="m-0 font-jakarta text-2xl font-normal leading-normal text-black">
          {card.number}
        </p>
        <h3 className="m-0 font-jakarta text-2xl font-medium leading-normal text-black">
          {card.title}
        </h3>
        <p className="m-0 max-w-[392px] font-sans text-base font-normal leading-[22px] text-[#141414]">
          {card.body}
        </p>
      </div>

      {visual != null && card.imageAlt != null ? (
        <img
          src={visual.src}
          alt={card.imageAlt}
          width={visual.width}
          height={visual.height}
          className={cn(
            "pointer-events-none absolute left-1/2 max-w-none -translate-x-1/2 select-none",
            /* Below md the cards stack — drop the desktop pin and flow under copy. */
            "max-md:relative max-md:left-auto max-md:top-auto max-md:mt-8 max-md:max-w-full max-md:translate-x-0",
            visual.topClass,
            visual.widthClass,
          )}
          decoding="async"
          draggable={false}
        />
      ) : null}
    </article>
  )
}

/**
 * Figma Pilot “Set up first” `5661:19947` / cards row `5661:19951`.
 * Visuals are absolutely pinned from the card top (Figma y≈208–209) and clipped.
 */
export function PilotSetup() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-setup-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10",
          PILOT_SECTION_INSET,
          PILOT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[722px] flex-col gap-[18px]">
          <p className={cn("m-0 max-w-[644px]", PILOT_SECTION_BODY)}>
            {PILOT_SETUP.body}
          </p>
          <h2
            id="pilot-setup-heading"
            className={cn("m-0 text-black", PILOT_SECTION_HEADING)}
          >
            {PILOT_SETUP.title}
          </h2>
        </header>

        <div className="grid w-full grid-cols-1 gap-5 md:grid-cols-3">
          {PILOT_SETUP.cards.map((card) => (
            <SetupCard key={card.id} card={card} />
          ))}
        </div>
      </div>
    </section>
  )
}
