import { forGroupsAiImage } from "@/assets/for-groups-images"
import aiConcernIcon from "@/assets/svg/marketing/for-groups/ai-concern.svg"
import aiFollowUpIcon from "@/assets/svg/marketing/for-groups/ai-follow-up.svg"
import aiLocalDetailIcon from "@/assets/svg/marketing/for-groups/ai-local-detail.svg"
import campaignsAudienceIcon from "@/assets/svg/marketing/for-groups/campaigns-audience.svg"
import campaignsConfirmIcon from "@/assets/svg/marketing/for-groups/campaigns-confirm.svg"
import campaignsScopeIcon from "@/assets/svg/marketing/for-groups/campaigns-scope.svg"
import locationAddIcon from "@/assets/svg/marketing/for-groups/location-add.svg"
import locationDistinctIcon from "@/assets/svg/marketing/for-groups/location-distinct.svg"
import locationStartIcon from "@/assets/svg/marketing/for-groups/location-start.svg"
import reportingCompareIcon from "@/assets/svg/marketing/for-groups/reporting-compare.svg"
import reportingFocusIcon from "@/assets/svg/marketing/for-groups/reporting-focus.svg"
import reportingLookBehindIcon from "@/assets/svg/marketing/for-groups/reporting-look-behind.svg"
import teamsAssignedIcon from "@/assets/svg/marketing/for-groups/teams-assigned.svg"
import teamsBillingIcon from "@/assets/svg/marketing/for-groups/teams-billing.svg"
import teamsStaffTaskIcon from "@/assets/svg/marketing/for-groups/teams-staff-task.svg"
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

/** Figma For Groups point glyphs — root width/height from export. */
const REPORTING_POINT_ICONS = {
  "compare-restaurants": {
    src: reportingCompareIcon,
    width: 22.45,
    height: 19.515,
  },
  "look-behind-change": {
    src: reportingLookBehindIcon,
    width: 17.5,
    height: 17.5137,
  },
  "focus-review": {
    src: reportingFocusIcon,
    width: 19.5,
    height: 19.5,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const LOCATION_MANAGEMENT_POINT_ICONS = {
  "start-with-one": {
    src: locationStartIcon,
    width: 14.5,
    height: 19,
  },
  "add-more": {
    src: locationAddIcon,
    width: 20.1,
    height: 19.3,
  },
  "keep-distinct": {
    src: locationDistinctIcon,
    width: 18.5,
    height: 18.5,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const TEAMS_POINT_ICONS = {
  "assigned-restaurants": {
    src: teamsAssignedIcon,
    width: 18.5178,
    height: 15,
  },
  "staff-task": {
    src: teamsStaffTaskIcon,
    width: 20.1,
    height: 19.3,
  },
  "restricted-billing": {
    src: teamsBillingIcon,
    width: 18.5,
    height: 18.5,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const CAMPAIGNS_POINT_ICONS = {
  "restaurant-scope": {
    src: campaignsScopeIcon,
    width: 18.5,
    height: 17.5,
  },
  "match-audience": {
    src: campaignsAudienceIcon,
    width: 18.2307,
    height: 16.4073,
  },
  "confirm-details": {
    src: campaignsConfirmIcon,
    width: 21,
    height: 15,
  },
} as const satisfies Record<string, FeaturePointIconImg>

const AI_POINT_ICONS = {
  "recurring-concern": {
    src: aiConcernIcon,
    width: 18.4875,
    height: 19.0188,
  },
  "local-detail": {
    src: aiLocalDetailIcon,
    width: 18.3222,
    height: 17.6636,
  },
  "prepare-follow-up": {
    src: aiFollowUpIcon,
    width: 18.3601,
    height: 15.5039,
  },
} as const satisfies Record<string, FeaturePointIconImg>

/** Figma point body: Helvetica 14/18 (Group-wide visibility keeps 14/20). */
const POINT_BODY_TIGHT = "leading-[18px]"

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
      fixedCopyHeight
      bodyClassName="max-w-[612px]"
    />
  )
}

/** Figma For Groups Adding your restaurants `5661:21077` — copy left, media right. */
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
      bodyClassName="max-w-[638px]"
      pointBodyClassName={POINT_BODY_TIGHT}
      cta={{
        label: FOR_GROUPS_LOCATION_MANAGEMENT.ctaLabel,
        to: FOR_GROUPS_LOCATION_MANAGEMENT.ctaTo,
      }}
    />
  )
}

/** Figma For Groups Team access `5661:21424` — media left. */
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
      fixedCopyHeight
      bodyClassName="max-w-[612px]"
      pointBodyClassName={POINT_BODY_TIGHT}
    />
  )
}

/** Figma For Groups Offers & campaigns `5661:21775` — copy left, media right. */
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
      fixedCopyHeight
      bodyClassName="max-w-[612px]"
      pointBodyClassName={POINT_BODY_TIGHT}
    />
  )
}

/** Figma For Groups AI assistant `5661:22127` — media left. */
export function ForGroupsAi() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-ai-heading"
      eyebrow={FOR_GROUPS_AI.eyebrow}
      title={FOR_GROUPS_AI.title}
      body={FOR_GROUPS_AI.body}
      points={FOR_GROUPS_AI.points}
      pointIconImgs={AI_POINT_ICONS}
      imageSrc={forGroupsAiImage.src}
      imageSrcSet={forGroupsAiImage.srcSet}
      imageAlt={FOR_GROUPS_AI.imageAlt}
      imageWidth={forGroupsAiImage.width}
      imageHeight={forGroupsAiImage.height}
      mediaFirst
      balanceToMedia
      fixedCopyHeight
      ctaGapClassName="gap-[51px]"
      contentClassName="lg:py-[84.5px]"
      pointBodyClassName={POINT_BODY_TIGHT}
      cta={{ label: FOR_GROUPS_AI.ctaLabel, to: FOR_GROUPS_AI.ctaTo }}
    />
  )
}
