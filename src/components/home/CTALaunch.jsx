import React from "react";
import { Link } from 'react-router-dom';


function CTALaunch() {
  return (
    <section className="cta-launch-section">
      <div className="cta-container">
        <div className="cta-content-box">
          <h2>Ready to launch your first Guest Loop?</h2>

          <p className="cta-description">
            Request guided access and start turning everyday guest touchpoints
            into private feedback, consented guest sign-ups and simple return
            offers.
          </p>

<div className="cta-action-row">
  <a href="#" className="btn-cta-primary">
    Request trial setup
  </a>

  <span className="cta-signin-text">
    {/* <a> ki jagah <Link> aur href ki jagah to */}
    Already have an account? <Link to="/Login">Sign in</Link>
  </span>
</div>

          <p className="cta-disclaimer">
            No payment is taken when you request access.
          </p>
        </div>

        {/* MOBILE IMAGE ONLY */}
        <div className="cta-image-mobile"></div>

      </div>
    </section>
  );
}

export default CTALaunch;