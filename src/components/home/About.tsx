import { whyTummlyPictures } from "@/assets/marketing-images"
import ImageWithCard from "@/components/home/ImageWithCard"
import { GRID_CARD_IMAGE_SIZES } from "@/lib/imagePresets"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionShell,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const aboutCards = [
  {
    picture: whyTummlyPictures[0],
    title: "Grow your guest list from everyday touchpoints",
    description:
      "Invite guests to join from counter cards, receipts, packaging, delivery inserts and digital links, with clear consent built in.",
  },
  {
    picture: whyTummlyPictures[1],
    title: "Collect private feedback before issues become public",
    description:
      "Guests can share a quick rating, issue tags and optional comments, so your team can spot problems early.",
  },
  {
    picture: whyTummlyPictures[2],
    title: "Encourage return visits with controlled offers",
    description:
      "Send thank-you, quiet-day or win-back offers to opted-in guests, with expiry and redemption controls built in.",
  },
] as const

function About() {
  return (
    <section className="w-full bg-[#f4f4f4]">
      <div className={marketingSectionShell()}>
        <header className="flex max-w-xl flex-col gap-3">
          <h2 className={cn("m-0", marketingSectionHeading)}>Why Tummly?</h2>
          <p className={cn("m-0", marketingSectionBody)}>
            Use QR prompts and guest links to collect private feedback, grow
            your guest list and send return offers without adding more admin.
          </p>
        </header>

        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3 lg:gap-7.5">
          {aboutCards.map((card) => (
            <ImageWithCard
              key={card.title}
              picture={card.picture}
              imageAlt={card.title}
              title={card.title}
              description={card.description}
              sizes={GRID_CARD_IMAGE_SIZES}
              size="compact"
            />
          ))}
        </div>
      </div>
    </section>
  )
}

export default About
