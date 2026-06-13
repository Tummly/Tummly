import type { CSSProperties } from "react";
import { Link } from "react-router-dom";
import "../../assets/css/homepage.css";
import ctaBgImage from "../../assets/images/new.png";

function CTALaunch() {
  const sectionStyle = {
    "--cta-bg": `url(${ctaBgImage})`,
  } as CSSProperties;

  return (
    <section className="cta-launch-section" style={sectionStyle}>
      <div className="cta-container">
        <div className="cta-content-grid-wrapper">
          <div className="cta-content-box">
            <h2>Ready to launch your first Guest Loop?</h2>

            <p className="cta-description">
              Request guided access and start turning everyday guest touchpoints
              into private feedback, consented guest sign-ups and simple return
              offers.
            </p>

            <div className="cta-action-row">
              <a href="#" className="btn-cta-primary-white">
                Request trial setup
              </a>

              <span className="cta-signin-text-white">
                Already have an account? <Link to="/Login">Sign in</Link>
              </span>
            </div>

            <p className="cta-disclaimer-fade">
              No payment is taken when you request access.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}

export default CTALaunch;
