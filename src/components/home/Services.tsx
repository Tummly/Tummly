import {
  ClipboardPen,
  Inbox,
  LayoutTemplate,
  List,
  Power,
  QrCode,
  Send,
  Sparkles,
  TicketPercent,
  type LucideIcon,
} from "lucide-react"

type ServiceItemProps = {
  icon: LucideIcon
  title: string
  description: string
}

function ServiceItem({ icon: Icon, title, description }: ServiceItemProps) {
  return (
    <article className="flex w-full flex-col gap-4.5">
      <div className="flex w-fit shrink-0 items-center self-start overflow-hidden rounded-[8px] bg-[#e4eee7] p-2.5">
        <Icon className="size-6 text-black" strokeWidth={2} aria-hidden />
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
    icon: QrCode,
    title: "Smart Guest Links and QR prompts",
    description:
      "Create scan and link prompts for in-store, takeaway, delivery and digital touchpoints.",
  },
  {
    icon: ClipboardPen,
    title: "Short mobile feedback form",
    description:
      "Let guests share quick visit feedback, choose issue tags, leave an optional comment and share one contact method.",
  },
  {
    icon: List,
    title: "Guest list with consent records",
    description:
      "Build a guest list with consent status, source, first interaction and recent activity in one place.",
  },
  {
    icon: Inbox,
    title: "Private feedback inbox",
    description:
      "Review feedback, tags, comments and recovery actions before small issues become bigger problems.",
  },
  {
    icon: TicketPercent,
    title: "Offers and redemption controls",
    description:
      "Create offers with expiry, unique codes, staff checks and redemption history.",
  },
  {
    icon: LayoutTemplate,
    title: "Campaign templates",
    description:
      "Send thank-you, quiet-day, win-back, new-item and recovery messages without starting from scratch.",
  },
  {
    icon: Send,
    title: "Email and SMS campaigns",
    description:
      "Reach opted-in guests through simple campaigns, with usage tracked against your plan or credits.",
  },
  {
    icon: Sparkles,
    title: "AI-assisted weekly brief",
    description:
      "See what changed this week, what guests are saying and which actions are worth reviewing next.",
  },
  {
    icon: Power,
    title: "Consent and opt-out controls",
    description:
      "Keep consent records, unsubscribe status and eligible campaign audiences clear before messages are sent.",
  },
] as const

function Services() {
  return (
    <section className="w-full bg-[#f8f8f8]">
      <div className="mx-auto flex w-full  flex-col gap-12 px-4 py-12 sm:gap-14 sm:px-6 sm:py-16 md:px-10 lg:gap-15 lg:px-16 lg:py-22.5 xl:px-45">
        <header className="mx-auto flex max-w-3xl flex-col items-center gap-3 text-center">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            What Tummly gives your restaurant
          </h2>
          <p className="m-0 max-w-xl text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
            The core tools to capture guests, collect private feedback, send
            return offers and see what needs action each week.
          </p>
        </header>

        <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 sm:gap-10 lg:grid-cols-3">
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
