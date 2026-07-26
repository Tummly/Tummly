/** Display label for Feedback sentiment wire values. Returns null for unknown. */
export function feedbackSentimentLabel(
  sentiment: string
): string | null {
  switch (sentiment) {
    case "positive":
      return "Positive"
    case "neutral":
      return "Neutral"
    case "negative":
      return "Negative"
    default:
      return null
  }
}
