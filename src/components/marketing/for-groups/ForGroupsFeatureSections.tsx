import aiCampaignsIcon from "@/assets/svg/marketing/for-groups/ai-campaigns.svg"
import aiCompareIcon from "@/assets/svg/marketing/for-groups/ai-compare.svg"
import aiFeedbackIcon from "@/assets/svg/marketing/for-groups/ai-feedback.svg"
import campaignsPromoteIcon from "@/assets/svg/marketing/for-groups/campaigns-promote.svg"
import campaignsResponseIcon from "@/assets/svg/marketing/for-groups/campaigns-response.svg"
import campaignsThankIcon from "@/assets/svg/marketing/for-groups/campaigns-thank.svg"
import locationConnectedIcon from "@/assets/svg/marketing/for-groups/location-connected.svg"
import locationExpandIcon from "@/assets/svg/marketing/for-groups/location-expand.svg"
import locationSetupIcon from "@/assets/svg/marketing/for-groups/location-setup.svg"
import reportingCompareIcon from "@/assets/svg/marketing/for-groups/reporting-compare.svg"
import reportingEngagementIcon from "@/assets/svg/marketing/for-groups/reporting-engagement.svg"
import reportingRecurringIcon from "@/assets/svg/marketing/for-groups/reporting-recurring.svg"
import teamsAreaIcon from "@/assets/svg/marketing/for-groups/teams-area.svg"
import teamsLocationIcon from "@/assets/svg/marketing/for-groups/teams-location.svg"
import teamsOwnersIcon from "@/assets/svg/marketing/for-groups/teams-owners.svg"
import {
  ProductSplitFeatureSection,
  type FeaturePointIconImg,
} from "@/components/marketing/product/ProductSplitFeatureSection"
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

/** Figma For Groups point glyphs — root width/height from export. */
const REPORTING_POINT_ICONS = {
  "compare-activity": {
    src: reportingCompareIcon,
    width: 22,
    height: 20,
  },
  "recurring-concerns": {
    src: reportingRecurringIcon,
    width: 18,
    height: 18,
  },
  "guest-engagement": {
    src: reportingEngagementIcon,
    width: 20,
    height: 20,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const LOCATION_MANAGEMENT_POINT_ICONS = {
  "connected-group": {
    src: locationConnectedIcon,
    width: 14,
    height: 19,
  },
  "set-up-restaurant": {
    src: locationSetupIcon,
    width: 20,
    height: 19,
  },
  "expand-control": {
    src: locationExpandIcon,
    width: 18,
    height: 18,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const TEAMS_POINT_ICONS = {
  "group-owners": {
    src: teamsOwnersIcon,
    width: 19,
    height: 15,
  },
  "area-managers": {
    src: teamsAreaIcon,
    width: 20,
    height: 19,
  },
  "location-managers": {
    src: teamsLocationIcon,
    width: 18,
    height: 18,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const CAMPAIGNS_POINT_ICONS = {
  "thank-recent": {
    src: campaignsThankIcon,
    width: 18,
    height: 18,
  },
  "promote-new": {
    src: campaignsPromoteIcon,
    width: 18,
    height: 16,
  },
  "understand-response": {
    src: campaignsResponseIcon,
    width: 21,
    height: 15,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const AI_POINT_ICONS = {
  "understand-feedback": {
    src: aiFeedbackIcon,
    width: 18,
    height: 19,
  },
  "compare-locations": {
    src: aiCompareIcon,
    width: 18,
    height: 18,
  },
  "prepare-campaigns": {
    src: aiCampaignsIcon,
    width: 18,
    height: 16,
  },
} as const satisfies Record<string, FeaturePointIconImg>

/** Figma For Groups Group-wide visibility `5661:20730` — media left. */
export function ForGroupsReporting() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-reporting-heading"
      eyebrow={FOR_GROUPS_REPORTING.eyebrow}
      title={FOR_GROUPS_REPORTING.title}
      body={FOR_GROUPS_REPORTING.body}
      points={FOR_GROUPS_REPORTING.points}
      pointIconImgs={REPORTING_POINT_ICONS}
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
      pointIconImgs={LOCATION_MANAGEMENT_POINT_ICONS}
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
      pointIconImgs={TEAMS_POINT_ICONS}
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
      pointIconImgs={CAMPAIGNS_POINT_ICONS}
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
      pointIconImgs={AI_POINT_ICONS}
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
