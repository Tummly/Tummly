import growAtYourOwnPaceIcon from "@/assets/svg/marketing/for-groups/grow-at-your-own-pace.svg"
import keepManagersFocusedIcon from "@/assets/svg/marketing/for-groups/keep-managers-focused.svg"
import spotRecurringIssuesIcon from "@/assets/svg/marketing/for-groups/spot-recurring-issues.svg"
import {
  FOR_GROUPS_MANAGING,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { PRODUCT_EYEBROW } from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

const CARD_ICONS = {
  "spot-recurring-issues": {
    src: spotRecurringIssuesIcon,
    width: 50,
    height: 49,
  },
  "keep-managers-focused": {
    src: keepManagersFocusedIcon,
    width: 50,
    height: 50,
  },
  "grow-at-your-own-pace": {
    src: growAtYourOwnPaceIcon,
    width: 50,
    height: 50,
  },
} as const

/**
 * Figma For Groups Managing multiple restaurants `5661:20535`.
 * Centre copy + three grey cards. 20px gap above (hero is a rounded frame).
 */
export function ForGroupsManaging() {
  return (
    <section
      className="mt-5 w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white"
      aria-labelledby="for-groups-managing-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-10 lg:gap-[70px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex w-full flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className={PRODUCT_EYEBROW}>{FOR_GROUPS_MANAGING.eyebrow}</p>
            <h2
              id="for-groups-managing-heading"
              className="m-0 max-w-[610px] font-jakarta text-[34px] font-medium leading-10 text-black lg:text-[46px] lg:leading-[52px]"
            >
              {FOR_GROUPS_MANAGING.title}
            </h2>
          </div>
          <p className={cn("m-0 max-w-[616px]", FOR_GROUPS_SECTION_BODY)}>
            {FOR_GROUPS_MANAGING.body}
          </p>
        </header>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-5 p-0 md:grid-cols-3">
          {FOR_GROUPS_MANAGING.cards.map((card) => {
            const icon = CARD_ICONS[card.id]
            return (
              <li key={card.id} className="flex min-w-0">
                <article className="flex w-full flex-col items-center gap-7 rounded-[12px] bg-[#f0f0f0] px-[26px] py-9 text-center text-[#141414]">
                  <img
                    src={icon.src}
                    alt=""
                    width={icon.width}
                    height={icon.height}
                    className="shrink-0"
                    aria-hidden
                    decoding="async"
                  />
                  <div className="flex w-full flex-col items-center gap-3">
                    <h3 className="m-0 font-jakarta text-2xl font-bold leading-[34px]">
                      {card.title}
                    </h3>
                    <p className="m-0 font-sans text-base font-normal leading-[22px] lg:text-[18px] lg:leading-6">
                      {card.body}
                    </p>
                  </div>
                </article>
              </li>
            )
          })}
        </ul>
      </div>
    </section>
  )
}
