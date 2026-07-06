import type { HelpCentreArticle } from "@/types/helpCentre"

/** Hub list order aligned with Figma node 2404:6277 */
export const HELP_CENTRE_HUB_SLUGS = [
  "getting-started",
  "materials-damaged",
  "reorder-materials",
  "qr-code-not-working",
  "privacy-and-data",
  "offer-redemption",
] as const

const HUB_SUMMARY =
  "Tummly Guest Loop helps restaurants turn orders, visits and deliveries into direct guest relationships using QR prompts, private feedback, consented offers, campaigns and weekly insights."

export { HUB_SUMMARY as HELP_CENTRE_HUB_SUMMARY }

export const HELP_CENTRE_ARTICLES: HelpCentreArticle[] = [
  {
    slug: "getting-started",
    title: "Set up your first Smart Guest Link",
    summary: HUB_SUMMARY,
    body: `A Smart Guest Link is the link behind your Tummly QR code. Guests can scan it from a counter card, table tent, receipt, delivery bag, packaging sticker or digital channel to leave quick private feedback, join your customer club and receive a thank-you or offer where enabled.

This guide shows you how to create your first Smart Guest Link and get it ready to use in your restaurant.

## Before you start

You'll need:

- Access to your Tummly account
- At least one restaurant location added
- The name of the location or brand guests will see
- A clear idea of where you want to place the QR code
- Optional: the offer or thank-you message you want guests to receive after submitting feedback

For your first setup, we recommend starting with one location and one simple QR placement, such as a counter card or delivery insert.

## Step 1: Complete Operator Setup

After your trial is approved, complete Operator Setup and sign in to your Tummly dashboard.

## Step 2: Enter your Activation Code

Enter your **Activation Code** when prompted to unlock the operator dashboard.

## Step 3: Test your Smart Guest Link

Open your Smart Guest Link or scan your QR code to test the guest feedback flow.`,
    relatedSlugs: ["privacy-and-data", "offer-redemption"],
  },
  {
    slug: "materials-damaged",
    title: "Report damaged or missing QR materials",
    summary: HUB_SUMMARY,
    body: `## Report damaged or missing materials

If your printed QR materials arrived damaged, were missing from a shipment, or cannot be used, contact Tummly support with:

- Your business name
- The affected location
- A short description of what is damaged or missing
- Photos if available

We will review your request and arrange replacement materials where appropriate.`,
    relatedSlugs: ["reorder-materials", "qr-code-not-working"],
  },
  {
    slug: "reorder-materials",
    title: "Reorder QR cards, stickers or table tents",
    summary: HUB_SUMMARY,
    body: `## Reordering materials

If you need replacement or additional printed QR materials for a new touchpoint, contact us with:

- Your business name
- The location that needs materials
- The type of material you need (counter card, sticker, table tent, etc.)
- How many sets you need

We will confirm availability and shipping details by email.`,
    relatedSlugs: ["materials-damaged", "qr-code-not-working"],
  },
  {
    slug: "qr-code-not-working",
    title: "Fix a QR code that is not scanning",
    summary: HUB_SUMMARY,
    body: `## QR code troubleshooting

### Check the guest link first

Open your Smart Guest Link in a browser. If the link works but the QR does not scan, the issue is usually with print quality or placement.

### Common fixes

- Ensure the QR is at least 3cm square on printed materials.
- Avoid glossy finishes that create glare.
- Test with your phone camera before reporting an issue.

### Still stuck?

Contact us with a photo of the QR and the location name.`,
    relatedSlugs: ["getting-started", "reorder-materials"],
  },
  {
    slug: "privacy-and-data",
    title: "Understand guest consent and opt-outs",
    summary: HUB_SUMMARY,
    body: `## Guest consent and opt-outs

Tummly processes guest feedback and contact data in line with our Privacy Policy. Guests can opt in or out of marketing communications according to your configured consent flows.

For data access, correction, or consent questions, contact us with **I need help with consent, privacy or data** and describe your request.`,
    relatedSlugs: ["offer-redemption", "getting-started"],
  },
  {
    slug: "offer-redemption",
    title: "Get help with offers and redemption",
    summary: HUB_SUMMARY,
    body: `## Offers and redemption

If guests are having trouble redeeming an offer, or you need help configuring thank-you offers and campaigns, contact Tummly support with:

- Your business name
- The location affected
- The offer or campaign name
- What the guest or operator sees when redemption fails

Include screenshots where possible so we can investigate quickly.`,
    relatedSlugs: ["privacy-and-data", "getting-started"],
  },
  {
    slug: "activation-code",
    title: "Account activation explained",
    summary:
      "Learn how activation codes work and what to do if yours has expired.",
    body: `## Activation period

After Operator Setup, Tummly sends starter QR materials and an **Activation Code**. Enter this code after Sign-in to unlock your operator dashboard.

### Activation expired?

If your activation period has ended, contact Tummly support. An admin can extend activation from your trial record.

### Where to enter the code

Sign in, then enter your code on the **Activation Code** screen. You can still reach the Help Centre while waiting to activate.`,
    relatedSlugs: ["getting-started"],
  },
  {
    slug: "guest-feedback",
    title: "Managing guest feedback",
    summary:
      "Understand how private feedback is collected and how to respond to guests.",
    body: `## Guest feedback in Tummly

Guests submit feedback through your Smart Guest Link or QR code. Feedback is private — only your team sees it in the operator dashboard.

### Tips

- Check feedback regularly for each location.
- Use tags and notes to track follow-up internally.

Contact us if feedback is not appearing for a specific location.`,
    relatedSlugs: ["qr-code-not-working"],
  },
  {
    slug: "billing-and-credits",
    title: "Billing and credits",
    summary:
      "Questions about invoices, credits, and subscription changes.",
    body: `## Billing support

For billing or credits questions, contact us from **Contact us** and choose **I have a billing or credits question**.

Include your business name and the email on your account so we can find your record quickly.`,
    relatedSlugs: ["privacy-and-data"],
  },
]

export function getHelpCentreArticle(slug: string) {
  return HELP_CENTRE_ARTICLES.find((article) => article.slug === slug) ?? null
}

export function getHelpCentreHubArticles() {
  return HELP_CENTRE_HUB_SLUGS.map((slug) => getHelpCentreArticle(slug)).filter(
    (article): article is HelpCentreArticle => article != null
  )
}

export function filterHelpCentreArticles(query: string) {
  const normalized = query.trim().toLowerCase()

  if (!normalized) {
    return getHelpCentreHubArticles()
  }

  return HELP_CENTRE_ARTICLES.filter(
    (article) =>
      article.title.toLowerCase().includes(normalized)
      || article.summary.toLowerCase().includes(normalized)
      || article.body.toLowerCase().includes(normalized)
  )
}
