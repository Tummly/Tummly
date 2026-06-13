import type { SelectOption } from "@/components/ui/floating-label-select"

export const BUSINESS_CATEGORY_OPTIONS: SelectOption[] = [
  { value: "takeaway", label: "Takeaway / quick-service restaurant" },
  { value: "cafe", label: "Café / coffee shop" },
  { value: "bakery", label: "Bakery / dessert shop" },
  { value: "casual-dining", label: "Casual dining restaurant" },
  { value: "food-truck", label: "Food truck / mobile food business" },
  { value: "pub-bar", label: "Pub / bar / hospitality venue" },
  { value: "multi-site", label: "Multi-site restaurant group" },
  { value: "other", label: "Other" },
]

export const LOCATION_COUNT_OPTIONS: SelectOption[] = [
  { value: "1", label: "1 location" },
  { value: "2-5", label: "2–5 locations" },
  { value: "6-10", label: "6–10 locations" },
  { value: "11-30", label: "11–30 locations" },
  { value: "31+", label: "31+ locations" },
]

export const ROLE_OPTIONS: SelectOption[] = [
  { value: "owner-operator", label: "Owner / operator" },
  { value: "founder-director", label: "Founder / director" },
  { value: "general-manager", label: "General manager" },
  { value: "area-operations-manager", label: "Area / operations manager" },
  { value: "marketing-growth", label: "Marketing / growth" },
  { value: "admin-support", label: "Admin / support" },
  { value: "agency-consultant", label: "Agency / consultant" },
  { value: "other", label: "Other" },
]

export const MAIN_GOAL_OPTIONS: SelectOption[] = [
  { value: "grow-guest-list", label: "Grow our guest list" },
  { value: "collect-feedback", label: "Collect private feedback" },
  { value: "return-offers", label: "Bring guests back with offers" },
  { value: "follow-up-feedback", label: "Follow up after poor feedback" },
  { value: "setup-qr", label: "Set up QR prompts and guest links" },
  { value: "manage-locations", label: "Manage multiple locations" },
  { value: "help-choose", label: "Help me choose the right setup" },
]
