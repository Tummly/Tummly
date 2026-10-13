import { useState } from "react"
import { ArrowRight } from "lucide-react"
import { motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import { HOME_HERO } from "@/content/marketing/homePage"
import {
  marketingChromeContentInset,
  marketingHeroBody,
  marketingHeroHeading,
} from "@/lib/marketing-layout"
import { homeHeroVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

const GROUPS_CTA_HOVER_MS = 0.3
/** Lucide `size-3.5` — reserved so hover does not change link width. */
const GROUPS_CTA_ARROW_W = 14
const GROUPS_CTA_GAP = 8

/**
 * Home hero groups link — same hover pattern as MarketingButton:
 * width locked; label shifts left; arrow fades in.
 */
function HeroGroupsCtaLink() {
  const reduceMotion = useReducedMotion()
  const [hovered, setHovered] = useState(false)
  const labelShift = (GROUPS_CTA_ARROW_W + GROUPS_CTA_GAP) / 2
  const duration = reduceMotion ? 0 : GROUPS_CTA_HOVER_MS

  return (
    <Link
      to={HOME_HERO.groupsCtaTo}
      className="inline-flex shrink-0 items-center text-xs font-medium leading-5 text-[#fafafa] no-underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40"
      onMouseEnter={() => {
        setHovered(true)
      }}
      onMouseLeave={() => {
        setHovered(false)
      }}
    >
      <motion.span
        className="inline-flex items-center"
        style={{ gap: GROUPS_CTA_GAP }}
        initial={false}
        animate={{ x: hovered ? 0 : labelShift }}
        transition={{ duration, ease: "easeOut" }}
      >
        {HOME_HERO.groupsCtaLabel}
        <motion.span
          aria-hidden
          initial={false}
          animate={{ opacity: hovered ? 1 : 0 }}
          transition={{ duration, ease: "easeOut" }}
          className="relative block h-3.5 shrink-0"
          style={{ width: GROUPS_CTA_ARROW_W }}
        >
          <ArrowRight
            className="absolute top-0 left-0 size-3.5"
            strokeWidth={2}
          />
        </motion.span>
      </motion.span>
    </Link>
  )
}

/**
 * Home hero — Figma `5433:12691` (Frame 269).
 * Centered copy + Pilot signup; campaign video in visual frame `5985:32077`.
 */
function Hero() {
  return (
    <section className="-mt-5 w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[50px] pt-[50px] pb-[70px]",
          marketingChromeContentInset,
        )}
      >
        <div className="flex w-full max-w-[1428px] flex-col items-center gap-[26px]">
          <div className="inline-flex max-w-full flex-wrap items-center justify-center gap-x-7 gap-y-2 rounded-[80px] bg-[#141414] px-5 py-3">
            <p className="m-0 text-sm font-medium leading-normal text-[#f4f4f4]">
              {HOME_HERO.groupsPrompt}
            </p>
            <HeroGroupsCtaLink />
          </div>

          <div className="flex w-full flex-col items-center gap-[26px] text-center text-[#141414]">
            <h1
              className={cn(
                "m-0 w-full max-w-[760px] font-semibold",
                marketingHeroHeading,
                "lg:text-[54px] lg:leading-[70px]",
              )}
            >
              {HOME_HERO.title}
            </h1>
            <p
              className={cn(
                "m-0 w-full max-w-[546px] font-normal",
                marketingHeroBody,
              )}
            >
              {HOME_HERO.body}
            </p>
          </div>

          <HeroSignupForm />
        </div>

        <div className="aspect-[1548/796] w-full overflow-hidden rounded-[12px] bg-[#ebebeb]">
          <AiAssistantMutedVideo
            src={homeHeroVideoUrl()}
            eager
            className="size-full bg-[#ebebeb]"
          />
        </div>
      </div>
    </section>
  )
}

export default Hero
