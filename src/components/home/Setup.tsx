import "../../assets/css/homepage.css";

function Setup() {
  return (
    <section className="setup-section">
      <div className="container">
        <div className="section-header">
          <h2>Choose the setup that fits your restaurant</h2>

          <p>
            Start with one location or request a guided setup for multiple
            sites.
            <br />
            We’ll review your details before sending the next setup step.
          </p>
        </div>

        <div className="cards-wrapper">
          <div className="setup-card">
            <div className="card-header">
              <h3>Single-location guided trial</h3>
            </div>

            <div className="card-body">
              <p className="description">
                For independent restaurants, cafés, takeaways and quick-service
                operators starting with one site.
              </p>

              <p className="details">
                Includes one workspace, your first QR prompts or guest links, a
                starter feedback form, one offer and a weekly brief.
              </p>

              <a href="#" className="btn-setup">
                Request single-location trial
              </a>
            </div>
          </div>

          <div className="setup-card">
            <div className="card-header">
              <h3>Multi-location setup review</h3>
            </div>

            <div className="card-body">
              <p className="description">
                For operators with 2+ locations, area managers and small groups
                that need location-level setup.
              </p>

              <p className="details">
                Includes location structure, team roles, location-specific QR
                prompts, rollout checklist and reporting by location.
              </p>

              <a href="#" className="btn-setup">
                Request multi-location setup
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

export default Setup;
