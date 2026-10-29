import bandImage from "@/assets/images/marketing/product/ai-assistant.webp"
import askAboutFeedbackIcon from "@/assets/svg/marketing/product/icons/ask-about-feedback.svg"
import understandFiguresIcon from "@/assets/svg/marketing/product/icons/understand-figures.svg"
import prepareWordingIcon from "@/assets/svg/marketing/product/icons/prepare-wording.svg"
import { ProductFeatureBand } from "@/components/marketing/product/ProductFeatureBand"
import { PRODUCT_AI_ASSISTANT } from "@/content/marketing/productPage"

/** Figma Product AI Assistant `5661:23627`. */
export function ProductAiAssistant() {
  return (
    <ProductFeatureBand
      headingId="product-ai-assistant-heading"
      eyebrow={PRODUCT_AI_ASSISTANT.eyebrow}
      title={PRODUCT_AI_ASSISTANT.title}
      body={PRODUCT_AI_ASSISTANT.body}
      points={PRODUCT_AI_ASSISTANT.points}
      pointIcons={{
        "ask-about-feedback": askAboutFeedbackIcon,
        "understand-figures": understandFiguresIcon,
        "prepare-wording": prepareWordingIcon,
      }}
      media={{
        src: bandImage,
        alt: PRODUCT_AI_ASSISTANT.imageAlt,
        width: 2400,
        height: 2400,
        imageClassName: "left-[5.97%] top-[1.17%] h-[102.69%] w-[88.06%]",
        plateClassName: "bg-[#ecebe6]",
      }}
      mediaFirst
      cta={{
        label: PRODUCT_AI_ASSISTANT.ctaLabel,
        to: PRODUCT_AI_ASSISTANT.ctaTo,
      }}
    />
  )
}
