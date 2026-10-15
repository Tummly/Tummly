import {
  FOR_GROUPS_BUILT_FOR,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_HEADING,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { cn } from "@/lib/utils"

/**
 * Figma For Groups “Built for restaurant groups” `5661:20535`.
 * Eyebrow + title + body, then three equal cards.
 */
export function ForGroupsBuiltFor() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="for-groups-built-for-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start gap-[46px] lg:gap-[70px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[835px] flex-col gap-[18px]">
          <div className="flex w-full max-w-[790px] flex-col gap-3.5">
            <p className="m-0 font-jakarta text-sm font-medium leading-4 text-[#4a4a4a]">
              {FOR_GROUPS_BUILT_FOR.eyebrow}
            </p>
            <h2
              id="for-groups-built-for-heading"
              className={cn("m-0 text-black", FOR_GROUPS_SECTION_HEADING)}
            >
              {FOR_GROUPS_BUILT_FOR.title}
            </h2>
          </div>
          <p className={cn("m-0 max-w-[835px]", FOR_GROUPS_SECTION_BODY)}>
            {FOR_GROUPS_BUILT_FOR.body}
          </p>
        </header>

        <div className="grid w-full grid-cols-1 gap-5 md:grid-cols-3 md:gap-10">
          {FOR_GROUPS_BUILT_FOR.cards.map((card) => (
            <article
              key={card.id}
              className="flex min-w-0 flex-col gap-4 bg-[#f0f0f0] p-[26px] text-[#141414]"
            >
              <h3 className="m-0 font-sans text-lg font-medium leading-6 lg:text-xl lg:leading-6">
                {card.title}
              </h3>
              <p className="m-0 font-sans text-base font-normal leading-[22px] lg:text-lg lg:leading-6">
                {card.body}
              </p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}
