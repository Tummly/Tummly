export const validationMessages = {
  email: {
    required: "Email is required.",
    invalid: "Please enter a valid email address.",
  },
  password: {
    required: "Password is required.",
    minLength: "Password must be at least 8 characters",
    mismatch: "Passwords do not match",
  },
  mobile: {
    required: "Mobile number is required.",
    invalid: "Please enter a valid mobile number.",
  },
  url: {
    invalid: "Please enter a valid business URL.",
  },
  otp: {
    incomplete: "Enter the full 6-digit verification code.",
  },
  trialRequest: {
    businessName: {
      required: "Business name is required.",
    },
    businessCategory: {
      required: "Business category is required.",
    },
    locations: {
      required: "Locations field is required.",
    },
    fullName: {
      required: "Full name is required.",
    },
    role: {
      required: "Role is required.",
    },
    goal: {
      required: "Goal is required.",
    },
    terms: {
      required: "You must accept terms and conditions.",
    },
  },
  accountSetup: {
    fullName: {
      required: "Full name is required.",
    },
    restaurantName: {
      required: "Restaurant name is required.",
    },
    locationName: {
      required: "Location name is required.",
    },
    address: {
      required: "Address is required.",
    },
    phone: {
      required: "Phone is required.",
      invalid: "Phone number must be 11 digits",
    },
    businessCategory: {
      required: "Business category is required.",
    },
    thankYouMessage: {
      required: "Thank you message is required",
    },
    groupName: {
      required: "Group name is required.",
    },
    postcode: {
      required: "Postcode is required.",
    },
    rolloutApproach: {
      required: "Select rollout approach",
    },
    offerTitle: {
      required: "Offer title required",
    },
    offerExpiry: {
      required: "Select expiry",
    },
    redemptionMethod: {
      required: "Select redemption method",
    },
    usageLimit: {
      required: "Select usage limit",
    },
    locations: {
      required: "At least one location is required",
    },
    terms: {
      required: "You must accept terms and conditions.",
    },
  },
} as const
