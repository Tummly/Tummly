import React from "react";
import "../../assets/css/homepage.css";
import trial1 from "../../assets/images/trial1.jpg";
import trial2 from "../../assets/images/trial2.jpg";
import trial3 from "../../assets/images/trial3.jpg";
import trial4 from "../../assets/images/trial4.jpg";

function GuidedTrial() {
  return (
    <section className="trial-included-section">

      <div className="trial-container">

        <div className="trial-header">

          <h2>What’s included in your guided trial</h2>

          <p>
            Your guided trial includes access to the core workspace, starter
            materials and a standard message allowance to help you launch your
            first Guest Loop. We confirm your setup before your workspace is
            opened.
          </p>

        </div>

        <div className="trial-grid">

          <div className="trial-card">

            <div className="trial-image-wrapper">
              <img
                src={trial1}
                alt="Standard workspace access"
              />
            </div>

            <div className="trial-card-content">

              <h3>Standard workspace access</h3>

              <p>
                Use the Tummly workspace during your trial, including guest
                forms, private feedback, guest list, offers, campaigns and your
                weekly brief.
              </p>

            </div>

          </div>

          <div className="trial-card">

            <div className="trial-image-wrapper bg-light">
              <img
                src={trial2}
                alt="Starter QR Pack"
              />
            </div>

            <div className="trial-card-content">

              <h3>Starter QR Pack</h3>

              <p>
                Approved trials include starter printed QR materials matched to
                your setup, so guests can scan and respond from key in-store or
                takeaway touchpoints.
              </p>

            </div>

          </div>

          <div className="trial-card">

            <div className="trial-image-wrapper">
              <img
                src={trial3}
                alt="Smart Guest Links"
              />
            </div>

            <div className="trial-card-content">

              <h3>Smart Guest Links</h3>

              <p>
                Use trackable links for digital channels, receipts, messages or
                places where a printed QR prompt is not the best fit.
              </p>

            </div>

          </div>

          <div className="trial-card">

            <div className="trial-image-wrapper">
              <img
                src={trial4}
                alt="Feedback and opt-in form"
              />
            </div>

            <div className="trial-card-content">

              <h3>Feedback and opt-in form</h3>

              <p>
                Let guests share quick private feedback and choose whether to
                join your restaurant’s guest list.
              </p>

            </div>

          </div>

        </div>

        <div className="trial-controls">

          <div className="trial-arrows">

            <button
              className="trial-btn trial-btn-prev"
              aria-label="Previous page"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="15 18 9 12 15 6"></polyline>
              </svg>
            </button>

            <button
              className="trial-btn trial-btn-next"
              aria-label="Next page"
            >
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="9 18 15 12 9 6"></polyline>
              </svg>
            </button>

          </div>

          <div className="trial-progress-container">
            <div className="trial-progress-line"></div>
          </div>

        </div>

      </div>

    </section>
  );
}

export default GuidedTrial;