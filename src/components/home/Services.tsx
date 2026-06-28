import aiBriefIcon from "@/assets/svg/ui-icons/ai-brief.svg"
import campaignIcon from "@/assets/svg/ui-icons/campaign.svg"
import consentIcon from "@/assets/svg/ui-icons/consent.svg"
import listIcon from "@/assets/svg/ui-icons/list.svg"
import mobileFeedbackIcon from "@/assets/svg/ui-icons/mobile-feedback.svg"
import offersIcon from "@/assets/svg/ui-icons/offers.svg"
import privateFeedbackIcon from "@/assets/svg/ui-icons/private-feedback.svg"
import qrIcon from "@/assets/svg/ui-icons/qr.svg"
import templatesIcon from "@/assets/svg/ui-icons/templates.svg"
import {
  marketingSectionBody,
  marketingSectionHeading,
  marketingSectionShell,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

type ServiceItemProps = {
  icon: string
  title: string
  description: string
}

function ServiceItem({ icon, title, description }: ServiceItemProps) {
  return (
    <article className="flex w-full flex-col gap-4.5">
      <div className="flex w-fit shrink-0 items-center self-start overflow-hidden rounded-[8px] bg-[#e4eee7] p-2.5">
        <img
          src={icon}
          alt=""
          width={24}
          height={24}
          className="size-6 object-contain"
          aria-hidden
        />
      </div>

      <div className="flex w-full flex-col gap-3.5 wrap-break-word">
        <h3 className="m-0 text-lg font-bold leading-[normal] text-black">
          {title}
        </h3>
        <p className="m-0 text-sm font-normal leading-5 text-black">
          {description}
        </p>
      </div>
    </article>
  )
}

const services = [
  {
    icon: qrIcon,
    title: "Smart Guest Links and QR prompts",
    description:
      "Create scan and link prompts for in-store, takeaway, delivery and digital touchpoints.",
  },
  {
    icon: mobileFeedbackIcon,
    title: "Short mobile feedback form",
    description:
      "Let guests share quick visit feedback, choose issue tags, leave an optional comment and share one contact method.",
  },
  {
    icon: listIcon,
    title: "Guest list with consent records",
    description:
      "Build a guest list with consent status, source, first interaction and recent activity in one place.",
  },
  {
    icon: privateFeedbackIcon,
    title: "Private feedback inbox",
    description:
      "Review feedback, tags, comments and recovery actions before small issues become bigger problems.",
  },
  {
    icon: offersIcon,
    title: "Offers and redemption controls",
    description:
      "Create offers with expiry, unique codes, staff checks and redemption history.",
  },
  {
    icon: templatesIcon,
    title: "Campaign templates",
    description:
      "Send thank-you, quiet-day, win-back, new-item and recovery messages without starting from scratch.",
  },
  {
    icon: campaignIcon,
    title: "Email and SMS campaigns",
    description:
      "Reach opted-in guests through simple campaigns, with usage tracked against your plan or credits.",
  },
  {
    icon: aiBriefIcon,
    title: "AI-assisted weekly brief",
    description:
      "See what changed this week, what guests are saying and which actions are worth reviewing next.",
  },
  {
    icon: consentIcon,
    title: "Consent and opt-out controls",
    description:
      "Keep consent records, unsubscribe status and eligible campaign audiences clear before messages are sent.",
  },
] as const

function Services() {
  return (
    <section className="w-full bg-[#f8f8f8]">
      <div className={marketingSectionShell()}>
        <header className="flex max-w-3xl flex-col items-start gap-3 text-left lg:mx-auto lg:items-center lg:text-center">
          <h2 className={cn("m-0", marketingSectionHeading)}>
            What Tummly gives your restaurant
          </h2>
          <p className={cn("m-0 max-w-xl", marketingSectionBody)}>
            The core tools to capture guests, collect private feedback, send
            return offers and see what needs action each week.
          </p>
        </header>

        <div className="grid grid-cols-2 gap-8 sm:gap-10 lg:grid-cols-3">
          {services.map((service) => (
            <ServiceItem
              key={service.title}
              icon={service.icon}
              title={service.title}
              description={service.description}
            />
          ))}
        </div>
      </div>
    </section>
  )
}

export default Services
