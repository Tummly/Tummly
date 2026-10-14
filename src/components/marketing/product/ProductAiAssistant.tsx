import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import { PRODUCT_AI_ASSISTANT } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const aiAssistantImage = remoteSimpleImg("product/ai-assistant")

/** Figma Product AI Assistant `5661:23627`. */
export function ProductAiAssistant() {
  return (
    <ProductSplitFeatureSection
      headingId="product-ai-assistant-heading"
      eyebrow={PRODUCT_AI_ASSISTANT.eyebrow}
      title={PRODUCT_AI_ASSISTANT.title}
      body={PRODUCT_AI_ASSISTANT.body}
      points={PRODUCT_AI_ASSISTANT.points}
      imageSrc={aiAssistantImage.src}
      imageSrcSet={aiAssistantImage.srcSet}
      imageAlt={PRODUCT_AI_ASSISTANT.imageAlt}
      imageWidth={aiAssistantImage.width}
      imageHeight={aiAssistantImage.height}
      cta={{
        label: PRODUCT_AI_ASSISTANT.ctaLabel,
        to: PRODUCT_AI_ASSISTANT.ctaTo,
      }}
    />
  )
}
