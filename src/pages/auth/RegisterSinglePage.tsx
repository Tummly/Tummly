import { useEffect, useState } from "react";
import type { ChangeEvent, ReactNode } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import axios, { isAxiosError } from "axios";
import { API_BASE_URL, AUTH_API_BASE_URL } from "../../config/api";

interface SingleRegisterFormData {
  token: string;
  email: string;
  fullName: string;
  password: string;
  confirmPassword: string;
  agree: boolean;
  restaurantName: string;
  locationName: string;
  address: string;
  postcode: string;
  phone: string;
  businessLink: string;
  businessCategory: string;
  touchpoints: string[];
  feedbackTags: string[];
  thankYouMessage: string;
  offerHeadline: string;
  offerDetails: string;
  offerExpiry: string;
  offerRedemption: string;
  offerUsageLimit: string;
}

type FormErrors = Record<string, string>;
type ArrayCheckboxField = "touchpoints" | "feedbackTags";

interface PasswordStrengthProps {
  password: string;
}

interface ProgressBarProps {
  activeStep: number;
}

interface InputFieldProps {
  label: string;
  type?: string;
  name: string;
  value: string;
  onChange: (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => void;
  placeholder?: string;
  error?: string;
  disabled?: boolean;
}

interface AccordionProps {
  title: string;
  children: ReactNode;
  open: boolean;
  setOpen: () => void;
}

const PasswordStrength = ({ password }: PasswordStrengthProps) => {
  let strength = 0;

  if (password.length >= 8) strength++;

  if (/[A-Z]/.test(password))
    strength++;

  if (/[0-9!@#$%^&*]/.test(password))
    strength++;

  if (password.length >= 12)
    strength++;

  const getColor = (index: number) => {
    if (index >= strength) {
      return "bg-[#E5E7EB]";
    }

    if (strength === 1) {
      return "bg-red-500";
    }

    if (strength === 2) {
      return "bg-yellow-500";
    }

    return "bg-[#22C55E]";
  };



  return (
    <div className="flex gap-3 mt-5 mb-2">
      {[1, 2, 3, 4].map(
        (item, index) => (
          <div
            key={item}
            className={`h-[5px] flex-1 rounded-full transition-all duration-300 ${getColor(
              index
            )}`}
          />
        )
      )}
    </div>
  );
};

const ProgressBar = ({ activeStep }: ProgressBarProps) => {
  return (
    <div className="pt-7">
      <div className="flex items-center justify-between text-[12px] mb-3">
        <span
          className={`font-medium ${activeStep >= 1
              ? "text-black"
              : "text-[#9CA3AF]"
            }`}
        >
          1 Account
        </span>

        <span
          className={`font-medium ${activeStep >= 2
              ? "text-black"
              : "text-[#9CA3AF]"
            }`}
        >
          2 Restaurant
        </span>

        <span
          className={`font-medium ${activeStep >= 3
              ? "text-black"
              : "text-[#9CA3AF]"
            }`}
        >
          3 Guest Loop
        </span>
      </div>

      <div className="flex items-center gap-2">
        <div
          className={`flex-1 h-[3px] rounded-full ${activeStep >= 1
              ? "bg-black"
              : "bg-[#E5E7EB]"
            }`}
        />

        <div
          className={`flex-1 h-[3px] rounded-full ${activeStep >= 2
              ? "bg-black"
              : "bg-[#E5E7EB]"
            }`}
        />

        <div
          className={`flex-1 h-[3px] rounded-full ${activeStep >= 3
              ? "bg-black"
              : "bg-[#E5E7EB]"
            }`}
        />
      </div>
    </div>
  );
};

const InputField = ({
  label,
  type = "text",
  name,
  value,
  onChange,
  placeholder,
  error,
  disabled = false,
}: InputFieldProps) => {
  return (
    <div className="w-full">
      <label
        className="
          block
          mb-3
          text-[14px]
          font-semibold
          text-[#374151]
        "
      >
        {label}
      </label>

      <input
        type={type}
        name={name}
        value={value}
        disabled={disabled}
        onChange={onChange}
        placeholder={placeholder}
        className={`
          w-full
          h-[60px]
          sm:h-[64px]
          px-5
          sm:px-6
          rounded-[14px]
          border
          bg-white
          text-[15px]
          sm:text-[16px]
          text-[#111827]
          placeholder:text-[#9CA3AF]
          shadow-[0_1px_3px_rgba(0,0,0,0.04)]
          outline-none
          transition-all
          duration-200
         ${
  error
    ? "border-red-300 focus:border-red-400"
    : "border-[#EEF2F6] focus:border-[#D8DEE6]"
}
        `}
      />

      {error && (
        <p className="text-red-500 text-[12px] mt-3">
          {error}
        </p>
      )}
    </div>
  );
};

const Accordion = ({
  title,
  children,
  open,
  setOpen,
}: AccordionProps) => {
  return (
    <div className="border-b border-[#E5E7EB] pb-5">
      <button
        onClick={setOpen}
        className="w-full flex items-center justify-between text-left"
      >
        <h3 className="text-[20px] leading-none font-semibold text-[#111827]">
          {title}
        </h3>

        <svg
          className={`w-5 h-5 text-[#6B7280] transition-all duration-300 ${open ? "rotate-180" : ""
            }`}
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
        >
          <path d="M6 9l6 6 6-6" />
        </svg>
      </button>

      {open && (
        <div className="pt-6">
          {children}
        </div>
      )}
    </div>
  );
};

function RegisterSinglePage() {

  /*
   =========================================
   URL TOKEN
   =========================================
  */

  const [searchParams] =
    useSearchParams();

  const token =
    searchParams.get("token");

  /*
   =========================================
   STATES
   =========================================
  */

const navigate = useNavigate();

  const [step, setStep] =
    useState(1);

  const [loading, setLoading] =
    useState(false);

  const [tokenLoading, setTokenLoading] =
    useState(() => Boolean(token));

  const [errors, setErrors] =
    useState<FormErrors>({});

  const [tokenError, setTokenError] =
    useState(() =>
      token ? "" : "Setup token is missing."
    );

  const [formData, setFormData] =
    useState<SingleRegisterFormData>({
      token: token ?? "",

      email: "",
      fullName: "",

      password: "",
      confirmPassword: "",

      agree: false,

      restaurantName: "",
      locationName: "",
      address: "",
      postcode: "",

      phone: "",
      businessLink: "",
      businessCategory: "",

      touchpoints: [],
      feedbackTags: [],

      thankYouMessage: "",

      offerHeadline: "",
      offerDetails: "",
      offerExpiry: "",
      offerRedemption: "",
      offerUsageLimit: "",
    });

  /*
   =========================================
   VALIDATE TOKEN ON PAGE LOAD
   =========================================
  */

  useEffect(() => {
    if (!token) {
      return;
    }

    let active = true;

    void (async () => {
      try {
        const response = await axios.get(
          `${API_BASE_URL}/Trial/validate-setup-token?token=${token}`
        );

        if (!active) {
          return;
        }

        const data = response.data.data;

        setFormData((prev) => ({
          ...prev,
          token,
          email: data.email || "",
          fullName: data.fullName || "",
          restaurantName: data.businessName || "",
        }));

        setTokenError("");
      } catch (error: unknown) {
        if (!active) {
          return;
        }

        console.log(error);

        if (isAxiosError<{ message?: string }>(error)) {
          setTokenError(
            error.response?.data?.message || "Invalid setup token"
          );
        } else {
          setTokenError("Invalid setup token");
        }
      } finally {
        if (active) {
          setTokenLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [token]);

  /*
   =========================================
   VALIDATION
   =========================================
  */

  const validateField = (
    name: string,
    value: string
  ) => {

    let error = "";

    /*
     =========================================
     EMAIL
     =========================================
    */

    if (name === "email") {

      const emailRegex =
        /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

      if (
        value &&
        !emailRegex.test(value)
      ) {
        error =
          "Invalid email address";
      }
    }

    /*
     =========================================
     PASSWORD
     =========================================
    */

    if (name === "password") {

      if (
        value &&
        value.length < 8
      ) {
        error =
          "Password must be at least 8 characters";
      }
    }

    /*
     =========================================
     CONFIRM PASSWORD
     =========================================
    */

    if (
      name === "confirmPassword"
    ) {

      if (
        value &&
        value !== formData.password
      ) {
        error =
          "Passwords do not match";
      }
    }

    /*
     =========================================
     PHONE
     =========================================
    */

    if (name === "phone") {

      const phoneRegex =
        /^[0-9]{11}$/;

      if (
        value &&
        !phoneRegex.test(value)
      ) {
        error =
          "Phone number must be 11 digits";
      }
    }

    setErrors((prev) => ({
      ...prev,
      [name]: error,
    }));
  };

  /*
   =========================================
   HANDLE INPUT CHANGE
   =========================================
  */

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {

    const { name, value, type } = e.target;
    const checked = "checked" in e.target ? e.target.checked : false;

    const updatedValue =
      type === "checkbox"
        ? checked
        : value;

    setFormData((prev) => ({
      ...prev,
      [name]: updatedValue,
    }));

    if (type !== "checkbox") {
      validateField(name, value);
    }

    /*
     =========================================
     REVALIDATE CONFIRM PASSWORD
     =========================================
    */

    if (
      name === "password" &&
      formData.confirmPassword
    ) {

      validateField(
        "confirmPassword",
        formData.confirmPassword
      );
    }
  };

  /*
   =========================================
   HANDLE CHECKBOX ARRAYS
   =========================================
  */

  const handleArrayCheckbox = (
    field: ArrayCheckboxField,
    value: string
  ) => {

    setFormData((prev) => {

      const exists =
        prev[field].includes(value);

      return {
        ...prev,

        [field]: exists
          ? prev[field].filter(
            (item) =>
              item !== value
          )
          : [
            ...prev[field],
            value,
          ],
      };
    });
  };

  /*
   =========================================
   STEP VALIDATIONS
   =========================================
  */

  const validateStepOne = () => {

    const newErrors: FormErrors = {};

    if (!formData.email) {
      newErrors.email =
        "Email is required";
    }

    if (!formData.fullName) {
      newErrors.fullName =
        "Full name is required";
    }

    if (!formData.password) {
      newErrors.password =
        "Password is required";
    }

    if (
      formData.password.length < 8
    ) {
      newErrors.password =
        "Password must be at least 8 characters";
    }

    if (
      formData.password !==
      formData.confirmPassword
    ) {
      newErrors.confirmPassword =
        "Passwords do not match";
    }

    if (!formData.agree) {
      newErrors.agree =
        "You must accept terms";
    }

    setErrors(newErrors);

    return (
      Object.keys(newErrors)
        .length === 0
    );
  };

  const validateStepTwo = () => {

    const newErrors: FormErrors = {};

    if (!formData.restaurantName) {
      newErrors.restaurantName =
        "Restaurant name is required";
    }

    if (!formData.locationName) {
      newErrors.locationName =
        "Location name is required";
    }

    if (!formData.address) {
      newErrors.address =
        "Address is required";
    }

    if (!formData.phone) {
      newErrors.phone =
        "Phone is required";
    }

    if (
      formData.phone &&
      formData.phone.length !== 11
    ) {
      newErrors.phone =
        "Phone must be 11 digits";
    }

    if (
      !formData.businessCategory
    ) {
      newErrors.businessCategory =
        "Business category is required";
    }

    setErrors((prev) => ({
      ...prev,
      ...newErrors,
    }));

    return (
      Object.keys(newErrors)
        .length === 0
    );
  };

  /*
   =========================================
   COMPLETE SETUP
   =========================================
  */

  const completeSetup =
    async () => {

      try {

        /*
         =========================================
         STEP 3 VALIDATION
         =========================================
        */

        if (
          !formData.thankYouMessage
        ) {

          alert(
            "Thank you message is required"
          );

          return;
        }

        setLoading(true);

        /*
         =========================================
         FINAL PAYLOAD
         =========================================
        */

const payload = {
  token: formData.token,
  password: formData.password,
  confirmPassword: formData.confirmPassword,

  groupName: formData.restaurantName,
  businessCategory: formData.businessCategory,
  primaryPhone: formData.phone,
  businessLink: formData.businessLink,

  locations: [
    {
      locationName: formData.locationName,
      address: formData.address,
      postcode: formData.postcode,
      locationPhone: formData.phone,
      localContact: formData.fullName,
      includeInRollout: true,
    },
  ],

  rolloutApproach: "Single",
  guestPrompt: "Please leave feedback",

  thankYouMessage: formData.thankYouMessage,

  offerType: "Single",
  offerTitle: formData.offerHeadline,
  offerMessage: formData.offerDetails,
  offerExpiry: formData.offerExpiry,
  redemptionMethod: formData.offerRedemption,
  usageLimit: formData.offerUsageLimit,
};

        /*
         =========================================
         API REQUEST
         =========================================
        */

       const response = await axios.post(
  `${AUTH_API_BASE_URL}/setup-account`,
  payload
);

if (response.data.success) {
  alert(response.data.message);

  // go to SINGLE dashboard
navigate("/single-dashboard");
}
        /*
         =========================================
         SUCCESS
         =========================================
        */

        alert(
          response.data.message
        );

        /*
         =========================================
         OPTIONAL REDIRECT
         =========================================
        */

        // window.location.href = "/login";

      } catch (error: unknown) {

  console.log("FULL ERROR:", error);

  if (isAxiosError<{ errors?: unknown; message?: string }>(error)) {
    console.log(
      "VALIDATION ERRORS:",
      error.response?.data?.errors
    );

    alert(
      JSON.stringify(
        error.response?.data?.errors,
        null,
        2
      )
    );
  }

} finally {

        setLoading(false);
      }
    };

  /*
   =========================================
   OPTIONS
   =========================================
  */

  const touchpointOptions = [
    "Counter card",
    "Table card",
    "Receipt prompt",
    "Packaging sticker",
    "Delivery insert",
    "Window sticker",
    "Digital Smart Guest Link",
  ];

  const feedbackOptions = [
    "Food quality",
    "Service quality",
    "Staff behaviour",
    "Cleanliness",
  ];



  /*
   =========================================
   LOADING SCREEN
   =========================================
  */

  if (tokenLoading) {

    return (
      <div className="min-h-screen flex items-center justify-center text-[20px] font-semibold">
        Validating setup token...
      </div>
    );
  }

  /*
   =========================================
   INVALID TOKEN SCREEN
   =========================================
  */

  if (tokenError) {

    return (
      <div className="min-h-screen flex items-center justify-center px-6">
        <div className="max-w-[500px] text-center">

          <h1 className="text-[34px] font-bold mb-4 text-red-500">
            Invalid Setup Link
          </h1>

          <p className="text-[#6B7280]">
            {tokenError}
          </p>

        </div>
      </div>
    );
  }

  return (
   
   <div className="min-h-screen bg-[#FAFAFA] flex flex-col">

      {/* STEP 1 */}

      {step === 1 && (
     <main
  className="
  flex-1
  flex
  items-center
  justify-center
  px-4
  sm:px-6
  md:px-8
  py-10
  sm:py-14
  md:py-20
"
>
  <div
    className="
    w-full
    max-w-[820px]
    bg-white
    rounded-[24px]
    border
    border-[#E5E7EB]
    shadow-lg
    p-6
    sm:p-8
    md:p-12
"
  >
<div className="text-center mb-10 md:mb-14">
  <h1
    className="
      text-[34px]
      sm:text-[42px]
      md:text-[56px]
      leading-[1.1]
      font-bold
      tracking-[-2px]
      text-[#111827]
      mb-3
    "
  >
    Create your account
  </h1>

  <p className="text-[#6B7280] text-[15px] sm:text-[16px]">
    Complete your account setup to continue
  </p>
</div>

 <div className="flex flex-col gap-8 md:gap-10">

              <InputField
                label="Email address"
                type="email"
                name="email"
                value={
                  formData.email
                }
                onChange={
                  handleChange
                }
                placeholder="hello@restaurant.com"
                error={
                  errors.email
                }
                disabled={true}
              />

              <InputField
                label="Full name"
                name="fullName"
                value={
                  formData.fullName
                }
                onChange={
                  handleChange
                }
                placeholder="John Smith"
                error={
                  errors.fullName
                }
              />

              <InputField
                label="Password"
                type="password"
                name="password"
                value={
                  formData.password
                }
                onChange={
                  handleChange
                }
                placeholder="Create password"
                error={
                  errors.password
                }
              />

              <PasswordStrength
                password={
                  formData.password
                }
              />

              <InputField
                label="Confirm Password"
                type="password"
                name="confirmPassword"
                value={
                  formData.confirmPassword
                }
                onChange={
                  handleChange
                }
                placeholder="Confirm password"
                error={
                  errors.confirmPassword
                }
              />

              <label
  className="
    flex
    items-center
    gap-3
    py-2
    text-[15px]
    text-[#374151]
  "
>
                <input
                  type="checkbox"
                  name="agree"
                  checked={
                    formData.agree
                  }
                  onChange={
                    handleChange
                  }
                />

                <span>
                  I agree to Terms
                </span>
              </label>

              {errors.agree && (
                <p className="text-red-500 text-[12px]">
                  {errors.agree}
                </p>
              )}

              <ProgressBar
                activeStep={1}
              />

              <button
                onClick={() => {

                  const valid =
                    validateStepOne();

                  if (valid) {
                    setStep(2);
                  }
                }}
                className="
w-full
h-[62px]
sm:h-[64px]
rounded-[16px]
bg-black
text-white
font-semibold
text-[16px]
transition-all
duration-300
hover:opacity-90
"
              >
                Continue
              </button>

            </div>

          </div>

        </main>
      )}

      {/* STEP 2 */}

      {step === 2 && (
        <main className="flex-1 flex items-center justify-center px-6 py-16 md:py-20">

          <div className="w-full max-w-[700px] space-y-5">

            <input
              type="text"
              name="restaurantName"
              placeholder="Restaurant Name"
              value={
                formData.restaurantName
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-7 rounded-[12px] border"
            />

            <input
              type="text"
              name="locationName"
              placeholder="Location Name"
              value={
                formData.locationName
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-7 rounded-[12px] border"
            />

            <input
              type="text"
              name="address"
              placeholder="Address"
              value={
                formData.address
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-7 rounded-[12px] border"
            />

            <input
              type="text"
              name="postcode"
              placeholder="Postcode"
              value={
                formData.postcode
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="phone"
              placeholder="Public Phone Number"
              value={
                formData.phone
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="businessLink"
              placeholder="Business Link"
              value={
                formData.businessLink
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <select
              name="businessCategory"
              value={
                formData.businessCategory
              }
              onChange={
                handleChange
              }
              className="w-full h-[58px] px-5 rounded-[12px] border"
            >
              <option value="">
                Select Category
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

            </select>

            <ProgressBar
              activeStep={2}
            />

            <div className="flex gap-4">

              <button
                onClick={() =>
                  setStep(1)
                }
                className="flex-1 h-[58px] rounded-full border"
              >
                Back
              </button>

              <button
                onClick={() => {

                  const valid =
                    validateStepTwo();

                  if (valid) {
                    setStep(3);
                  }
                }}
                className="flex-1 h-[58px] rounded-full bg-black text-white"
              >
                Continue
              </button>

            </div>

          </div>

        </main>
      )}

      {/* STEP 3 */}

      {step === 3 && (
        <main className="flex-1 flex items-center justify-center px-6 py-16 md:py-20">

          <div className="w-full max-w-[700px] space-y-8">

            <Accordion
              title="Touchpoints"
              open={true}
              setOpen={() => { }}
            >

              <div className="space-y-4">

                {touchpointOptions.map(
                  (item) => (
                    <label
                      key={item}
                      className="flex items-center gap-3"
                    >
                      <input
                        type="checkbox"
                        checked={formData.touchpoints.includes(
                          item
                        )}
                        onChange={() =>
                          handleArrayCheckbox(
                            "touchpoints",
                            item
                          )
                        }
                      />

                      <span>
                        {item}
                      </span>
                    </label>
                  )
                )}

              </div>

            </Accordion>

            <Accordion
              title="Feedback Tags"
              open={true}
              setOpen={() => { }}
            >

              <div className="space-y-4">

                {feedbackOptions.map(
                  (item) => (
                    <label
                      key={item}
                      className="flex items-center gap-3"
                    >
                      <input
                        type="checkbox"
                        checked={formData.feedbackTags.includes(
                          item
                        )}
                        onChange={() =>
                          handleArrayCheckbox(
                            "feedbackTags",
                            item
                          )
                        }
                      />

                      <span>
                        {item}
                      </span>
                    </label>
                  )
                )}

              </div>

            </Accordion>

            <textarea
              name="thankYouMessage"
              value={
                formData.thankYouMessage
              }
              onChange={
                handleChange
              }
              placeholder="Thank you message"
              className="w-full h-[140px] p-5 rounded-[18px] border"
            />

            <input
              type="text"
              name="offerHeadline"
              value={
                formData.offerHeadline
              }
              onChange={
                handleChange
              }
              placeholder="Offer headline"
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="offerDetails"
              value={
                formData.offerDetails
              }
              onChange={
                handleChange
              }
              placeholder="Offer details"
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="offerExpiry"
              value={
                formData.offerExpiry
              }
              onChange={
                handleChange
              }
              placeholder="Offer expiry"
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="offerRedemption"
              value={
                formData.offerRedemption
              }
              onChange={
                handleChange
              }
              placeholder="Offer redemption"
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <input
              type="text"
              name="offerUsageLimit"
              value={
                formData.offerUsageLimit
              }
              onChange={
                handleChange
              }
              placeholder="Offer usage limit"
              className="w-full h-[58px] px-5 rounded-[12px] border"
            />

            <ProgressBar
              activeStep={3}
            />

            <div className="flex gap-4">

              <button
                onClick={() =>
                  setStep(2)
                }
                className="flex-1 h-[58px] rounded-full border"
              >
                Back
              </button>

              <button
                onClick={
                  completeSetup
                }
                disabled={loading}
                className="flex-1 h-[58px] rounded-full bg-black text-white"
              >
                {loading
                  ? "Creating..."
                  : "Complete Setup"}
              </button>

            </div>

          </div>

        </main>
      )}

    </div>
  );
}

export default RegisterSinglePage;