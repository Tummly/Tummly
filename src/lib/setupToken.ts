import {
  readFirstString,
  readString,
  unwrapDataObject,
} from "@/lib/apiEnvelope";

export type SetupAccountType = "Single" | "Multi";

export interface SetupTokenPrefill {
  email: string;
  fullName: string;
  businessName: string;
  mobile: string;
  businessCategory: string;
  accountType: SetupAccountType;
  /** Location-count band from the Trial Request (e.g. "2-5"). Present for multi-location invites. */
  numLocations?: string;
}

function normalizeAccountType(value: string): SetupAccountType | null {
  const normalized = value.trim().toLowerCase();

  if (normalized === "single") {
    return "Single";
  }

  if (normalized === "multi") {
    return "Multi";
  }

  return null;
}

export function parseValidateSetupTokenResponse(
  payload: unknown,
): SetupTokenPrefill | null {
  const nested = unwrapDataObject(payload);

  if (!nested) {
    return null;
  }

  const accountTypeRaw = readString(nested, "accountType");
  const accountType = accountTypeRaw
    ? normalizeAccountType(accountTypeRaw)
    : null;

  if (!accountType) {
    return null;
  }

  const email = readString(nested, "email");
  const fullName = readString(nested, "fullName") ?? "";
  const businessName =
    readFirstString(nested, [
      "businessName",
      "restaurantName",
      "groupName",
    ]) ?? "";
  const mobile = readString(nested, "mobile") ?? "";
  const businessCategory = readString(nested, "businessCategory") ?? "";
  const numLocations = readFirstString(nested, [
    "locations",
    "numLocations",
  ]);

  if (!email) {
    return null;
  }

  return {
    email,
    fullName,
    businessName,
    mobile,
    businessCategory,
    accountType,
    ...(numLocations ? { numLocations } : {}),
  };
}

export function getSetupAccountPath(
  accountType: SetupAccountType,
  token: string,
): string {
  const encodedToken = encodeURIComponent(token);
  const basePath =
    accountType === "Single" ? "/setup-account-single" : "/setup-account-multi";

  return `${basePath}?token=${encodedToken}`;
}

export function getSetupTokenErrorMessage(error: unknown): string {
  if (
    error &&
    typeof error === "object" &&
    "response" in error &&
    error.response &&
    typeof error.response === "object" &&
    "data" in error.response
  ) {
    const data = error.response.data;

    if (data && typeof data === "object" && "message" in data) {
      const message = data.message;
      if (typeof message === "string" && message.trim()) {
        return message.trim();
      }
    }
  }

  if (error instanceof Error && error.message.trim()) {
    if (error.message.toLowerCase().includes("network error")) {
      return "Unable to reach the server. Check your connection and try again.";
    }

    return error.message.trim();
  }

  return "This setup link is invalid or has expired.";
}
