import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"

const faqTriggerClassName =
  "items-center rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer focus-visible:border-0 focus-visible:ring-0 **:data-[slot=accordion-trigger-icon]:box-content **:data-[slot=accordion-trigger-icon]:size-3 **:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:rounded-full **:data-[slot=accordion-trigger-icon]:bg-[#232323] **:data-[slot=accordion-trigger-icon]:p-1.75 **:data-[slot=accordion-trigger-icon]:text-white"

const faqQuestionClassName =
  "m-0 min-w-0 flex-1 text-left font-sans text-[22px] font-semibold leading-[normal] tracking-normal text-[#232323]"

const faqContentClassName = "pt-3 pb-0 pr-6"

const faqAnswerClassName =
  "m-0 font-sans text-base font-normal leading-5.5 tracking-normal text-[#232323]"

type FaqItemProps = {
  value: string
  question: string
  answer: string
}

function FaqItem({ value, question, answer }: FaqItemProps) {
  return (
    <AccordionItem
      value={value}
      className="border-0 border-b border-[#e0e0e0] pb-3.5"
    >
      <AccordionTrigger className={faqTriggerClassName}>
        <span className={faqQuestionClassName}>{question}</span>
      </AccordionTrigger>
      <AccordionContent className={faqContentClassName}>
        <p className={faqAnswerClassName}>{answer}</p>
      </AccordionContent>
    </AccordionItem>
  )
}

const faqItems = [
  {
    value: "what-does-tummly-do",
    question: "What does Tummly help my restaurant do?",
    answer:
      "Tummly helps restaurants collect private guest feedback, grow a consented guest list, send simple return offers and see what needs action each week from one workspace.",
  },
  {
    value: "how-guests-join",
    question: "How do guests join or leave feedback?",
    answer:
      "Guests scan a QR prompt or open a guest link. They can share quick private feedback, add an optional comment and choose whether to join your restaurant's guest list.",
  },
  {
    value: "guest-app",
    question: "Do guests need to download an app?",
    answer:
      "No. Guests open a short mobile page from a QR code or link. They can leave feedback and opt in without downloading an app.",
  },
  {
    value: "pos-setup",
    question: "Do I need to change my POS, website or delivery setup?",
    answer:
      "No. Tummly is designed to work alongside your existing channels. You can start with guest prompts, links, feedback and campaigns without replacing your current setup.",
  },
  {
    value: "after-request",
    question: "What happens after I request access?",
    answer:
      "You submit your restaurant details and verify your email. We review the request, then send the right setup link if your restaurant is approved for guided access.",
  },
  {
    value: "guided-trial",
    question: "What is included in the guided trial?",
    answer:
      "Approved trials include access to the core workspace, guest links, private feedback, guest list, offers, campaigns, weekly brief, starter QR materials and a standard trial launch allowance.",
  },
  {
    value: "guest-list-control",
    question: "Who controls the guest list?",
    answer:
      "Your restaurant controls its identifiable guest list. Tummly provides the secure workspace to manage feedback, consent, offers and messages. Guests can opt out where required.",
  },
  {
    value: "public-reviews",
    question: "Can Tummly help with public reviews?",
    answer:
      "Tummly is built for private feedback first. Do not use Tummly to reward, gate or manipulate public reviews. Any future public review features must stay separate from incentives and follow platform rules.",
  },
  {
    value: "charged-on-request",
    question: "Will I be charged when I request access?",
    answer:
      "No payment is taken when you request access. Any paid plan, extra usage, premium print packs or add-ons are confirmed before use.",
  },
] as const

function Faqs() {
  return (
    <section className="w-full bg-white">
      <div className="mx-auto flex w-full max-w-360 flex-col gap-8 px-4 py-12 sm:gap-10 sm:px-6 sm:py-16 md:px-10 lg:flex-row lg:items-start lg:gap-10 lg:px-16 lg:py-22.5 xl:gap-16 2xl:gap-24 min-[1728px]:gap-47.5 xl:px-45">
        <header className="flex w-full shrink-0 flex-col gap-3 sm:max-w-sm lg:max-w-72 xl:max-w-99.5">
          <h2 className="m-0 text-[clamp(1.75rem,4vw,2.625rem)] font-bold leading-[normal] text-[#232323]">
            FAQs
          </h2>
          <p className="m-0 text-base font-medium leading-6.5 text-[#232323] sm:text-[17px] lg:text-lg">
            Answers to common questions about guided access, guest feedback,
            consent, offers and setup.
          </p>
        </header>

        <Accordion
          type="single"
          collapsible
          className="min-w-0 flex-1 gap-8.5"
          defaultValue="what-does-tummly-do"
        >
          {faqItems.map((item) => (
            <FaqItem
              key={item.value}
              value={item.value}
              question={item.question}
              answer={item.answer}
            />
          ))}
        </Accordion>
      </div>
    </section>
  )
}

export default Faqs
