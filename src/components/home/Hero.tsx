import { useState } from "react"
import { ArrowRight } from "lucide-react"
import { motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import { HeroSignupForm } from "@/components/home/HeroSignupForm"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import { HOME_HERO } from "@/content/marketing/homePage"
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
      className="inline-flex shrink-0 items-center text-[10px] font-medium leading-[12px] text-[#fafafa] no-underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/40 lg:text-xs lg:leading-5"
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
 * Home hero — desktop Figma `5433:12691`, mobile `6241:17596`.
 * Centered copy + Pilot signup; campaign video in visual frame `5985:32077`.
 */
function Hero() {
  return (
    <section className="-mt-5 w-full overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-[#fafafa]">
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[50px] pt-[50px] pb-[70px]",
          // Figma mobile `6241:17596`: 24px. Desktop keep 60px chrome inset.
          "px-6 lg:px-[60px]",
        )}
      >
        <div className="flex w-full max-w-[1428px] flex-col items-center gap-[26px]">
          <div className="inline-flex max-w-full flex-nowrap items-center justify-center gap-2 rounded-[80px] bg-[#141414] px-5 py-3">
            <p className="m-0 shrink-0 text-[10px] font-medium leading-[13px] whitespace-nowrap text-[#f4f4f4] lg:text-sm lg:leading-normal">
              {HOME_HERO.groupsPrompt}
            </p>
            <HeroGroupsCtaLink />
          </div>

          <div className="flex w-full flex-col items-center gap-[18px] text-center text-[#141414] lg:gap-[26px]">
            <h1
              className={cn(
                "m-0 w-full max-w-[760px] font-jakarta font-semibold",
                "text-[28px] leading-[34px] lg:text-[54px] lg:leading-[70px]",
              )}
            >
              {HOME_HERO.title}
            </h1>
            <p
              className={cn(
                "m-0 w-full max-w-[546px] font-sans font-normal",
                "text-sm leading-5 lg:text-lg lg:leading-6",
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
