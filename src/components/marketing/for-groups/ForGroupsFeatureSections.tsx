import featureAi from "@/assets/images/marketing/for-groups/feature-ai.png"
import featureCampaigns from "@/assets/images/marketing/for-groups/feature-campaigns.png"
import featureFeedback from "@/assets/images/marketing/for-groups/feature-feedback.png"
import featureReporting from "@/assets/images/marketing/for-groups/feature-reporting.png"
import featureTeams from "@/assets/images/marketing/for-groups/feature-teams.png"
import { ProductSplitFeatureSection } from "@/components/marketing/product/ProductSplitFeatureSection"
import {
  FOR_GROUPS_AI,
  FOR_GROUPS_CAMPAIGNS,
  FOR_GROUPS_FEEDBACK,
  FOR_GROUPS_REPORTING,
  FOR_GROUPS_TEAMS,
} from "@/content/marketing/forGroupsPage"

/** Figma For Groups Group Reporting `5661:20730` — media left. */
export function ForGroupsReporting() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-reporting-heading"
      eyebrow={FOR_GROUPS_REPORTING.eyebrow}
      title={FOR_GROUPS_REPORTING.title}
      body={FOR_GROUPS_REPORTING.body}
      points={FOR_GROUPS_REPORTING.points}
      imageSrc={featureReporting}
      imageAlt={FOR_GROUPS_REPORTING.imageAlt}
      imageWidth={1532}
      imageHeight={1062}
      mediaFirst
    />
  )
}

/** Figma For Groups Feedback across Locations `5661:21077`. */
export function ForGroupsFeedback() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-feedback-heading"
      eyebrow={FOR_GROUPS_FEEDBACK.eyebrow}
      title={FOR_GROUPS_FEEDBACK.title}
      body={FOR_GROUPS_FEEDBACK.body}
      points={FOR_GROUPS_FEEDBACK.points}
      imageSrc={featureFeedback}
      imageAlt={FOR_GROUPS_FEEDBACK.imageAlt}
      imageWidth={1532}
      imageHeight={1214}
    />
  )
}

/** Figma For Groups Teams & Permissions `5661:21424` — media left. */
export function ForGroupsTeams() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-teams-heading"
      eyebrow={FOR_GROUPS_TEAMS.eyebrow}
      title={FOR_GROUPS_TEAMS.title}
      body={FOR_GROUPS_TEAMS.body}
      points={FOR_GROUPS_TEAMS.points}
      imageSrc={featureTeams}
      imageAlt={FOR_GROUPS_TEAMS.imageAlt}
      imageWidth={1532}
      imageHeight={1222}
      mediaFirst
    />
  )
}

/** Figma For Groups Group Campaigns `5661:21775`. */
export function ForGroupsCampaigns() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-campaigns-heading"
      eyebrow={FOR_GROUPS_CAMPAIGNS.eyebrow}
      title={FOR_GROUPS_CAMPAIGNS.title}
      body={FOR_GROUPS_CAMPAIGNS.body}
      points={FOR_GROUPS_CAMPAIGNS.points}
      imageSrc={featureCampaigns}
      imageAlt={FOR_GROUPS_CAMPAIGNS.imageAlt}
      imageWidth={1532}
      imageHeight={1326}
    />
  )
}

/** Figma For Groups Tummly AI `5661:22127` — media left, CTA only. */
export function ForGroupsAi() {
  return (
    <ProductSplitFeatureSection
      headingId="for-groups-ai-heading"
      eyebrow={FOR_GROUPS_AI.eyebrow}
      title={FOR_GROUPS_AI.title}
      body={FOR_GROUPS_AI.body}
      imageSrc={featureAi}
      imageAlt={FOR_GROUPS_AI.imageAlt}
      imageWidth={1532}
      imageHeight={1358}
      mediaFirst
      cta={{ label: FOR_GROUPS_AI.ctaLabel, to: FOR_GROUPS_AI.ctaTo }}
    />
  )
}
