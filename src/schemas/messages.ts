export const validationMessages = {
  email: {
    required: "Email is required.",
    invalid: "Please enter a valid email address.",
  },
  password: {
    required: "Password is required.",
    minLength: "Password must be at least 8 characters",
    characterRequirement: "Password must include a number or symbol",
    uppercaseRequired: "Password must include an uppercase letter",
    mismatch: "Passwords do not match",
  },
  mobile: {
    required: "Mobile number is required.",
    invalid: "Please enter a valid UK phone number.",
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
    mainLocation: {
      required: "Main location is required.",
      commitRequired:
        "Select an address from the suggestions or choose Use my address instead.",
    },
    townCity: {
      required: "Town/City is required.",
    },
    postcode: {
      required: "Postcode is required.",
      invalid: "Please enter a valid UK postcode",
    },
  },
  guestFeedback: {
    guestName: {
      required: "Your name is required.",
    },
    contact: {
      required: "Email or phone number is required.",
      invalid: "Please enter a valid email address or UK phone number.",
    },
    comment: {
      required: "Please leave your feedback.",
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
      invalid: "Please enter a valid UK phone number.",
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
    numLocations: {
      required: "Select number of locations.",
    },
    postcode: {
      required: "Postcode is required.",
      invalid: "Please enter a valid UK postcode",
    },
    city: {
      required: "City is required.",
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
