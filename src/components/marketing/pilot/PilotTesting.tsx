import {
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  PILOT_SECTION_Y,
  PILOT_TESTING,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

/**
 * Figma Pilot “What you’re testing” `5661:19910`.
 * Eyebrow + title, then six numbered cards in a 3×2 grid.
 */
export function PilotTesting() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-testing-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10",
          PILOT_SECTION_INSET,
          PILOT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[895px] flex-col gap-[18px] text-black">
          <p className="m-0 font-sans text-sm font-medium uppercase leading-normal lg:text-base">
            {PILOT_TESTING.eyebrow}
          </p>
          <h2
            id="pilot-testing-heading"
            className={cn("m-0", PILOT_SECTION_HEADING)}
          >
            {PILOT_TESTING.title}
          </h2>
        </header>

        <div className="grid w-full grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {PILOT_TESTING.cards.map((card) => (
            <article
              key={card.id}
              className="flex min-h-0 min-w-0 flex-col bg-[#f0f0f0] p-[38px]"
            >
              <div className="flex w-full flex-col items-start gap-1.5">
                <p className="m-0 font-jakarta text-2xl font-normal leading-normal text-black">
                  {card.number}
                </p>
                <h3 className="m-0 font-jakarta text-2xl font-medium leading-normal text-black">
                  {card.title}
                </h3>
                <p className="m-0 font-sans text-base font-normal leading-[22px] text-[#141414]">
                  {card.body}
                </p>
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}
