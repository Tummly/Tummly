function Dashboard() {
  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#f5f7fb",
        padding: "30px",
        fontFamily: "Inter, sans-serif",
      }}
    >
      {/* Header */}
      <div
        style={{
          marginBottom: "30px",
        }}
      >
        <h1
          style={{
            margin: 0,
            color: "#111827",
            fontSize: "32px",
            fontWeight: "700",
          }}
        >
          Multi User Dashboard
        </h1>

        <p
          style={{
            color: "#6b7280",
            marginTop: "8px",
          }}
        >
          Welcome back! Here's an overview of your platform.
        </p>
      </div>

      {/* Stats Cards */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns:
            "repeat(auto-fit, minmax(250px, 1fr))",
          gap: "20px",
          marginBottom: "30px",
        }}
      >
        {[
          {
            title: "Total Users",
            value: "1,245",
            icon: "👥",
          },
          {
            title: "Active Users",
            value: "985",
            icon: "🟢",
          },
          {
            title: "Subscriptions",
            value: "542",
            icon: "💳",
          },
          {
            title: "Revenue",
            value: "$12,450",
            icon: "💰",
          },
        ].map((card, index) => (
          <div
            key={index}
            style={{
              background: "#fff",
              padding: "24px",
              borderRadius: "18px",
              boxShadow:
                "0 2px 10px rgba(0,0,0,0.06)",
            }}
          >
            <div
              style={{
                fontSize: "30px",
                marginBottom: "12px",
              }}
            >
              {card.icon}
            </div>

            <p
              style={{
                margin: 0,
                color: "#6b7280",
              }}
            >
              {card.title}
            </p>

            <h2
              style={{
                marginTop: "10px",
                color: "#111827",
              }}
            >
              {card.value}
            </h2>
          </div>
        ))}
      </div>

      {/* Main Content */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "2fr 1fr",
          gap: "20px",
        }}
      >
        {/* Activity */}
        <div
          style={{
            background: "#fff",
            padding: "24px",
            borderRadius: "18px",
            boxShadow:
              "0 2px 10px rgba(0,0,0,0.06)",
          }}
        >
          <h3
            style={{
              marginBottom: "20px",
            }}
          >
            Recent Activity
          </h3>

          <ul
            style={{
              padding: 0,
              listStyle: "none",
            }}
          >
            <li style={{ padding: "12px 0" }}>
              ✅ New user registered
            </li>

            <li style={{ padding: "12px 0" }}>
              💳 Subscription upgraded
            </li>

            <li style={{ padding: "12px 0" }}>
              📧 Invite sent successfully
            </li>

            <li style={{ padding: "12px 0" }}>
              🔐 Account verified
            </li>
          </ul>
        </div>

        {/* Quick Actions */}
        <div
          style={{
            background: "#fff",
            padding: "24px",
            borderRadius: "18px",
            boxShadow:
              "0 2px 10px rgba(0,0,0,0.06)",
          }}
        >
          <h3
            style={{
              marginBottom: "20px",
            }}
          >
            Quick Actions
          </h3>

          <button
            style={{
              width: "100%",
              background: "#2563eb",
              color: "#fff",
              border: "none",
              padding: "12px",
              borderRadius: "10px",
              marginBottom: "12px",
              cursor: "pointer",
            }}
          >
            Add User
          </button>

          <button
            style={{
              width: "100%",
              background: "#16a34a",
              color: "#fff",
              border: "none",
              padding: "12px",
              borderRadius: "10px",
              marginBottom: "12px",
              cursor: "pointer",
            }}
          >
            Manage Plans
          </button>

          <button
            style={{
              width: "100%",
              background: "#7c3aed",
              color: "#fff",
              border: "none",
              padding: "12px",
              borderRadius: "10px",
              cursor: "pointer",
            }}
          >
            View Reports
          </button>
        </div>
      </div>
    </div>
  );
}

export default Dashboard;