import { Link } from "react-router-dom"

import phoneFeedbackImage from "@/assets/images/marketing/home/platform-phone-feedback.webp"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLoopVideo } from "@/components/marketing/MarketingLoopVideo"
import {
  HOME_PLATFORM,
  type HomePlatformCard,
} from "@/content/marketing/homePage"
import { homePlatformVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

const CARD_BACKGROUND = "#e1dfdb"

/**
 * Start the three loops (~0.7 MB in total) about two screens early, so they
 * have buffered before the cards scroll in.
 */
const PLATFORM_VIDEO_LOAD_MARGIN = "1800px 0px"

/**
 * Figma video fill transform on cards `6761:46436` / `46440` / `46444`:
 * the 4:5 loop is drawn ~9% taller than the card, centred, and nudged 11.5%
 * down (the plate is the same beige as the card, so the gap above is
 * seamless). Sizing from the height keeps the loop 4:5 at every card width.
 */
const VIDEO_FILL_STYLE = {
  left: "50%",
  top: "11.5%",
  width: "auto",
  height: "108.8%",
  aspectRatio: "4 / 5",
  transform: "translateX(-50%)",
} as const

function PlatformCardCopy({ card }: { card: HomePlatformCard }) {
  return (
    <div className="relative z-10 flex w-full shrink-0 flex-col items-center gap-3.5 px-2.5 text-center text-[#141414]">
      <h3 className="m-0 font-jakarta text-xl font-bold leading-[normal]">
        {card.title}
      </h3>
      <p
        className="m-0 font-sans text-sm font-normal leading-5"
        style={{ maxWidth: card.bodyWidth }}
      >
        {card.body}
      </p>
    </div>
  )
}

/**
 * Card 1 `6761:46432` — beige plate, copy, then the phone image
 * (`7195:11567`, 229×435) cut off by the card's bottom edge.
 * Cards 2–4 `6761:46436` / `46440` / `46444` — the For Groups product loop
 * (same Figma video fill) fills the whole card with the copy over its top.
 */
function PlatformCard({ card }: { card: HomePlatformCard }) {
  return (
    <article
      className={cn(
        "relative flex h-[458px] w-full flex-col items-center overflow-hidden rounded-[12px] pt-10",
        card.media === "phone" ? "gap-[22px]" : "pb-5",
      )}
      style={{ backgroundColor: CARD_BACKGROUND }}
    >
      {card.media === "phone" ? (
        <>
          <PlatformCardCopy card={card} />
          <img
            src={phoneFeedbackImage}
            alt={card.mediaAlt}
            width={229}
            height={435}
            className="h-[435px] w-[229px] max-w-none shrink-0"
            draggable={false}
            decoding="async"
            loading="lazy"
          />
        </>
      ) : (
        <>
          <MarketingLoopVideo
            src={homePlatformVideoUrl(card.media)}
            loadRootMargin={PLATFORM_VIDEO_LOAD_MARGIN}
            preload="auto"
            className="absolute inset-0 size-full"
            videoClassName="object-cover object-center"
            style={VIDEO_FILL_STYLE}
          />
          <PlatformCardCopy card={card} />
        </>
      )}
    </article>
  )
}

/**
 * Home The platform — Figma `6761:46375`.
 * Eyebrow + title + body, four equal cards and the product CTA.
 */
export function HomePlatform() {
  return (
    <section
      className="w-full bg-[#f2f0ec]"
      aria-labelledby="home-platform-heading"
    >
      <div className="mx-auto flex w-full flex-col items-center gap-[46px] px-5 py-12.5 lg:px-[60px] lg:py-[70px]">
        <header className="flex w-full max-w-[696px] flex-col items-center gap-[22px] text-center text-[#141414]">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 font-sans text-sm font-semibold leading-4 tracking-[1.12px] text-[#4a4a4a]">
              {HOME_PLATFORM.eyebrow}
            </p>
            <h2
              id="home-platform-heading"
              className="m-0 w-full max-w-[598px] font-jakarta text-[28px] font-medium leading-9 text-balance lg:text-[46px] lg:leading-[52px]"
            >
              {HOME_PLATFORM.title}
            </h2>
          </div>
          <p className="m-0 w-full max-w-[526px] font-sans text-base font-normal leading-[22px] lg:text-[18px] lg:leading-6">
            {HOME_PLATFORM.body}
          </p>
        </header>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-[26px] p-0 sm:grid-cols-2 min-[85rem]:grid-cols-4">
          {HOME_PLATFORM.cards.map((card) => (
            <li
              key={card.id}
              className="mx-auto flex w-full min-w-0 max-w-[420px] sm:max-w-none"
            >
              <PlatformCard card={card} />
            </li>
          ))}
        </ul>

        <MarketingButton
          asChild
          variant="secondary"
          className="w-full sm:w-[210px]"
        >
          <Link to={HOME_PLATFORM.ctaTo}>{HOME_PLATFORM.ctaLabel}</Link>
        </MarketingButton>
      </div>
    </section>
  )
}
