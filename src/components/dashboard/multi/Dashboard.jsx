import React from "react";

function Dashboard() {
  // Logout function jo admin ko wapis home page par redirect karega
  const handleLogout = () => {
    // Agar aap React Router use kar rahe hain toh yahan useNavigate() use kar sakte hain
    // Filhal standard web standard ke mutabik yeh home page (/) par redirect karega:
    window.location.href = "/";
  };

  return (
    <div
      style={{
        display: "flex",
        minHeight: "100vh",
        background: "#f4f5f7",
        fontFamily: "'Inter', sans-serif",
        color: "#1e293b",
      }}
    >
      {/* 1. SIDEBAR */}
      <div
        style={{
          width: "240px",
          background: "#ffffff",
          borderRight: "1px solid #e2e8f0",
          display: "flex",
          flexDirection: "column",
          padding: "24px 16px",
          boxSizing: "border-box",
          justifyContent: "space-between", // Isse niche wala content bottom mein push hojayega
        }}
      >
        <div>
          {/* Brand Logo */}
          <div style={{ fontSize: "22px", fontWeight: "800", color: "#00aa5b", marginBottom: "32px", display: "flex", alignItems: "center", gap: "8px" }}>
            <span style={{ fontSize: "24px" }}>🥗</span> tummly
          </div>

          {/* Navigation Links */}
          <nav style={{ display: "flex", flexDirection: "column", gap: "8px", width: "100%" }}>
            {[
              { name: "Home", icon: "🏠", active: true },
              { name: "Customer club", icon: "👥", active: false },
              { name: "Feedback", icon: "💬", active: false },
              { name: "Campaigns", icon: "📣", active: false },
              { name: "Offers", icon: "🏷️", active: false },
              { name: "Reports", icon: "📊", active: false },
              { name: "Settings", icon: "⚙️", active: false },
            ].map((item, index) => (
              <div
                key={index}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "12px",
                  padding: "12px 16px",
                  borderRadius: "8px",
                  cursor: "pointer",
                  fontWeight: "500",
                  fontSize: "14px",
                  background: item.active ? "#e6f6ef" : "transparent",
                  color: item.active ? "#00aa5b" : "#64748b",
                  transition: "all 0.2s",
                }}
              >
                <span>{item.icon}</span>
                <span>{item.name}</span>
              </div>
            ))}
          </nav>
        </div>
      </div>

      {/* 2. MAIN CONTENT AREA */}
      <div style={{ flex: 1, padding: "40px", overflowY: "auto", boxSizing: "border-box" }}>
        
        {/* TOP BAR (Header & Logout Button) */}
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "8px" }}>
          <h1 style={{ margin: 0, fontSize: "28px", fontWeight: "700", color: "#0f172a" }}>Home</h1>
          
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            {/* Filter Buttons */}
            <div style={{ display: "flex", gap: "12px" }}>
              <button style={{ background: "#fff", border: "1px solid #cbd5e1", padding: "8px 16px", borderRadius: "6px", cursor: "pointer", fontSize: "14px" }}>📅 Last 30 days</button>
              <button style={{ background: "#fff", border: "1px solid #cbd5e1", padding: "8px 16px", borderRadius: "6px", cursor: "pointer", fontSize: "14px" }}>📤 Export</button>
              <button style={{ background: "#00aa5b", color: "#fff", border: "none", padding: "8px 16px", borderRadius: "6px", cursor: "pointer", fontWeight: "600", fontSize: "14px" }}>+ Create campaign</button>
            </div>

            {/* --- NEWLY ADDED LOGOUT SECTION --- */}
            <div style={{ width: "1px", height: "30px", background: "#cbd5e1" }}></div> {/* Divider line */}
            
            <button 
              onClick={handleLogout}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                background: "#fee2e2", // Light red background
                color: "#dc2626", // Red text
                border: "1px solid #fca5a5",
                padding: "8px 16px",
                borderRadius: "6px",
                cursor: "pointer",
                fontWeight: "600",
                fontSize: "14px",
                transition: "all 0.2s"
              }}
              onMouseEnter={(e) => e.currentTarget.style.background = "#fca5a5"}
              onMouseLeave={(e) => e.currentTarget.style.background = "#fee2e2"}
            >
              <span>🚪</span> Logout
            </button>
            {/* --------------------------------- */}
          </div>
        </div>
        
        <p style={{ color: "#64748b", marginTop: 0, marginBottom: "24px", fontSize: "14px" }}>
          Your Guest Loop at a glance — scans, opt-ins and weekly actions for your restaurant.
        </p>

        {/* Highlight Banner */}
        <h3 style={{ fontSize: "16px", fontWeight: "600", color: "#0f172a", marginBottom: "20px" }}>
          Your Customer Club gained <span style={{ color: "#00aa5b" }}>126 new guests</span> this month
        </h3>

        {/* Key Performance Mini Cards */}
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "20px", marginBottom: "30px" }}>
          <div style={{ background: "#fff", padding: "20px", borderRadius: "12px", border: "1px solid #e2e8f0", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <p style={{ margin: "0 0 6px 0", color: "#64748b", fontSize: "13px" }}>New members ℹ️</p>
              <h2 style={{ margin: 0, fontSize: "28px", fontWeight: "700" }}>21</h2>
              <span style={{ color: "#00aa5b", fontSize: "12px", fontWeight: "600" }}>↗ +18%</span>
            </div>
            <span style={{ fontSize: "24px", opacity: 0.6 }}>📈</span>
          </div>

          <div style={{ background: "#fff", padding: "20px", borderRadius: "12px", border: "1px solid #e2e8f0", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <p style={{ margin: "0 0 6px 0", color: "#64748b", fontSize: "13px" }}>Redeemed ℹ️</p>
              <h2 style={{ margin: 0, fontSize: "28px", fontWeight: "700" }}>63</h2>
              <span style={{ color: "#00aa5b", fontSize: "12px", fontWeight: "600" }}>↗ +31%</span>
            </div>
            <span style={{ fontSize: "24px", opacity: 0.6 }}>🏷️</span>
          </div>
        </div>

        {/* Grid Sections */}
        <div style={{ display: "flex", flexDirection: "column", gap: "24px" }}>
          
          {/* Row 1: Customer Club Growth & Feedback Quick Summary */}
          <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr", gap: "24px" }}>
            <div style={{ background: "#fff", padding: "24px", borderRadius: "14px", border: "1px solid #e2e8f0" }}>
              <h4 style={{ margin: "0 0 16px 0", fontSize: "16px", fontWeight: "600" }}>Customer Club growth ℹ️</h4>
              <div style={{ display: "flex", gap: "40px", marginBottom: "20px" }}>
                <div>
                  <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>Total guests</p>
                  <h3 style={{ margin: 0, fontSize: "22px", fontWeight: "700" }}>1,240</h3>
                </div>
                <div>
                  <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>New this week</p>
                  <h3 style={{ margin: 0, fontSize: "22px", fontWeight: "700", color: "#00aa5b" }}>+12</h3>
                </div>
              </div>
              <button style={{ background: "#f1f5f9", border: "none", padding: "8px 16px", borderRadius: "6px", cursor: "pointer", fontSize: "13px", fontWeight: "500" }}>View guests</button>
            </div>

            <div style={{ background: "#fff", padding: "24px", borderRadius: "14px", border: "1px solid #e2e8f0" }}>
              <h4 style={{ margin: "0 0 16px 0", fontSize: "16px", fontWeight: "600" }}>Feedback summary ℹ️</h4>
              <div style={{ display: "flex", justifyContent: "space-between" }}>
                <div>
                  <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>Needs recovery</p>
                  <h3 style={{ margin: 0, fontSize: "22px", fontWeight: "700", color: "#ef4444" }}>6</h3>
                </div>
                <div>
                  <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>Top issue</p>
                  <h3 style={{ margin: 0, fontSize: "18px", fontWeight: "700" }}>Speed</h3>
                </div>
              </div>
            </div>
          </div>

          {/* Row 2: Offers Analytics */}
          <div style={{ background: "#fff", padding: "24px", borderRadius: "14px", border: "1px solid #e2e8f0" }}>
            <h4 style={{ margin: "0 0 16px 0", fontSize: "16px", fontWeight: "600" }}>Offers ℹ️</h4>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: "20px" }}>
              <div>
                <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>Claimed</p>
                <h3 style={{ margin: 0, fontSize: "20px", fontWeight: "700" }}>31</h3>
              </div>
              <div>
                <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>Redeemed</p>
                <h3 style={{ margin: 0, fontSize: "20px", fontWeight: "700" }}>14</h3>
              </div>
              <div>
                <p style={{ margin: "0 0 4px 0", color: "#64748b", fontSize: "12px" }}>New Item</p>
                <h3 style={{ margin: 0, fontSize: "20px", fontWeight: "700", color: "#00aa5b" }}>9 redeemed</h3>
              </div>
            </div>
          </div>

          {/* Row 3: Needs Attention List */}
          <div style={{ background: "#fff", padding: "24px", borderRadius: "14px", border: "1px solid #e2e8f0" }}>
            <h4 style={{ margin: "0 0 16px 0", fontSize: "16px", fontWeight: "600" }}>Needs attention ℹ️</h4>
            <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
              {[
                { text: "6 negative feedback items need review", icon: "⚠️" },
                { text: "14 claimed offers not redeemed", icon: "🏷️" },
                { text: "SMS credits running low", icon: "💬" },
              ].map((item, idx) => (
                <div
                  key={idx}
                  style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    padding: "14px 16px",
                    background: "#f8fafc",
                    borderRadius: "8px",
                    border: "1px solid #e2e8f0",
                  }}
                >
                  <div style={{ display: "flex", alignItems: "center", gap: "12px", fontSize: "14px" }}>
                    <span>{item.icon}</span>
                    <span style={{ fontWeight: "500" }}>{item.text}</span>
                  </div>
                  <span style={{ color: "#00aa5b", cursor: "pointer", fontSize: "13px", fontWeight: "600" }}>Open →</span>
                </div>
              ))}
            </div>
          </div>

          {/* Row 4: This week's brief */}
          <div style={{ background: "#fff", padding: "24px", borderRadius: "14px", border: "1px solid #e2e8f0" }}>
            <h4 style={{ margin: "0 0 4px 0", fontSize: "16px", fontWeight: "600" }}>This week's brief ℹ️</h4>
            <p style={{ margin: "0 0 16px 0", color: "#64748b", fontSize: "14px" }}>Growth is up, but speed complaints increased...</p>
            <button style={{ background: "#f1f5f9", border: "none", padding: "8px 16px", borderRadius: "6px", cursor: "pointer", fontSize: "13px", fontWeight: "500" }}>View brief</button>
          </div>

        </div>
      </div>
    </div>
  );
}

export default Dashboard;