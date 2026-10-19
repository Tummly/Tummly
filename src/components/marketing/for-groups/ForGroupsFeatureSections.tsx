import {
  ArrowLeftRightIcon,
  Building2Icon,
  EyeIcon,
  GitBranchIcon,
  Grid2x2PlusIcon,
  LayoutGridIcon,
  MailIcon,
  MapPinPlusIcon,
  MegaphoneIcon,
  MessageCircleIcon,
  RefreshCwIcon,
  SlidersHorizontalIcon,
  UsersIcon,
} from "lucide-react"
import type { LucideIcon } from "lucide-react"

import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import {
  FOR_GROUPS_AI,
  FOR_GROUPS_CAMPAIGNS,
  FOR_GROUPS_LOCATION_MANAGEMENT,
  FOR_GROUPS_REPORTING,
  FOR_GROUPS_TEAMS,
} from "@/content/marketing/forGroupsPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"

const reportingImage = remoteSimpleImg("for-groups/feature-reporting")
const locationManagementImage = remoteSimpleImg(
  "for-groups/feature-location-management",
)
const teamsImage = remoteSimpleImg("for-groups/feature-teams")
const campaignsImage = remoteSimpleImg("for-groups/feature-campaigns")
const aiImage = remoteSimpleImg("for-groups/feature-ai")

const REPORTING_POINT_ICONS = {
  "compare-activity": ArrowLeftRightIcon,
  "recurring-concerns": RefreshCwIcon,
  "guest-engagement": LayoutGridIcon,
} as const satisfies Record<string, LucideIcon>

const LOCATION_MANAGEMENT_POINT_ICONS = {
  "connected-group": MapPinPlusIcon,
  "set-up-restaurant": GitBranchIcon,
  "expand-control": Grid2x2PlusIcon,
} as const satisfies Record<string, LucideIcon>

const TEAMS_POINT_ICONS = {
  "group-owners": UsersIcon,
  "area-managers": GitBranchIcon,
  "location-managers": Building2Icon,
} as const satisfies Record<string, LucideIcon>

const CAMPAIGNS_POINT_ICONS = {
  "thank-recent": SlidersHorizontalIcon,
  "promote-new": MailIcon,
  "understand-response": EyeIcon,
} as const satisfies Record<string, LucideIcon>

const AI_POINT_ICONS = {
  "understand-feedback": MessageCircleIcon,
  "compare-locations": ArrowLeftRightIcon,
  "prepare-campaigns": MegaphoneIcon,
} as const satisfies Record<string, LucideIcon>

/** Figma For Groups Group-wide visibility `5661:20730` — media left. */
export function ForGroupsReporting() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-reporting-heading"
      eyebrow={FOR_GROUPS_REPORTING.eyebrow}
      title={FOR_GROUPS_REPORTING.title}
      body={FOR_GROUPS_REPORTING.body}
      points={FOR_GROUPS_REPORTING.points}
      pointIcons={REPORTING_POINT_ICONS}
      imageSrc={reportingImage.src}
      imageSrcSet={reportingImage.srcSet}
      imageAlt={FOR_GROUPS_REPORTING.imageAlt}
      imageWidth={reportingImage.width}
      imageHeight={reportingImage.height}
      mediaFirst
      balanceToMedia
    />
  )
}

/** Figma For Groups Location management `5661:21077` — copy left, media right. */
export function ForGroupsLocationManagement() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-location-management-heading"
      eyebrow={FOR_GROUPS_LOCATION_MANAGEMENT.eyebrow}
      title={FOR_GROUPS_LOCATION_MANAGEMENT.title}
      body={FOR_GROUPS_LOCATION_MANAGEMENT.body}
      points={FOR_GROUPS_LOCATION_MANAGEMENT.points}
      pointIcons={LOCATION_MANAGEMENT_POINT_ICONS}
      imageSrc={locationManagementImage.src}
      imageSrcSet={locationManagementImage.srcSet}
      imageAlt={FOR_GROUPS_LOCATION_MANAGEMENT.imageAlt}
      imageWidth={locationManagementImage.width}
      imageHeight={locationManagementImage.height}
      balanceToMedia
      cta={{
        label: FOR_GROUPS_LOCATION_MANAGEMENT.ctaLabel,
        to: FOR_GROUPS_LOCATION_MANAGEMENT.ctaTo,
      }}
    />
  )
}

/** Figma For Groups Team Access `5661:21424` — media left. */
export function ForGroupsTeams() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-teams-heading"
      eyebrow={FOR_GROUPS_TEAMS.eyebrow}
      title={FOR_GROUPS_TEAMS.title}
      body={FOR_GROUPS_TEAMS.body}
      points={FOR_GROUPS_TEAMS.points}
      pointIcons={TEAMS_POINT_ICONS}
      imageSrc={teamsImage.src}
      imageSrcSet={teamsImage.srcSet}
      imageAlt={FOR_GROUPS_TEAMS.imageAlt}
      imageWidth={teamsImage.width}
      imageHeight={teamsImage.height}
      mediaFirst
      balanceToMedia
    />
  )
}

/** Figma For Groups Offers & Campaigns `5661:21775` — copy left, media right. */
export function ForGroupsCampaigns() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-campaigns-heading"
      eyebrow={FOR_GROUPS_CAMPAIGNS.eyebrow}
      title={FOR_GROUPS_CAMPAIGNS.title}
      body={FOR_GROUPS_CAMPAIGNS.body}
      points={FOR_GROUPS_CAMPAIGNS.points}
      pointIcons={CAMPAIGNS_POINT_ICONS}
      imageSrc={campaignsImage.src}
      imageSrcSet={campaignsImage.srcSet}
      imageAlt={FOR_GROUPS_CAMPAIGNS.imageAlt}
      imageWidth={campaignsImage.width}
      imageHeight={campaignsImage.height}
      balanceToMedia
    />
  )
}

/** Figma For Groups AI Assistant `5661:22127` — media left. */
export function ForGroupsAi() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-ai-heading"
      eyebrow={FOR_GROUPS_AI.eyebrow}
      title={FOR_GROUPS_AI.title}
      body={FOR_GROUPS_AI.body}
      points={FOR_GROUPS_AI.points}
      pointIcons={AI_POINT_ICONS}
      imageSrc={aiImage.src}
      imageSrcSet={aiImage.srcSet}
      imageAlt={FOR_GROUPS_AI.imageAlt}
      imageWidth={aiImage.width}
      imageHeight={aiImage.height}
      mediaFirst
      balanceToMedia
      cta={{ label: FOR_GROUPS_AI.ctaLabel, to: FOR_GROUPS_AI.ctaTo }}
    />
  )
}
