import {
  PRODUCT_DISPLAY_HEADING,
  PRODUCT_OUTCOMES,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
} from "@/content/marketing/productPage"
import consideredServiceReplyIcon from "@/assets/svg/marketing/product/icons/considered-service-reply.svg"
import relevantReasonToReturnIcon from "@/assets/svg/marketing/product/icons/relevant-reason-to-return.svg"
import clearerManagementConversationIcon from "@/assets/svg/marketing/product/icons/clearer-management-conversation.svg"
import { cn } from "@/lib/utils"

const CARD_ICONS: Readonly<Record<string, string>> = {
  "considered-service-reply": consideredServiceReplyIcon,
  "relevant-reason-to-return": relevantReasonToReturnIcon,
  "clearer-management-conversation": clearerManagementConversationIcon,
}

/** Figma Product outcomes `5661:24151` — three white cards on grey. */
export function ProductOutcomes() {
  return (
    <section
      className="w-full rounded-bl-[12px] rounded-br-[12px] bg-[#f0f0f0]"
      aria-labelledby="product-outcomes-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[46px] lg:gap-[66px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <h2
          id="product-outcomes-heading"
          className={cn(PRODUCT_DISPLAY_HEADING, "max-w-[597px] text-center")}
        >
          {PRODUCT_OUTCOMES.title}
        </h2>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-3 p-0 md:grid-cols-3">
          {PRODUCT_OUTCOMES.cards.map((card) => (
            <li key={card.id} className="flex min-w-0">
              <article className="flex w-full flex-col gap-4 rounded-[4px] bg-white p-[26px]">
                <img
                  src={CARD_ICONS[card.id]}
                  alt=""
                  width={34}
                  height={34}
                  className="size-[34px] shrink-0"
                  aria-hidden
                />
                <div className="flex flex-col gap-3.5">
                  <h3 className="m-0 font-jakarta text-xl font-semibold leading-6 text-[#141414]">
                    {card.title}
                  </h3>
                  <p className="m-0 font-sans text-base font-normal leading-5 text-[#141414]">
                    {card.body}
                  </p>
                </div>
              </article>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
