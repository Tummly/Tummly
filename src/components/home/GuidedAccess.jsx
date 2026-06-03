import React from "react";
import "../../assets/css/homepage.css";
import step1 from "../../assets/images/step1.jpg";
import step2 from "../../assets/images/step2.jpg";
import step3 from "../../assets/images/step3.jpg";

function GuidedAccess() {
  return (
    <section className="guided-access-section">

      <div className="guided-container">

        <div className="guided-header">

          <h2>How guided access works</h2>

          <p>
            Request access, verify your email and create your restaurant
            workspace once approved.
          </p>

        </div>

        <div className="steps-grid">

          <div className="step-card">

            <div className="step-image-wrapper">
              <img
                src={step1}
                alt="Request your trial"
              />
            </div>

            <span className="step-number">Step 1</span>

            <h3>Request your trial</h3>

            <p>
              Tell us about your restaurant, business category and number of
              locations so we can route your setup correctly.
            </p>

          </div>

          <div className="step-card">

            <div className="step-image-wrapper">
              <img
                src={step2}
                alt="Verify and review"
              />
            </div>

            <span className="step-number">Step 2</span>

            <h3>Verify and review</h3>

            <p>
              Verify your email with a code. We’ll review your restaurant
              details before sending the next setup step.
            </p>

          </div>

          <div className="step-card">

            <div className="step-image-wrapper">
              <img
                src={step3}
                alt="Create your workspace"
              />
            </div>

            <span className="step-number">Step 3</span>

            <h3>Create your workspace</h3>

            <p>
              If approved, you’ll receive a secure setup link to create your
              account and confirm your restaurant workspace.
            </p>

          </div>

        </div>

        <div className="guided-footer-note">

          <p>
            After your workspace is created, we’ll help you prepare your first
            Guest Loop with starter QR materials, guest form setup, offer
            guidance and launch support.
          </p>

        </div>

      </div>

    </section>
  );
}

export default GuidedAccess;