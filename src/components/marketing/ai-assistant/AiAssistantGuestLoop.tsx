import chipAi from "@/assets/svg/marketing/ai-assistant/chip-ai.svg"
import chipCampaigns from "@/assets/svg/marketing/ai-assistant/chip-campaigns.svg"
import chipFeedback from "@/assets/svg/marketing/ai-assistant/chip-feedback.svg"
import chipGuests from "@/assets/svg/marketing/ai-assistant/chip-guests.svg"
import chipReports from "@/assets/svg/marketing/ai-assistant/chip-reports.svg"
import { AiAssistantMutedVideo } from "@/components/marketing/ai-assistant/AiAssistantMutedVideo"
import {
  AI_ASSISTANT_GUEST_LOOP,
  AI_ASSISTANT_SECTION_INSET,
  AI_ASSISTANT_SECTION_Y,
  type AiAssistantGuestLoopChip,
} from "@/content/marketing/aiAssistantPage"
import { aiAssistantVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

const CHIP_ICON_SRC: Record<AiAssistantGuestLoopChip["icon"], string> = {
  feedback: chipFeedback,
  guests: chipGuests,
  campaigns: chipCampaigns,
  reports: chipReports,
  ai: chipAi,
}

function ChipIcon({ icon }: { icon: AiAssistantGuestLoopChip["icon"] }) {
  return (
    <img
      src={CHIP_ICON_SRC[icon]}
      alt=""
      width={20}
      height={20}
      className="size-5 shrink-0"
      aria-hidden
    />
  )
}

/** Figma Guest Loop video section `5924:17849` — full player chrome. */
export function AiAssistantGuestLoop() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[46px] lg:gap-[70px]",
          AI_ASSISTANT_SECTION_INSET,
          AI_ASSISTANT_SECTION_Y,
        )}
      >
        <div className="flex w-full flex-col items-center gap-[26px]">
          <h2 className="m-0 max-w-[548px] text-center font-jakarta text-xl font-medium leading-[26px] text-black">
            {AI_ASSISTANT_GUEST_LOOP.title}
          </h2>

          <ul className="m-0 flex list-none flex-wrap items-center justify-center gap-x-0 gap-y-2 p-0">
            {AI_ASSISTANT_GUEST_LOOP.chips.map((chip, index) => (
              <li key={chip.id} className="flex items-center">
                {index > 0 ? (
                  <span
                    aria-hidden
                    className="mx-2 hidden h-[34px] w-px bg-[#d0d0d0] sm:block"
                  />
                ) : null}
                <div className="flex items-center gap-4 rounded-[8px] px-5 py-3.5">
                  <ChipIcon icon={chip.icon} />
                  <span className="font-jakarta text-sm font-medium leading-4 text-black">
                    {chip.label}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        </div>

        <div
          id="guest-loop-video"
          className="w-full max-w-[1354px] scroll-mt-[180px]"
        >
          <AiAssistantMutedVideo
            src={aiAssistantVideoUrl("guestLoop")}
            withPlayerControls
            className="aspect-video w-full rounded-[12px] bg-[#e8e8e8]"
          />
        </div>
      </div>
    </section>
  )
}
