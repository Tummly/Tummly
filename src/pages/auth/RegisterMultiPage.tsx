import { useState } from "react";
import type { ChangeEvent, CSSProperties } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { AUTH_API_BASE_URL } from "../../config/api";
import type { CompleteSetupLocation } from "../../types/trial";
import { Button } from "@/components/ui/button";

type FormErrors = Record<string, string>;

interface MultiFormData {
  email: string;
  fullName: string;
  password: string;
  confirmPassword: string;
  agree: boolean;
  groupName: string;
  businessCategory: string;
  numLocations: string;
  primaryPhone: string;
  businessLink: string;
  rolloutApproach: string;
  thankYouMessage: string;
  offerType: string;
  offerTitle: string;
  offerMessage: string;
  expiry: string;
  redemptionMethod: string;
  usageLimit: string;
  guestPrompt: string;
  guestPreview: string;
}

const MultiRegisterPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const token = searchParams.get("token");

  const [formData, setFormData] = useState<MultiFormData>({
    email: "Mohamed@gmail.com",
    fullName: "Mohamed@gmail.com",
    password: "",
    confirmPassword: "",
    agree: false,

    groupName: "",
    businessCategory: "",
    numLocations: "",
    primaryPhone: "",
    businessLink: "",

    rolloutApproach: "",
    thankYouMessage:
      "Thanks for your feedback. We appreciate you taking a moment to help us improve.",
    offerType: "",
    offerTitle: "",
    offerMessage: "",
    expiry: "",
    redemptionMethod: "",
    usageLimit: "",
    guestPrompt: "",
    guestPreview: "",
  });
const handleSubmit = async () => {
try {
const payload = {
token: token,
  password: formData.password,
  confirmPassword: formData.confirmPassword,

  groupName: formData.groupName,
  businessCategory: formData.businessCategory,
  numLocations: formData.numLocations,
  primaryPhone: formData.primaryPhone,
  businessLink: formData.businessLink,

  rolloutApproach: formData.rolloutApproach,
  guestPrompt: formData.guestPrompt,
  thankYouMessage: formData.thankYouMessage,

  feedbackItems: Object.keys(feedbackItems).filter(
    (key) => feedbackItems[key]
  ),

  offerType: formData.offerType,
  offerTitle: formData.offerTitle,
  offerMessage: formData.offerMessage,
  offerExpiry: formData.expiry,
  redemptionMethod: formData.redemptionMethod,
  usageLimit: formData.usageLimit,
  guestPreview: formData.guestPreview,

  locations: locations,
};

console.log("FINAL PAYLOAD:", payload);

const response = await fetch(
  `${AUTH_API_BASE_URL}/setup-account`,
  {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  }
);

const data = await response.json();

console.log("RESPONSE:", data);

if (response.ok) {
  alert("Account setup successful");

  navigate("/multi-dashboard");
} else {
  alert(data.message || "Failed");
}


} catch (error) {
console.error("SUBMIT ERROR:", error);


alert("Server Error");


}
};

  const [locations, setLocations] = useState<CompleteSetupLocation[]>([
    {
      locationName: "",
      address: "",
      postcode: "",
      locationPhone: "",
      localContact: "",
      includeInRollout: true,

    },
  ]);
const [openSection, setOpenSection] = useState<Record<string, boolean>>({
  rollout: true,
  prompts: false,
  feedback: false,
  thankyou: false,
  offer: false,
});

const toggleSection = (section: string) => {
  setOpenSection((prev) => ({
    ...prev,
    [section]: !prev[section],
  }));
};

const [feedbackItems, setFeedbackItems] = useState<Record<string, boolean>>({
  rating: true,
  issueTags: true,
  comment: true,
  firstName: true,
  contact: true,
  consent: true,
});

const toggleFeedbackItem = (key: string) => {
  setFeedbackItems((prev) => ({
    ...prev,
    [key]: !prev[key],
  }));
};

  const [step, setStep] = useState(1);
  const [errors, setErrors] = useState<FormErrors>({});

// =========================
// STYLES (FIXED MISSING CONSTS)
// =========================

const inputStyle: CSSProperties = {
  width: "100%",
  height: "44px",
  border: "1px solid #D9D9D9",
  borderRadius: "6px",
  padding: "0 12px",
  fontSize: "14px",
  outline: "none",
  marginTop: "6px",
  background: "#FFFFFF",
  boxSizing: "border-box",
};

const labelStyle = {
  fontSize: "13px",
  color: "#6B6B6B",
  fontWeight: "500",
};

const errorStyle = {
  color: "#DC2626",
  fontSize: "12px",
  marginTop: "6px",
};


// =========================
// PASSWORD STRENGTH BAR COLOR
// =========================

const strength = (() => {
  const password = formData.password;
  let s = 0;

  if (password.length >= 8) s++;
  if (/[A-Z]/.test(password)) s++;
  if (/[0-9]/.test(password)) s++;
  if (/[^A-Za-z0-9]/.test(password)) s++;

  return s;
})();

const getBarColor = (index: number) => {
  if (strength >= index) {
    if (strength === 1) return "#EF4444";
    if (strength === 2) return "#F59E0B";
    if (strength >= 3) return "#22C55E";
  }
  return "#E5E5E5";
};

  // ================= INPUT =================
  const handleInput = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target;
    const checked = type === "checkbox" ? (e.target as HTMLInputElement).checked : undefined;

    setFormData({
      ...formData,
      [name]: type === "checkbox" ? checked : value,
    });
  };

  const handleLocationChange = (index: number, e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setLocations((prev) =>
      prev.map((loc, i) =>
        i === index
          ? { ...loc, [name]: value }
          : loc
      )
    );
  };

  const addLocation = () => {
    setLocations([
      ...locations,
      {
        locationName: "",
        address: "",
        postcode: "",
        locationPhone: "",
        localContact: "",
        includeInRollout: true,

      },
    ]);
  };

  
const deleteLocation = (index: number) => {
  if (locations.length === 1) return;

  const updated = locations.filter(
    (_, i) => i !== index
  );

  setLocations(updated);
};

const toggleRollout = (index: number) => {
  const updatedLocations = [...locations];

  updatedLocations[index].includeInRollout =
    !updatedLocations[index].includeInRollout;

  setLocations(updatedLocations);
};

  // ================= VALIDATION =================
  const validateStep1 = () => {
    const err: FormErrors = {};

    if (!formData.email) err.email = "Email required";
    if (!formData.fullName) err.fullName = "Name required";
    if (formData.password.length < 8)
      err.password = "Min 8 characters";
    if (formData.password !== formData.confirmPassword)
      err.confirmPassword = "Passwords not match";
    if (!formData.agree) err.agree = "Accept terms";

    setErrors(err);
    return Object.keys(err).length === 0;
  };

  const validateStep2 = () => {
    const err: FormErrors = {};

    if (!formData.groupName) err.groupName = "Required";
    if (!formData.businessCategory)
      err.businessCategory = "Select category";

    setErrors(err);
    return Object.keys(err).length === 0;
  };

  const validateStep3 = () => {
    const err: FormErrors = {};

    locations.forEach((loc, i) => {
      if (!loc.locationName)
        err[`locationName${i}`] = "Required";
      if (!loc.address)
        err[`address${i}`] = "Required";
      if (!loc.postcode)
        err[`postcode${i}`] = "Required";
    });

    setErrors(err);
    return Object.keys(err).length === 0;
  };

const validateStep4 = () => {
  const err: FormErrors = {};

  if (!formData.rolloutApproach) {
    err.rolloutApproach = "Select rollout approach";
  }

  if (formData.offerType) {
    if (!formData.offerTitle)
      err.offerTitle = "Offer title required";

    if (!formData.expiry)
      err.expiry = "Select expiry";

    if (!formData.redemptionMethod)
      err.redemptionMethod = "Select redemption method";

    if (!formData.usageLimit)
      err.usageLimit = "Select usage limit";
  }

  setErrors(err);
  return Object.keys(err).length === 0;
};
  // ================= NEXT STEP =================
  const nextStep = () => {
    if (step === 1 && !validateStep1()) return;
    if (step === 2 && !validateStep2()) return;
    if (step === 3 && !validateStep3()) return;
    if (step === 4 && !validateStep4()) return;

    setStep((s) => s + 1);
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#F8F8F8",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        padding: "40px 20px",
        fontFamily:
          "Inter, Arial, sans-serif",
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: "480px",
        }}
      >
        {/* STEP 1 */}

        {step === 1 && (
          <div>
            {/* TITLE */}

            <h1
              style={{
                fontSize: "34px",
                fontWeight: "700",
                color: "#202020",
                marginBottom: "14px",
                textAlign: "center",
              }}
            >
              Create your account
            </h1>

            {/* SUBTITLE */}

            <p
              style={{
                fontSize: "13px",
                lineHeight: "24px",
                color: "#4B4B4B",
                textAlign: "center",
                marginBottom: "24px",
              }}
            >
              Your multi-location setup request has
              been approved.
              <br />
              Create a password to access your
              Tummly workspace.
            </p>

            {/* EMAIL */}

            <div style={{ marginBottom: "18px" }}>
              <label style={labelStyle}>
                mohamedemail@gmail.com
              </label>

              <input
                style={inputStyle}
                name="email"
                value={formData.email}
                onChange={handleInput}
                placeholder="Email"
              />

              {errors.email && (
                <div style={errorStyle}>
                  {errors.email}
                </div>
              )}
            </div>

            {/* FULL NAME */}

            <div style={{ marginBottom: "18px" }}>
              <label style={labelStyle}>
                Your full name
              </label>

              <input
                style={inputStyle}
                name="fullName"
                value={formData.fullName}
                onChange={handleInput}
                placeholder="Full name"
              />

              {errors.fullName && (
                <div style={errorStyle}>
                  {errors.fullName}
                </div>
              )}
            </div>

            {/* PASSWORD */}

            <div style={{ marginBottom: "18px" }}>
              <input
                type="password"
                style={inputStyle}
                name="password"
                value={formData.password}
                onChange={handleInput}
                placeholder="Password"
              />

              {/* PASSWORD STRENGTH DASHES */}

              <div
                style={{
                  display: "flex",
                  gap: "8px",
                  marginTop: "12px",
                }}
              >
                {[1, 2, 3, 4].map((item) => (
                  <div
                    key={item}
                    style={{
                      flex: 1,
                      height: "4px",
                      borderRadius: "999px",
                      background: getBarColor(item),
                    }}
                  />
                ))}
              </div>

              <p
                style={{
                  marginTop: "10px",
                  fontSize: "14px",
                  color: "#444",
                }}
              >
                Use at least 8 characters,
                including a number or symbol.
              </p>

              {errors.password && (
                <div style={errorStyle}>
                  {errors.password}
                </div>
              )}
            </div>

            {/* CONFIRM PASSWORD */}

            <div style={{ marginBottom: "24px" }}>
              <input
                type="password"
                style={inputStyle}
                name="confirmPassword"
                value={
                  formData.confirmPassword
                }
                onChange={handleInput}
                placeholder="Confirm password"
              />

              {errors.confirmPassword && (
                <div style={errorStyle}>
                  {errors.confirmPassword}
                </div>
              )}
            </div>

            {/* TERMS */}

            <div
              style={{
                display: "flex",
                alignItems: "flex-start",
                gap: "10px",
                marginBottom: "34px",
              }}
            >
              <input
                type="checkbox"
                name="agree"
                checked={formData.agree}
                onChange={handleInput}
                style={{
                  marginTop: "4px",
                }}
              />

              <div
                style={{
                  fontSize: "14px",
                  color: "#303030",
                }}
              >
                I agree to the{" "}
                <span
                  style={{
                    textDecoration: "underline",
                    fontWeight: "600",
                  }}
                >
                  Terms
                </span>{" "}
                and{" "}
                <span
                  style={{
                    textDecoration: "underline",
                    fontWeight: "600",
                  }}
                >
                  Privacy Notice.
                </span>
              </div>
            </div>

            {errors.agree && (
              <div
                style={{
                  ...errorStyle,
                  marginBottom: "20px",
                }}
              >
                {errors.agree}
              </div>
            )}

            {/* STEP BAR */}

            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent:
                  "space-between",
                marginBottom: "28px",
                color: "#6A6A6A",
                fontSize: "14px",
              }}
            >
              <div>1 Account</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>2 Group</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>3 Locations</div>

              <div
                style={{
                  flex: 1,
                  height: "1px",
                  background: "#D9D9D9",
                  margin: "0 10px",
                }}
              />

              <div>4 Rollout</div>
            </div>

            {/* BUTTON */}

            <Button variant="secondary" size="auth-sm" onClick={nextStep}>
              Create account
            </Button>

            {/* HELP */}

            <p
              style={{
                textAlign: "center",
                marginTop: "18px",
                fontSize: "15px",
                color: "#4B4B4B",
                fontWeight: "500",
              }}
            >
              Need help? Contact support or visit
              the Help Centre.
            </p>

            {/* FOOTER */}

            <div
              style={{
                marginTop: "50px",
                textAlign: "center",
                fontSize: "11px",
                color: "#6B6B6B",
              }}
            >
              <div
                style={{
                  display: "flex",
                  justifyContent: "center",
                  gap: "24px",
                  flexWrap: "wrap",
                  marginBottom: "14px",
                }}
              >
                <span>© 2026 Tummly</span>
                <span>Help Centre</span>
                <span>Terms</span>
                <span>Privacy</span>
                <span>Cookie settings</span>
              </div>

              <div>
                🔒 Secure restaurant access
              </div>
            </div>
          </div>
        )}
{/* STEP 2 */}

{step === 2 && (
  <div
    style={{
      width: "100%",
      maxWidth: "480px",
      margin: "0 auto",
    }}
  >
    {/* BACK BUTTON */}

    <div
      onClick={() => setStep(1)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "20px",
        color: "#1F1F1F",
        fontSize: "18px",
        fontWeight: "500",
      }}
    >
      ← Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "32px",
        fontWeight: "700",
        lineHeight: "40px",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "18px",
      }}
    >
      Confirm your group details
    </h1>

    {/* DESCRIPTION */}

    <p
      style={{
        textAlign: "center",
        color: "#4F4F4F",
        fontSize: "14px",
        lineHeight: "22px",
        maxWidth: "760px",
        margin: "0 auto 24px auto",
      }}
    >
      Check the business details for the group or brand you want
      to manage in Tummly. We'll use this to create your shared
      workspace and location structure.
    </p>

    {/* GROUP / BRAND NAME */}

    <div style={{ marginBottom: "22px" }}>
      <input
        type="text"
        name="groupName"
        value={formData.groupName}
        onChange={handleInput}
        placeholder="Group / brand name"
        style={{
          width: "100%",
          height: "46px",
          border: "1px solid #D8D8D8",
          borderRadius: "8px",
          padding: "0 12px",
          fontSize: "14px",
          outline: "none",
          background: "#fff",
          color: "#1E1E1E",
        }}
      />
      {errors.groupName && (
  <div style={errorStyle}>
    {errors.groupName}
  </div>
)}
    </div>

    {/* BUSINESS CATEGORY */}

    <div style={{ marginBottom: "22px" }}>
      <select
        name="businessCategory"
        value={formData.businessCategory}
        onChange={handleInput}
        style={{
          width: "100%",
          height: "68px",
          border: "1px solid #D8D8D8",
          borderRadius: "8px",
          padding: "0 18px",
          fontSize: "18px",
          outline: "none",
          background: "#fff",
          color: "#6B6B6B",
          cursor: "pointer",
        }}
      >
        <option value="">
          Business category
        </option>

        <option value="Restaurant">
          Restaurant
        </option>

        <option value="Cafe">
          Cafe
        </option>

        <option value="Fast Food">
          Fast Food
        </option>

        <option value="Bakery">
          Bakery
        </option>
      </select>
      {errors.businessCategory && (
  <div style={errorStyle}>
    {errors.businessCategory}
  </div>
)}
    </div>

    {/* NUMBER OF LOCATIONS */}

    <div style={{ marginBottom: "22px" }}>
      <select
        name="numLocations"
        value={formData.numLocations}
        onChange={handleInput}
        style={{
          width: "100%",
          height: "68px",
          border: "1px solid #D8D8D8",
          borderRadius: "8px",
          padding: "0 18px",
          fontSize: "18px",
          outline: "none",
          background: "#fff",
          color: "#6B6B6B",
          cursor: "pointer",
        }}
      >
        <option value="">
          Number of locations
        </option>

        <option value="1">
          1 Location
        </option>

        <option value="2">
          2 Locations
        </option>

        <option value="3">
          3 Locations
        </option>

        <option value="4+">
          4+ Locations
        </option>
      </select>
    </div>

    {/* PRIMARY CONTACT PHONE */}

    <div style={{ marginBottom: "22px" }}>
      <input
        type="text"
        name="primaryPhone"
        value={formData.primaryPhone || ""}
        onChange={handleInput}
        placeholder="Primary contact phone   Optional"
        style={{
          width: "100%",
          height: "68px",
          border: "1px solid #D8D8D8",
          borderRadius: "8px",
          padding: "0 18px",
          fontSize: "18px",
          outline: "none",
          background: "#fff",
          color: "#1E1E1E",
        }}
      />
    </div>

    {/* BUSINESS LINK */}

    <div style={{ marginBottom: "40px" }}>
      <input
        type="text"
        name="businessLink"
        value={formData.businessLink || ""}
        onChange={handleInput}
        placeholder="Business link   Optional"
        style={{
          width: "100%",
          height: "68px",
          border: "1px solid #D8D8D8",
          borderRadius: "8px",
          padding: "0 18px",
          fontSize: "18px",
          outline: "none",
          background: "#fff",
          color: "#1E1E1E",
        }}
      />
    </div>

    {/* STEPS */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "28px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "15px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#444",
            fontWeight: "600",
            fontSize: "15px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#DADADA",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#8A8A8A",
            fontWeight: "500",
            fontSize: "15px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "12px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "14px",
        }}
      >
        <span
          style={{
            color: "#8A8A8A",
            fontWeight: "500",
            fontSize: "15px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "12px",
          }}
        />
      </div>
    </div>

    {/* BUTTON */}

    <Button variant="muted" size="form-action" onClick={nextStep}>
      Confirm group
    </Button>

    {/* HELP TEXT */}

    <p
      style={{
        textAlign: "center",
        fontSize: "13px",
        color: "#353535",
        marginBottom: "50px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "14px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "26px",
          flexWrap: "wrap",
          marginBottom: "12px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
)}

{/* STEP 3 */}

{step === 3 && (
  <div
    style={{
      width: "100%",
      maxWidth: "460px",
      margin: "0 auto",
    }}
  >
    {/* BACK */}

    <div
      onClick={() => setStep(2)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "20px",
        color: "#1F1F1F",
        fontSize: "18px",
        fontWeight: "500",
      }}
    >
      ← Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "28px",
        fontWeight: "700",
        lineHeight: "34px",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "16px",
      }}
    >
      Add your locations
    </h1>

    {/* DESCRIPTION */}

    <p
      style={{
        textAlign: "center",
        color: "#4F4F4F",
        fontSize: "14px",
        lineHeight: "22px",
        marginBottom: "42px",
      }}
    >
      Add the locations you want to include in your first
      Tummly rollout.
      <br />
      You can add more locations later from your workspace.
    </p>


   {/* LOCATIONS LIST */}
{locations.map((loc, index) => (
  <div key={index} style={{ marginBottom: "20px" }}>

         {/* LOCATION HEADER */}
    <div
      style={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        marginBottom: "12px",
      }}
    >
      <span
        style={{
          fontSize: "18px",
          fontWeight: "700",
        }}
      >
       
      </span>

      {locations.length > 1 && (
        <Button
          type="button"
          variant="link-destructive"
          onClick={() => deleteLocation(index)}
        >
          Delete
        </Button>
      )}
    </div>

    {/* LOCATION NAME */}
    <input
      type="text"
      name="locationName"
      value={loc.locationName}
      onChange={(e) => handleLocationChange(index, e)}
      placeholder="Location name"
      style={{
        width: "100%",
        height: "44px",
        border: "1px solid #D9D9D9",
        borderRadius: "6px",
        padding: "0 12px",
        marginBottom: "10px",
      }}
    />
{errors[`locationName${index}`] && (
  <div style={errorStyle}>
    {errors[`locationName${index}`]}
  </div>
)}
    {/* ADDRESS */}
    <input
      type="text"
      name="address"
      value={loc.address}
      onChange={(e) => handleLocationChange(index, e)}
      placeholder="📍 Address"
      style={{
        width: "100%",
        height: "44px",
        border: "1px solid #D9D9D9",
        borderRadius: "6px",
        padding: "0 12px",
        marginBottom: "10px",
      }}
    />
{errors[`address${index}`] && (
  <div style={errorStyle}>
    {errors[`address${index}`]}
  </div>
)}
    {/* POSTCODE */}
    <input
      type="text"
      name="postcode"
      value={loc.postcode}
      onChange={(e) => handleLocationChange(index, e)}
      placeholder="Postcode"
      style={{
        width: "100%",
        height: "44px",
        border: "1px solid #D9D9D9",
        borderRadius: "6px",
        padding: "0 12px",
        marginBottom: "10px",
      }}
    />
{errors[`postcode${index}`] && (
  <div style={errorStyle}>
    {errors[`postcode${index}`]}
  </div>
)}

    {/* PHONE */}
    <input
      type="text"
      name="locationPhone"
      value={loc.locationPhone}
      onChange={(e) => handleLocationChange(index, e)}
      placeholder="Location phone (optional)"
      style={{
        width: "100%",
        height: "44px",
        border: "1px solid #D9D9D9",
        borderRadius: "6px",
        padding: "0 12px",
        marginBottom: "10px",
      }}
    />

    {/* LOCAL CONTACT */}
    <input
      type="text"
      name="localContact"
      value={loc.localContact}
      onChange={(e) => handleLocationChange(index, e)}
      placeholder="Local contact (optional)"
      style={{
        width: "100%",
        height: "44px",
        border: "1px solid #D9D9D9",
        borderRadius: "6px",
        padding: "0 12px",
      }}
    />
    <div
  onClick={() => toggleRollout(index)}
  style={{
    display: "flex",
    alignItems: "center",
    gap: "12px",
    marginTop: "12px",
  }}
>
  <span>Include in first rollout?</span>

  <div
    style={{
      width: "42px",
      height: "24px",
      borderRadius: "20px",
      background: loc.includeInRollout
        ? "#22C55E"
        : "#D8D8D8",
      position: "relative",
      cursor: "pointer",
    }}
  >
    <div
      style={{
        width: "18px",
        height: "18px",
        borderRadius: "50%",
        background: "#fff",
        position: "absolute",
        top: "3px",
        left: loc.includeInRollout
          ? "21px"
          : "3px",
      }}
    />
  </div>
</div>
  </div>
))}

    {/* DIVIDER */}

    <div
      style={{
        width: "100%",
        height: "1px",
        background: "#E6E6E6",
        marginBottom: "24px",
      }}
    />

    {/* ADD LOCATION */}

   <div
  onClick={addLocation}
  style={{
    display: "flex",
    alignItems: "center",
    gap: "10px",
    marginBottom: "18px",
    cursor: "pointer",
    fontWeight: "600",
    color: "#1F1F1F",
    fontSize: "16px",
  }}
>
  ⊕ Add location
</div>

    {/* UPLOAD */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        marginBottom: "38px",
        cursor: "pointer",
        fontWeight: "600",
        color: "#1F1F1F",
        fontSize: "16px",
      }}
    >
      ↥ Upload locations instead
    </div>

    {/* STEP BAR */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "28px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "14px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "14px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#1F1F1F",
            fontWeight: "700",
            fontSize: "14px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#D9D9D9",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "12px",
        }}
      >
        <span
          style={{
            color: "#9B9B9B",
            fontWeight: "500",
            fontSize: "14px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#E5E5E5",
            marginLeft: "10px",
          }}
        />
      </div>
    </div>

    {/* BUTTON */}

    <Button variant="muted" size="form-action-lg" onClick={nextStep}>
      Continue to rollout
    </Button>

    {/* HELP */}

    <p
      style={{
        textAlign: "center",
        fontSize: "13px",
        color: "#353535",
        marginBottom: "50px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "14px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "24px",
          flexWrap: "wrap",
          marginBottom: "12px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
)}


{/* STEP 4 */}

{step === 4 && (
  <div
    style={{
      width: "100%",
      maxWidth: "620px",
      margin: "0 auto",
      position: "relative",
    }}
  >
    {/* BACK BUTTON */}

    <div
      onClick={() => setStep(3)}
      style={{
        display: "flex",
        alignItems: "center",
        gap: "10px",
        cursor: "pointer",
        marginBottom: "22px",
        color: "#1F1F1F",
        fontSize: "15px",
        fontWeight: "500",
      }}
    >
      <div
        style={{
          width: "26px",
          height: "26px",
          borderRadius: "50%",
          background: "#F2F2F2",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "12px",
        }}
      >
        ←
      </div>

      Back
    </div>

    {/* TITLE */}

    <h1
      style={{
        fontSize: "46px",
        fontWeight: "700",
        textAlign: "center",
        color: "#1E1E1E",
        marginBottom: "16px",
        lineHeight: "54px",
      }}
    >
      Prepare your first rollout
    </h1>

    {/* SUBTITLE */}

    <p
      style={{
        textAlign: "center",
        color: "#5E5E5E",
        fontSize: "16px",
        lineHeight: "28px",
        maxWidth: "760px",
        margin: "0 auto 42px auto",
      }}
    >
      Choose the guest touchpoints and starter settings
      you want to use across your first locations. You
      can customise each location later.
    </p>

  {/* ========================= */}
{/* ROLLOUT APPROACH */}
{/* ========================= */}
<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "18px",
    marginBottom: "18px",
  }}
>
  <div
    onClick={() => toggleSection("rollout")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
      }}
    >
      Rollout approach
    </span>

    <span>
      {openSection.rollout ? "▲" : "▼"}
    </span>
  </div>

  {openSection.rollout && (
    <div style={{ marginTop: "16px" }}>
      <select
        name="rolloutApproach"
        value={formData.rolloutApproach}
        onChange={handleInput}
        style={{
          width: "100%",
          height: "56px",
          border: "1px solid #D8D8D8",
        }}
      >
        {errors.rolloutApproach && (
  <div style={errorStyle}>
    {errors.rolloutApproach}
  </div>
)}
        <option value="">
          How do you want to start?
        </option>

        <option value="qr">
          QR code feedback
        </option>

        <option value="tablet">
          Tablet feedback station
        </option>

        <option value="sms">
          SMS review flow
        </option>

        <option value="email">
          Email follow-up
        </option>
      </select>
    </div>
  )}
</div>


{/* ========================= */}
{/* GUEST PROMPTS */}
{/* ========================= */}

<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "20px",
    marginBottom: "20px",
  }}
>
  <div
    onClick={() => toggleSection("prompts")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
        color: "#1F1F1F",
      }}
    >
      Where will guests see your prompts?
    </span>

    <span
      style={{
        fontSize: "18px",
        color: "#555",
      }}
    >
      {openSection.prompts ? "▲" : "▼"}
    </span>
  </div>

  {openSection.prompts && (
    <div style={{ marginTop: "16px" }}>
      <select
        name="guestPrompt"
        value={formData.guestPrompt || ""}
        onChange={handleInput}
        style={{
          width: "100%",
          height: "56px",
          border: "1px solid #D8D8D8",
          borderRadius: "4px",
          padding: "0 16px",
          fontSize: "14px",
          outline: "none",
          background: "#fff",
        }}
      >
        <option value="">Choose option</option>
        <option value="table">On Tables</option>
        <option value="receipt">On Receipt</option>
        <option value="counter">At Counter</option>
      </select>
    </div>
  )}
</div>
<div
  style={{
    borderBottom: "1px solid #E5E5E5",
    paddingBottom: "26px",
    marginBottom: "22px",
  }}
>
  {/* HEADER */}

  <div
    onClick={() => toggleSection("feedback")}
    style={{
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: "18px",
      cursor: "pointer",
    }}
  >
    <span
      style={{
        fontSize: "24px",
        fontWeight: "600",
        color: "#1F1F1F",
      }}
    >
      Feedback form
    </span>

    <span
      style={{
        fontSize: "18px",
        color: "#555",
      }}
    >
      {openSection.feedback ? "▲" : "▼"}
    </span>
  </div>

  {/* CONTENT */}

  {openSection.feedback && (
    <>
      <p
        style={{
          color: "#666",
          fontSize: "14px",
          lineHeight: "24px",
          marginBottom: "14px",
        }}
      >
        Guests can rate their experience, choose issue
        tags, leave an optional comment and choose whether
        to join your guest list.
      </p>

      <div
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#EAFBEF",
          color: "#22C55E",
          padding: "8px 14px",
          borderRadius: "999px",
          fontSize: "13px",
          fontWeight: "600",
          marginBottom: "20px",
        }}
      >
        Use recommended starter form
      </div>

      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "10px",
          marginBottom: "18px",
        }}
      >
        <span
          style={{
            fontSize: "14px",
            color: "#555",
            fontWeight: "500",
          }}
        >
          Review starter form settings
        </span>

        <span
          style={{
            fontSize: "12px",
            color: "#555",
          }}
        >
          ▲
        </span>
      </div>

   {[
  { label: "Rating", key: "rating" },
  { label: "Issue tags", key: "issueTags" },
  { label: "Optional comment", key: "comment" },
  { label: "First name", key: "firstName" },
  { label: "Email or phone", key: "contact" },
  { label: "Consent checkboxes", key: "consent" },
].map((item, index) => (
  <div
    key={index}
    style={{
      display: "flex",
      alignItems: "center",
      gap: "12px",
      marginBottom: "14px",
      cursor: "pointer",
    }}
    onClick={() => toggleFeedbackItem(item.key)}
  >
    <div
      style={{
        width: "18px",
        height: "18px",
        border: "1px solid #22C55E",
        borderRadius: "2px",
        background: feedbackItems[item.key]
          ? "#22C55E"
          : "#fff",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "#fff",
        fontSize: "11px",
        fontWeight: "700",
      }}
    >
      {feedbackItems[item.key] ? "✓" : ""}
    </div>

    <span
      style={{
        fontSize: "14px",
        color: "#333",
      }}
    >
      {item.label}
    </span>
  </div>
))}
    </>
  )}
</div>

    {/* ========================= */}
    {/* THANK YOU MESSAGE */}
    {/* ========================= */}

    <div
      style={{
        borderBottom: "1px solid #E5E5E5",
        paddingBottom: "24px",
        marginBottom: "22px",
      }}
    >
      {/* HEADER */}

    <div
  onClick={() => toggleSection("thankyou")}
  style={{
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "14px",
    cursor: "pointer",
  }}
>
  <span
    style={{
      fontSize: "24px",
      fontWeight: "600",
      color: "#1F1F1F",
    }}
  >
    Thank-you message
  </span>

  <span
    style={{
      fontSize: "18px",
      color: "#555",
    }}
  >
    {openSection.thankyou ? "▲" : "▼"}
  </span>
</div>

{openSection.thankyou && (
  <>
    <p
      style={{
        color: "#666",
        fontSize: "14px",
        marginBottom: "14px",
      }}
    >
      You can customise this by location later.
    </p>

    <textarea
      name="thankYouMessage"
      value={
        formData.thankYouMessage ||
        "Thanks for your feedback. We appreciate you taking a moment to help us improve."
      }
      onChange={handleInput}
      placeholder="Thank you message"
      style={{
        width: "100%",
        minHeight: "120px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "16px",
        fontSize: "14px",
        lineHeight: "24px",
        resize: "none",
        outline: "none",
        background: "#fff",
        color: "#333",
        boxSizing: "border-box",
      }}
    />
  </>
)}
    </div>

    {/* ========================= */}
    {/* STARTER OFFER */}
    {/* ========================= */}

    <div
      style={{
        borderBottom: "1px solid #E5E5E5",
        paddingBottom: "28px",
        marginBottom: "30px",
      }}
    >
      {/* HEADER */}

  <div
  onClick={() => toggleSection("offer")}
  style={{
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "14px",
    cursor: "pointer",
  }}
>
  <span
    style={{
      fontSize: "24px",
      fontWeight: "600",
      color: "#1F1F1F",
    }}
  >
    Starter offer — optional
  </span>

  <span
    style={{
      fontSize: "18px",
      color: "#555",
    }}
  >
    {openSection.offer ? "▲" : "▼"}
  </span>
</div>

{openSection.offer && (
  <>
    <p
      style={{
        color: "#666",
        fontSize: "14px",
        lineHeight: "22px",
        marginBottom: "18px",
      }}
    >
      Add a simple offer for guests who opt in, or
      skip this step and create one later from your
      workspace.
    </p>

    <select
      name="offerType"
      value={formData.offerType || ""}
      onChange={handleInput}
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#7A7A7A",
        marginBottom: "14px",
      }}
    >
      <option value="">Offer type</option>
      <option value="discount">Discount code</option>
      <option value="freeItem">Free item</option>
      <option value="voucher">Voucher</option>
    </select>

    <input
      type="text"
      name="offerTitle"
      value={formData.offerTitle || ""}
      onChange={handleInput}
      placeholder="Thanks you offer"
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#333",
        marginBottom: "14px",
        boxSizing: "border-box",
      }}
    />

    <input
      type="text"
      name="offerMessage"
      value={formData.offerMessage || ""}
      onChange={handleInput}
      placeholder="Example: Free side with your next order"
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#333",
        marginBottom: "14px",
        boxSizing: "border-box",
      }}
    />

    <select
      name="expiry"
      value={formData.expiry || ""}
      onChange={handleInput}
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#7A7A7A",
        marginBottom: "14px",
      }}
    >
      <option value="">Expiry</option>
      <option value="7days">7 Days</option>
      <option value="14days">14 Days</option>
      <option value="30days">30 Days</option>
    </select>

    <select
      name="redemptionMethod"
      value={formData.redemptionMethod || ""}
      onChange={handleInput}
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#7A7A7A",
        marginBottom: "14px",
      }}
    >
      <option value="">Redemption method</option>
      <option value="showStaff">Show to staff</option>
      <option value="coupon">Coupon code</option>
      <option value="scanQr">Scan QR code</option>
    </select>

    <select
      name="usageLimit"
      value={formData.usageLimit || ""}
      onChange={handleInput}
      style={{
        width: "100%",
        height: "56px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "0 16px",
        fontSize: "14px",
        outline: "none",
        background: "#fff",
        color: "#7A7A7A",
        marginBottom: "14px",
      }}
    >
      <option value="">Usage limit</option>
      <option value="one">One time use</option>
      <option value="multi">Multiple use</option>
    </select>

    <textarea
      name="guestPreview"
      value={
        formData.guestPreview ||
        "Thanks for your feedback.\nWe appreciate you taking a moment to help us improve."
      }
      onChange={handleInput}
      placeholder="Guest thank-you preview"
      style={{
        width: "100%",
        minHeight: "100px",
        border: "1px solid #D8D8D8",
        borderRadius: "4px",
        padding: "14px",
        fontSize: "14px",
        lineHeight: "22px",
        resize: "none",
        outline: "none",
        background: "#F8F8F8",
        color: "#555",
        boxSizing: "border-box",
      }}
    />
  </>
)}
    </div>

    {/* ========================= */}
    {/* STEP BAR */}
    {/* ========================= */}

    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: "34px",
      }}
    >
      {/* STEP 1 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          1 Account
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 2 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          2 Group
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 3 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#22C55E",
            fontWeight: "600",
            fontSize: "13px",
          }}
        >
          3 Locations
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#22C55E",
            marginLeft: "10px",
          }}
        />
      </div>

      {/* STEP 4 */}

      <div
        style={{
          display: "flex",
          alignItems: "center",
          flex: 1,
          marginLeft: "10px",
        }}
      >
        <span
          style={{
            color: "#1F1F1F",
            fontWeight: "700",
            fontSize: "13px",
          }}
        >
          4 Rollout
        </span>

        <div
          style={{
            flex: 1,
            height: "2px",
            background: "#1F1F1F",
            marginLeft: "10px",
          }}
        />
      </div>
    </div>

    {/* BUTTONS */}

    <div
      style={{
        display: "flex",
        gap: "16px",
        marginBottom: "34px",
      }}
    >
      {/* SKIP */}

      <Button
        variant="secondary"
        size="toolbar"
        onClick={() => navigate("/multi-dashboard")}
      >
        Skip for now
      </Button>

      {/* COMPLETE */}

<Button variant="muted" size="toolbar-flex" onClick={handleSubmit}>
  Complete setup and open workspace
</Button>
    </div>

    {/* HELP */}

    <p
      style={{
        textAlign: "center",
        fontSize: "14px",
        color: "#444",
        marginBottom: "100px",
        fontWeight: "500",
      }}
    >
      Need help? Contact support or visit the Help
      Centre.
    </p>

    {/* FOOTER */}

    <div
      style={{
        textAlign: "center",
        color: "#777",
        fontSize: "12px",
      }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          gap: "20px",
          flexWrap: "wrap",
          marginBottom: "10px",
        }}
      >
        <span>© 2026 Tummly</span>
        <span>Help Centre</span>
        <span>Terms</span>
        <span>Privacy</span>
        <span>Cookie settings</span>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          gap: "8px",
        }}
      >
        🔒
        <span>Secure restaurant access</span>
      </div>
    </div>
  </div>
  
)}
  </div>
  </div>  
  ); 
  }; 
    
    export default MultiRegisterPage;