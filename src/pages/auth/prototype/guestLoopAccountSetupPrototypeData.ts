/**
 * PROTOTYPE — throwaway QA fixtures for Guest Loop Account Setup.
 * Delete when the prototype has served its purpose.
 */
import type { SetupTokenPrefill } from "@/lib/setupToken"
import {
  accountSetupMultiDefaultValues,
  emptyLocationItem,
  type AccountSetupMultiFormValues,
} from "@/schemas/accountSetupMulti"
import {
  accountSetupSingleDefaultValues,
  type AccountSetupSingleFormValues,
} from "@/schemas/accountSetupSingle"

export const PROTOTYPE_SETUP_TOKEN = "prototype-qa-setup-token"

export const PROTOTYPE_QA_PASSWORD = "QA-TestPass-12!"

export const SINGLE_QA_PREFILL: SetupTokenPrefill = {
  email: "qa.single.operator@tummly.test",
  fullName: "Jordan Singh",
  businessName: "The Golden Fork",
  mobile: "07700900123",
  businessCategory: "takeaway",
  accountType: "Single",
}

export const MULTI_QA_PREFILL: SetupTokenPrefill = {
  email: "qa.multi.operator@tummly.test",
  fullName: "Alex Morgan",
  businessName: "Golden Fork Group",
  mobile: "07700900456",
  businessCategory: "multi-site",
  accountType: "Multi",
  numLocations: "2-5",
}

export function buildPrototypeSingleFormValues(): AccountSetupSingleFormValues {
  return {
    ...accountSetupSingleDefaultValues,
    token: PROTOTYPE_SETUP_TOKEN,
    email: SINGLE_QA_PREFILL.email,
    fullName: SINGLE_QA_PREFILL.fullName,
    restaurantName: SINGLE_QA_PREFILL.businessName,
    locationName: "High Street",
    address: "12 Market Street",
    postcode: "SW1A 1AA",
    phone: SINGLE_QA_PREFILL.mobile,
    businessCategory: SINGLE_QA_PREFILL.businessCategory,
    businessLink: "",
    password: PROTOTYPE_QA_PASSWORD,
    confirmPassword: PROTOTYPE_QA_PASSWORD,
    agree: true,
  }
}

export function buildPrototypeMultiFormValues(): AccountSetupMultiFormValues {
  return {
    ...accountSetupMultiDefaultValues,
    token: PROTOTYPE_SETUP_TOKEN,
    email: MULTI_QA_PREFILL.email,
    fullName: MULTI_QA_PREFILL.fullName,
    groupName: MULTI_QA_PREFILL.businessName,
    primaryPhone: MULTI_QA_PREFILL.mobile,
    businessCategory: MULTI_QA_PREFILL.businessCategory,
    numLocations: MULTI_QA_PREFILL.numLocations ?? "2-5",
    businessLink: "",
    password: PROTOTYPE_QA_PASSWORD,
    confirmPassword: PROTOTYPE_QA_PASSWORD,
    agree: true,
    locations: [
      {
        ...emptyLocationItem,
        locationName: "High Street",
        address: "12 Market Street",
        postcode: "SW1A 1AA",
        locationPhone: "02079460001",
        localContact: "Site manager",
      },
      {
        ...emptyLocationItem,
        locationName: "Harbour Side",
        address: "3 Pier Road",
        postcode: "BN1 1AA",
      },
    ],
  }
}

export function buildPrototypePrefillFormValues(
  accountType: "Single" | "Multi"
): AccountSetupSingleFormValues | AccountSetupMultiFormValues {
  if (accountType === "Multi") {
    return {
      ...accountSetupMultiDefaultValues,
      token: PROTOTYPE_SETUP_TOKEN,
      email: MULTI_QA_PREFILL.email,
      fullName: MULTI_QA_PREFILL.fullName,
      groupName: MULTI_QA_PREFILL.businessName,
      primaryPhone: MULTI_QA_PREFILL.mobile,
      businessCategory: MULTI_QA_PREFILL.businessCategory,
      numLocations: MULTI_QA_PREFILL.numLocations ?? "",
    }
  }

  return {
    ...accountSetupSingleDefaultValues,
    token: PROTOTYPE_SETUP_TOKEN,
    email: SINGLE_QA_PREFILL.email,
    fullName: SINGLE_QA_PREFILL.fullName,
    restaurantName: SINGLE_QA_PREFILL.businessName,
    locationName: SINGLE_QA_PREFILL.businessName,
    phone: SINGLE_QA_PREFILL.mobile,
    businessCategory: SINGLE_QA_PREFILL.businessCategory,
  }
}
