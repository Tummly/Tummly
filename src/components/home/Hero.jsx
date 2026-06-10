// src/components/home/Hero.jsx
import { useState } from "react";
import "../../assets/css/homepage.css";
import {
  submitTrialRequest,
  verifyOtpRequest
} from "../../api/trialApi";

import heroImage from "../../assets/images/hero.png";


function Hero() {

  /*
   =========================================
   STATES
   =========================================
  */

  const [currentStep, setCurrentStep] = useState(1);

  const [loading, setLoading] = useState(false);

  const [otpLoading, setOtpLoading] = useState(false);

  const [error, setError] = useState("");

  const [otp, setOtp] = useState([
    "",
    "",
    "",
    "",
    "",
    ""
  ]);

  /*
   =========================================
   FORM DATA
   =========================================
  */

  const [formData, setFormData] = useState({
    businessName: "",
    businessCategory: "",
    locations: "",
    businessLink: "",
    fullName: "",
    email: "",
    mobile: "",
    role: "",
    goal: "",
    termsAccepted: false,
  });

  /*
   =========================================
   HANDLE CHANGE
   =========================================
  */

  const handleChange = (e) => {

    const {
      name,
      value,
      type,
      checked
    } = e.target;

    setFormData((prev) => ({
      ...prev,

      [name]:
        type === "checkbox"
          ? checked
          : value,
    }));
  };

  /*
   =========================================
   OTP INPUT CHANGE
   =========================================
  */

  const handleOtpChange = (value, index) => {

    if (!/^\d?$/.test(value)) {
      return;
    }

    const updatedOtp = [...otp];

    updatedOtp[index] = value;

    setOtp(updatedOtp);

    /*
     AUTO FOCUS NEXT
    */

    if (value && index < 5) {

      const nextInput =
        document.getElementById(
          `otp-${index + 1}`
        );

      if (nextInput) {
        nextInput.focus();
      }
    }
  };

  /*
   =========================================
   SUBMIT TRIAL
   =========================================
  */

  const handleTrialSubmit = async (e) => {

    e.preventDefault();

    setError("");

    /*
     TERMS VALIDATION
    */

    if (!formData.termsAccepted) {

      setError(
        "Please accept terms and conditions."
      );

      return;
    }

    try {

      setLoading(true);

      const response =
        await submitTrialRequest(formData);

      console.log(response);

      /*
       SUCCESS -> OTP STEP
      */

      setCurrentStep(2);

    } catch (err) {

      console.log(err);

      /*
       =========================================
       VALIDATION ERRORS
       =========================================
      */

      if (
        err.response?.data?.message?.includes(
          "mobile number"
        )
      ) {
        setError(
          "This mobile number is already associated with another business account."
        );

        return;
      }

      if (
        err.response?.data?.message?.includes(
          "business name"
        )
      ) {
        setError(
          "This restaurant/business name is already registered."
        );

        return;
      }

      if (
        err.response?.data?.message?.includes(
          "website/link"
        )
      ) {
        setError(
          "This business website is already linked to another account."
        );

        return;
      }

      if (err.response?.data?.errors) {

        const errors = err.response.data.errors;

        /*
         =========================================
         MOBILE ERROR
         =========================================
        */

        if (errors.Mobile) {

          setError(
            "Please enter a valid mobile number. Use only numbers and include your country code if needed."
          );

          return;
        }

        /*
         =========================================
         EMAIL ERROR
         =========================================
        */

        if (errors.Email) {

          setError(
            "Please enter a valid email address."
          );

          return;
        }

        /*
         =========================================
         BUSINESS NAME ERROR
         =========================================
        */

        if (errors.BusinessName) {

          setError(
            "Please enter your restaurant or business name."
          );

          return;
        }

        /*
         =========================================
         FULL NAME ERROR
         =========================================
        */

        if (errors.FullName) {

          setError(
            "Please enter your full name."
          );

          return;
        }

        /*
         =========================================
         TERMS ERROR
         =========================================
        */

        if (errors.TermsAccepted) {

          setError(
            "Please accept the terms and privacy notice to continue."
          );

          return;
        }

        /*
         =========================================
         DEFAULT VALIDATION ERROR
         =========================================
        */

        setError(
          "Some information is missing or invalid. Please check your form and try again."
        );

        return;
      }

      /*
       =========================================
       DUPLICATE EMAIL ERROR
       =========================================
      */

/*
       =========================================
       DUPLICATE EMAIL ERROR
       =========================================
      */

      if (
        err.response?.data?.message?.includes(
          "already exists"
        )
      ) {
        setError(
          "A trial request with this email already exists. Please use another email address."
        );
        return;
      }

      /*
       =========================================
       SERVER ERROR
       =========================================
      */

      // Dynamic error handling: Backend ka message dikhayega, 
      // agar backend ka message nahi mila toh default message dikhayega
      setError(
        err.response?.data?.message || "Something went wrong while submitting your request. Please try again."
      );

    } finally {
      setLoading(false);
    }
  };
  /*
   =========================================
   VERIFY OTP
   =========================================
  */

  const handleOtpVerify = async (e) => {

    e.preventDefault();

    setError("");

    const finalOtp = otp.join("");

    /*
     OTP VALIDATION
    */

    if (finalOtp.length !== 6) {

      setError(
        "Please enter complete 6-digit OTP."
      );

      return;
    }

    try {

      setOtpLoading(true);

      const response =
        await verifyOtpRequest({

          email: formData.email,

          otpCode: finalOtp,
        });

      console.log(response);

      /*
       SUCCESS SCREEN
      */

      setCurrentStep(3);

    } catch (err) {

      console.log(err);

      setError(
        err?.response?.data?.message ||
        "Invalid or expired OTP."
      );

    } finally {

      setOtpLoading(false);
    }
  };

  /*
   =========================================
   RESET FORM
   =========================================
  */

  const resetForm = () => {

    setCurrentStep(1);

    setError("");

    setOtp([
      "",
      "",
      "",
      "",
      "",
      ""
    ]);

    setFormData({
      businessName: "",
      businessCategory: "",
      locations: "",
      businessLink: "",
      fullName: "",
      email: "",
      mobile: "",
      role: "",
      goal: "",
      termsAccepted: false,
    });
  };

  return (

 <section
  id="request-trial"
  className="hero"
  style={{
    backgroundImage: `url(${heroImage})`,
  }}
>

 {/* LEFT CONTENT */}

<div className="hero-left">
  <h1>
    Turn  every order into a <br />
    direct guest relationship.
  </h1>
  <p>
    Use QR prompts to collect private feedback, grow your guest list, <br />
    send return offers and see what's working each week.
  </p>
</div>

      {/* RIGHT FORM */}

      <div className="form-wrapper-engine">

        {/* ===================================================== */}
        {/* STEP 1 */}
        {/* ===================================================== */}

        {currentStep === 1 && (

          <div className="form-box">

            <h2>
              Request your guided Tummly trial
            </h2>

            <p className="top-text">
              Tell us about your restaurant. We'll verify your email,
              review your details and send the next setup step if your
              business is a fit for the trial.
            </p>

            {
              error && (

                <div
                  style={{
                    background: "#ffebee",
                    color: "#c62828",
                    padding: "12px",
                    borderRadius: "8px",
                    marginBottom: "16px",
                    fontSize: "14px"
                  }}
                >
                  {error}
                </div>
              )
            }

            <form
              className="form-actual-body"
              onSubmit={handleTrialSubmit}
            >

              <div className="input-group">

                <input
                  type="text"
                  name="businessName"
                  placeholder="Restaurant / business name"
                  value={formData.businessName}
                  onChange={handleChange}
                  required
                />

              </div>

              <div className="row">

                <div className="input-group">

                  <select
                    name="businessCategory"
                    value={formData.businessCategory}
                    onChange={handleChange}
                    required
                  >
                    <option value="">
                      Business category
                    </option>

                    <option value="Cafe">
                      Café / Coffee Shop
                    </option>

                    <option value="Restaurant">
                      Restaurant
                    </option>

                    <option value="Takeaway">
                      Takeaway
                    </option>

                    <option value="Hospitality Group">
                      Hospitality Group
                    </option>

                  </select>

                </div>

                <div className="input-group">

                  <select
                    name="locations"
                    value={formData.locations}
                    onChange={handleChange}
                    required
                  >
                    <option value="">
                      Number of locations
                    </option>

                    <option value="1">
                      1 Location
                    </option>

                    <option value="2-5">
                      2-5 Locations
                    </option>

                    <option value="6+">
                      6+ Locations
                    </option>

                  </select>

                </div>

              </div>

              <div className="input-group">

                <input
                  type="url"
                  name="businessLink"
                  placeholder="Business link"
                  value={formData.businessLink}
                  onChange={handleChange}
                />

              </div>

              <div className="input-group">

                <input
                  type="text"
                  name="fullName"
                  placeholder="Your full name"
                  value={formData.fullName}
                  onChange={handleChange}
                  required
                />

              </div>

              <div className="row">

                <div className="input-group">

                  <input
                    type="email"
                    name="email"
                    placeholder="Email address"
                    value={formData.email}
                    onChange={handleChange}
                    required
                  />

                </div>

                <div className="input-group">

                  <input
                    type="tel"
                    name="mobile"
                    placeholder="Mobile number"
                    value={formData.mobile}
                    onChange={handleChange}
                    required
                  />

                </div>

              </div>

              <div className="row">

                <div className="input-group">

                  <select
                    name="role"
                    value={formData.role}
                    onChange={handleChange}
                    required
                  >
                    <option value="">
                      Your role
                    </option>

                    <option value="Owner">
                      Owner
                    </option>

                    <option value="Manager">
                      Manager
                    </option>

                    <option value="Marketing">
                      Marketing
                    </option>

                    <option value="Staff">
                      Staff
                    </option>

                  </select>

                </div>

                <div className="input-group">

                  <select
                    name="goal"
                    value={formData.goal}
                    onChange={handleChange}
                    required
                  >
                    <option value="">
                      Main goal
                    </option>

                    <option value="Guest feedback">
                      Guest feedback
                    </option>

                    <option value="Guest retention">
                      Guest retention
                    </option>

                    <option value="Marketing campaigns">
                      Marketing campaigns
                    </option>

                    <option value="Business growth">
                      Business growth
                    </option>

                  </select>

                </div>

              </div>

              <div className="checkbox">

                <input
                  type="checkbox"
                  id="consent"
                  name="termsAccepted"
                  checked={formData.termsAccepted}
                  onChange={handleChange}
                  required
                />

                <label htmlFor="consent">
                  I confirm I’m requesting Tummly for a restaurant or
                  hospitality business. I agree that Tummly can contact
                  me about this trial request, and I accept the Terms and
                  Privacy Notice.
                </label>

              </div>

              <button
                type="submit"
                className="submit-btn"
                disabled={loading}
              >
                {
                  loading
                    ? "Submitting..."
                    : "Request trial setup"
                }
              </button>

            </form>

            <p className="signin">
              Already have an account?
              <a href="#"> Sign in</a>
            </p>

            <p className="bottom-note">
              For restaurants and hospitality operators only.
              <br />
              No payment is taken on this form.
            </p>

          </div>
        )}

        {/* ===================================================== */}
        {/* STEP 2 OTP */}
        {/* ===================================================== */}

        {currentStep === 2 && (

          <div className="form-box">

            <h2>
              Request your guided Tummly trial
            </h2>

            <p className="top-text">
              Tell us about your restaurant. We'll verify your email,
              review your details and send the next setup step if your
              business is a fit for the trial.
            </p>

            {
              error && (

                <div
                  style={{
                    background: "#ffebee",
                    color: "#c62828",
                    padding: "12px",
                    borderRadius: "8px",
                    marginBottom: "16px",
                    fontSize: "14px"
                  }}
                >
                  {error}
                </div>
              )
            }

            <div className="verification-sub-frame">

              <h3>Check your email</h3>

              <p className="sub-alert-text">
                We've sent a 6-digit verification code to:
                <strong> {formData.email}</strong>
              </p>

              <p className="instruction-lead">
                Enter the code below to verify your email and continue
                your trial request.
              </p>

              <form onSubmit={handleOtpVerify}>

                <div className="pin-code-row">

                  {
                    otp.map((digit, index) => (

                      <input
                        key={index}
                        id={`otp-${index}`}
                        type="text"
                        className="otp-field"
                        maxLength="1"
                        value={digit}
                        onChange={(e) =>
                          handleOtpChange(
                            e.target.value,
                            index
                          )
                        }
                        required
                      />
                    ))
                  }

                </div>

                <button
                  type="submit"
                  className="submit-btn"
                  disabled={otpLoading}
                >
                  {
                    otpLoading
                      ? "Verifying..."
                      : "Verify email"
                  }
                </button>

              </form>

              <div className="action-footer-links">

                <span
                  className="pseudo-link"
                  onClick={async () => {

                    try {

                      setError("");

                      /*
                       =========================================
                       RESEND OTP API
                       =========================================
                      */

                      await submitTrialRequest(formData);

                      alert(
                        "A new verification code has been sent to your email."
                      );

                    } catch (err) {

                      console.log(err);

                      setError(
                        err?.response?.data?.message ||
                        "Unable to resend verification code."
                      );
                    }
                  }}
                >
                  Resend code
                </span>

                <span
                  className="pseudo-link"
                  onClick={() => {

                    /*
                     =========================================
                     GO BACK TO STEP 1
                     =========================================
                    */

                    setCurrentStep(1);

                    /*
                     =========================================
                     CLEAR OLD EMAIL
                     =========================================
                    */

                    setFormData((prev) => ({
                      ...prev,
                      email: ""
                    }));

                    /*
                     =========================================
                     CLEAR OTP
                     =========================================
                    */

                    setOtp([
                      "",
                      "",
                      "",
                      "",
                      "",
                      ""
                    ]);

                    /*
                     =========================================
                     CLEAR ERRORS
                     =========================================
                    */

                    setError("");
                  }}
                >
                  Change email address
                </span>

              </div>
              <p className="spam-disclaimer">
                Didn't receive it? Check your spam folder or resend the code.
              </p>

            </div>

          </div>
        )}

        {/* ===================================================== */}
        {/* STEP 3 SUCCESS */}
        {/* ===================================================== */}

        {currentStep === 3 && (

          <div className="form-box">

            <h2>
              Request your guided Tummly trial
            </h2>

            <div className="success-message-block">

              <h3 className="success-lead-heading">
                Email verified — your request is now in review
              </h3>

              <p className="success-subtext">
                Thanks. We've verified your email and received your trial request.
              </p>

              <p className="success-subtext">
                We'll review your restaurant details and send the next setup
                step if your business is eligible.
              </p>

              <h4 className="timeline-title">
                What happens next
              </h4>

              <ol className="step-sequential-list">

                <li>
                  We review your restaurant and location details.
                </li>

                <li>
                  If approved, we send a secure setup link.
                </li>

                <li>
                  You create your account and confirm your restaurant workspace.
                </li>

                <li>
                  You can then set up QR prompts, feedback, offers and your first campaign.
                </li>

              </ol>

              <button
                type="button"
                className="submit-btn success-green-btn"
                onClick={resetForm}
              >
                Back to Tummly
              </button>

              <p className="wrong-email-footer">

                Used the wrong email?

                <span className="pseudo-link">
                  {" "}Submit the form again
                </span>

              </p>

            </div>

          </div>
        )}

      </div>

    </section>
  );
}

export default Hero;