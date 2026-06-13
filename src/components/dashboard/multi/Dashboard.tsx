function MultiDashboard() {
  const handleLogout = () => {
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
      <div
        style={{
          width: "240px",
          background: "#ffffff",
          borderRight: "1px solid #e2e8f0",
          display: "flex",
          flexDirection: "column",
          padding: "24px 16px",
          boxSizing: "border-box",
          justifyContent: "space-between",
        }}
      >
        <div>
          <div
            style={{
              fontSize: "22px",
              fontWeight: "800",
              color: "#00aa5b",
              marginBottom: "32px",
              display: "flex",
              alignItems: "center",
              gap: "8px",
            }}
          >
            <span style={{ fontSize: "24px" }}>🥗</span> tummly (Multi SaaS)
          </div>

          <nav
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "8px",
              width: "100%",
            }}
          >
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
                }}
              >
                <span>{item.icon}</span>
                <span>{item.name}</span>
              </div>
            ))}
          </nav>
        </div>
      </div>

      <div style={{ flex: 1, padding: "40px", overflowY: "auto" }}>
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            marginBottom: "8px",
            alignItems: "center",
          }}
        >
          <h1 style={{ fontSize: "28px", fontWeight: "700" }}>
            Multi Admin Dashboard
          </h1>

          <div style={{ display: "flex", gap: "12px", alignItems: "center" }}>
            <button>📅 Last 30 days</button>
            <button>📤 Export</button>
            <button>+ Create campaign</button>

            <div style={{ width: "1px", height: "30px", background: "#ccc" }} />

            <button
              onClick={handleLogout}
              style={{
                background: "#fee2e2",
                color: "#dc2626",
                padding: "8px 12px",
                borderRadius: "6px",
                border: "1px solid #fca5a5",
              }}
            >
              🚪 Logout
            </button>
          </div>
        </div>

        <p style={{ color: "#64748b" }}>
          Multi-tenant SaaS dashboard with full analytics & controls.
        </p>
      </div>
    </div>
  );
}

export default MultiDashboard;
