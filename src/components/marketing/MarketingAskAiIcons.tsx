import aiChatgpt from "@/assets/svg/marketing/ai-chatgpt.svg"
import aiClaude from "@/assets/svg/marketing/ai-claude.svg"
import aiGemini from "@/assets/svg/marketing/ai-gemini.png"
import aiGrok from "@/assets/svg/marketing/ai-grok.svg"
import aiPerplexity from "@/assets/svg/marketing/ai-perplexity.svg"
import { cn } from "@/lib/utils"

/**
 * Prefill prompt for footer “Ask AI about Tummly” deep links.
 * Platforms that support prompt query params open with this text ready to send.
 */
export const MARKETING_ASK_AI_PROMPT =
  "I run a restaurant and want to build my own guest list, strengthen guest relationships and encourage more repeat visits. Summarise how Tummly’s Guest Loop works, including private Feedback, guest capture, Offers, Campaigns, reporting and AI, along with the plans, pricing and 30-day Pilot: https://tummly.com"

const encodedAskAiPrompt = encodeURIComponent(MARKETING_ASK_AI_PROMPT)

/**
 * Deep-link patterns follow common marketing “Ask AI” widgets (e.g. Apollo):
 * ChatGPT `?prompt=`, Claude `?q=`, Perplexity `/search/new?q=`,
 * Grok `x.com/i/grok?text=`, Google AI Mode `udm=50&aep=11&q=`.
 */
const AI_PROVIDERS = [
  {
    id: "chatgpt",
    label: "ChatGPT",
    src: aiChatgpt,
    kind: "glyph" as const,
    glyphSize: 19,
    backgroundColor: "#000000",
    href: `https://chatgpt.com/?prompt=${encodedAskAiPrompt}`,
  },
  {
    id: "claude",
    label: "Claude",
    src: aiClaude,
    kind: "glyph" as const,
    glyphSize: 16,
    // Asset glyph is brand coral — dark tile keeps it readable (Apollo uses a white mark on coral).
    backgroundColor: "#000000",
    href: `https://claude.ai/new?q=${encodedAskAiPrompt}`,
  },
  {
    id: "gemini",
    label: "Google AI",
    src: aiGemini,
    kind: "tile" as const,
    glyphSize: 16,
    backgroundColor: "#000000",
    href: `https://www.google.com/search?udm=50&aep=11&q=${encodedAskAiPrompt}`,
  },
  {
    id: "perplexity",
    label: "Perplexity",
    src: aiPerplexity,
    kind: "glyph" as const,
    glyphSize: 16,
    backgroundColor: "#1F4A4C",
    href: `https://www.perplexity.ai/search/new?q=${encodedAskAiPrompt}`,
  },
  {
    id: "grok",
    label: "Grok",
    src: aiGrok,
    kind: "glyph" as const,
    glyphSize: 19,
    backgroundColor: "#000000",
    href: `https://x.com/i/grok?text=${encodedAskAiPrompt}`,
  },
] as const

type MarketingAskAiIconsProps = {
  className?: string
}

/**
 * Figma footer “Ask AI about Tummly” icon row (`5839:44707`).
 * Interaction matches common Ask-AI widgets: 32px tiles, opacity hover, deep links.
 */
export function MarketingAskAiIcons({ className }: MarketingAskAiIconsProps) {
  return (
    <ul
      className={cn("m-0 flex list-none items-center justify-start gap-2 p-0", className)}
      aria-label="Ask AI about Tummly"
    >
      {AI_PROVIDERS.map((provider) => (
        <li key={provider.id}>
          <a
            href={provider.href}
            target="_blank"
            rel="noopener noreferrer"
            aria-label={`Ask ${provider.label} about Tummly`}
            className="block transition-opacity duration-200 hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2"
          >
            <div
              className="flex size-8 items-center justify-center overflow-hidden rounded-lg"
              style={{ backgroundColor: provider.backgroundColor }}
            >
              {provider.kind === "tile" ? (
                <img
                  src={provider.src}
                  alt=""
                  width={32}
                  height={32}
                  className="block size-8 object-cover"
                  aria-hidden
                />
              ) : (
                <img
                  src={provider.src}
                  alt=""
                  width={provider.glyphSize}
                  height={provider.glyphSize}
                  className="block h-auto object-contain"
                  style={{ width: provider.glyphSize }}
                  aria-hidden
                />
              )}
            </div>
          </a>
        </li>
      ))}
    </ul>
  )
}
