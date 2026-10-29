/** Doc 08 custom plan quote copy. Strings are exact; edit only with the doc. */
export const CUSTOM_PLAN_QUOTE_COPY = {
  loading: "Updating your quote…",
  failed:
    "We could not load a current quote. Your selections are saved. Try again.",
  retry: "Try again",
  changed:
    "Your quote has changed. Review the updated total before continuing.",
  changedConfirm: "I have reviewed the updated total.",
  optionUnavailable:
    "This option is no longer available. Review the current capacity options and confirm your updated total before continuing.",
  restoreFailed:
    "We could not restore your selected plan. Review your choices before continuing.",
  noSelection: "Choose a Pilot or paid plan to continue.",
  locationNotWhole: "Enter a whole number of Locations.",
  locationOverLimit: "Maximum online plan size is 500 Locations",
  locationOverLimitHelp:
    "Reduce the count to continue online, or contact us about your requirements.",
  contactUs: "Contact us",
  contactUsHref: "/help-center/contact?topic=plans-and-pricing",
  locationEmpty: "Add at least 1 Location to customise your estimate.",
  packEmpty: "Enter a number of packs. Use 0 for none.",
  packNotWhole: "Enter a whole number of packs.",
  packOverLimit: "Maximum 99 packs per option.",
  unitEmpty: "Enter a whole number of units, starting from 1.",
  unitNotWhole: "Enter a whole number of units, starting from 1.",
  unitOverLimit:
    "This quantity exceeds the available limit. Reduce the quantity or review your capacity options.",
  /** Signup review: the server quote could not be confirmed. Nothing is charged. */
  quoteUnverified:
    "We couldn't confirm your updated total. Refresh your quote and try again. No payment has been taken.",
  /** Labels for the full server quote shown beside the price confirmation. */
  breakdown: {
    linesHeading: "Plan and selected extras",
    quantity: "Quantity",
    pricePerUnit: "Price per unit",
    lineTotal: "Line total",
    billingPeriod: "Billing period",
    net: "Total before VAT",
    vat: "VAT",
    updatedTotal: "Updated total",
    effectiveDate: "Effective date",
    effectiveOnPayment: "When payment is confirmed",
    newRecurring: "New recurring amount",
    dueToday: "Amount due today",
  },
  noPaymentTaken:
    "No payment is taken until you confirm your subscription and payment details at checkout.",
  /** Temporary until Director D7; shown only when vatRateBps > 0. */
  taxExcluded: "Prices exclude VAT. VAT is added at checkout.",
}
