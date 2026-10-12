import aiChatgpt from "@/assets/svg/marketing/ai-chatgpt.svg"
import aiClaude from "@/assets/svg/marketing/ai-claude.svg"
import aiGemini from "@/assets/svg/marketing/ai-gemini.png"
import aiGrok from "@/assets/svg/marketing/ai-grok.svg"
import aiPerplexity from "@/assets/svg/marketing/ai-perplexity.svg"
import { cn } from "@/lib/utils"

const AI_PROVIDERS = [
  {
    id: "chatgpt",
    label: "ChatGPT",
    src: aiChatgpt,
    kind: "glyph" as const,
  },
  {
    id: "claude",
    label: "Claude",
    src: aiClaude,
    kind: "glyph" as const,
  },
  {
    id: "gemini",
    label: "Gemini",
    src: aiGemini,
    kind: "tile" as const,
  },
  {
    id: "perplexity",
    label: "Perplexity",
    src: aiPerplexity,
    kind: "glyph" as const,
  },
  {
    id: "grok",
    label: "Grok",
    src: aiGrok,
    kind: "glyph" as const,
  },
] as const

type MarketingAskAiIconsProps = {
  className?: string
}

/**
 * Figma footer “Ask AI about Tummly” icon row (`5839:44707`).
 * Destinations are not shipped yet — decorative until links exist.
 */
export function MarketingAskAiIcons({ className }: MarketingAskAiIconsProps) {
  return (
    <ul
      className={cn("m-0 flex list-none items-center gap-3 p-0", className)}
      aria-label="Ask AI about Tummly"
    >
      {AI_PROVIDERS.map((provider) => (
        <li key={provider.id}>
          {provider.kind === "tile" ? (
            <span
              className="block size-[26px] overflow-hidden rounded-[6px]"
              title={provider.label}
            >
              <img
                src={provider.src}
                alt=""
                width={26}
                height={26}
                className="block size-[26px]"
                aria-hidden
              />
              <span className="sr-only">{provider.label}</span>
            </span>
          ) : (
            <span
              className="flex size-[26px] items-center justify-center overflow-hidden rounded-[6px] bg-[#141414]"
              title={provider.label}
            >
              <img
                src={provider.src}
                alt=""
                width={12}
                height={12}
                className="block h-3 w-auto max-w-3 object-contain"
                aria-hidden
              />
              <span className="sr-only">{provider.label}</span>
            </span>
          )}
        </li>
      ))}
    </ul>
  )
}
