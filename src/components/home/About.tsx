import card1 from "@/assets/images/card1.png"
import card2 from "@/assets/images/card2.png"
import card3 from "@/assets/images/card3.jpg"
import ImageWithCard from "@/components/home/ImageWithCard"

const aboutCards = [
  {
    image: card1,
    title: "Grow your guest list from everyday touchpoints",
    description:
      "Invite guests to join from counter cards, receipts, packaging, delivery inserts and digital links, with clear consent built in.",
  },
  {
    image: card2,
    title: "Collect private feedback before issues become public",
    description:
      "Guests can share a quick rating, issue tags and optional comments, so your team can spot problems early.",
  },
  {
    image: card3,
    title: "Encourage return visits with controlled offers",
    description:
      "Send thank-you, quiet-day or win-back offers to opted-in guests, with expiry and redemption controls built in.",
  },
] as const

function About() {
  return (
    <section className="w-full bg-[#f4f4f4]">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="flex max-w-xl flex-col gap-3">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-normal text-[#232323]">
            Why Tummly?
          </h2>
          <p className="m-0 text-base leading-normal text-[#232323] sm:text-[17px] lg:text-lg">
            Use QR prompts and guest links to collect private feedback, grow
            your guest list and send return offers without adding more admin.
          </p>
        </header>

        <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3 lg:gap-7.5">
          {aboutCards.map((card) => (
            <ImageWithCard
              key={card.title}
              image={card.image}
              imageAlt={card.title}
              title={card.title}
              description={card.description}
              size="compact"
            />
          ))}
        </div>
      </div>
    </section>
  )
}

export default About
