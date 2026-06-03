import "../../../assets/css/adminDashboard.css";

import { useEffect, useState } from "react";
import {
  getTrialRequests,
  approveTrialRequest,
  resendInvite,
  declineTrialRequest,
  requestMoreInfo,
    updateStatus
} from "../../../api/adminApi";

function Dashboard() {
  const [requests, setRequests] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);

      const result =
        await getTrialRequests();

      setRequests(result.data || []);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (id) => {
    try {
      await approveTrialRequest(id);

      alert("Request Approved");

      loadData();
    } catch (error) {
      console.error(error);
      alert("Approval Failed");
    }
  };

const handleDecline = async (id) => {
  try {
    await updateStatus({
  TrialRequestId: id,
  Status: "DECLINED",
  DeclineReason: "Not eligible",
  AdminNotes: "Rejected by admin"
});

    alert("Request Declined");
    loadData();
  } catch (error) {
    console.error(error);
    alert("Decline Failed");
  }
};

const handleRequestMoreInfo = async (id) => {
  try {
    await updateStatus({
  TrialRequestId: id,
  Status: "MORE_INFO_REQUESTED",
  MoreInfoMessage: "Please provide required documents",
  AdminNotes: "Need more info"
});

    alert("More Info Email Sent");
    loadData();
  } catch (error) {
    console.error(error);
    alert("Failed");
  }
};

  const filteredRequests =
    requests.filter((request) => {
      return (
        request.businessName
          ?.toLowerCase()
          .includes(search.toLowerCase()) ||
        request.fullName
          ?.toLowerCase()
          .includes(search.toLowerCase()) ||
        request.email
          ?.toLowerCase()
          .includes(search.toLowerCase())
      );
    });

  const totalRequests =
    requests.length;

  const approvedCount =
    requests.filter(
      (x) => x.isApproved
    ).length;

  const emailVerifiedCount =
    requests.filter(
      (x) => x.isEmailVerified
    ).length;

  const accountCreatedCount =
    requests.filter(
      (x) => x.isAccountCreated
    ).length;

    const thStyle = {
  padding: "14px",
  textAlign: "left",
  fontSize: "14px",
};

const tdStyle = {
  padding: "14px",
  fontSize: "14px",
  color: "#334155",
};

  return (
  <div
  style={{
    padding: "30px",
    background: "#f8fafc",
    minHeight: "100vh",
    fontFamily: "Arial, sans-serif",
  }}
>
  <h1
    style={{
      marginBottom: "25px",
      color: "#0f172a",
      fontWeight: "700",
    }}
  >
    Admin Dashboard
  </h1>

  {/* STATS */}

  <div
    style={{
      display: "flex",
      gap: "20px",
      marginBottom: "30px",
      flexWrap: "wrap",
    }}
  >
    {[
      {
        title: "Total Requests",
        value: totalRequests,
      },
      {
        title: "Email Verified",
        value: emailVerifiedCount,
      },
      {
        title: "Approved",
        value: approvedCount,
      },
      {
        title: "Accounts Created",
        value: accountCreatedCount,
      },
    ].map((item, index) => (
      <div
        key={index}
        style={{
          background: "#fff",
          padding: "25px",
          minWidth: "220px",
          borderRadius: "12px",
          boxShadow:
            "0 4px 12px rgba(0,0,0,0.08)",
          border:
            "1px solid rgba(0,0,0,0.05)",
        }}
      >
        <h3
          style={{
            margin: 0,
            color: "#64748b",
            fontSize: "15px",
          }}
        >
          {item.title}
        </h3>

        <h2
          style={{
            marginTop: "12px",
            marginBottom: 0,
            color: "#0f172a",
            fontSize: "30px",
          }}
        >
          {item.value}
        </h2>
      </div>
    ))}
  </div>

  {/* SEARCH */}

  <div
    style={{
      marginBottom: "20px",
    }}
  >
    <input
      type="text"
      placeholder="🔍 Search by business, owner or email"
      value={search}
      onChange={(e) =>
        setSearch(e.target.value)
      }
      style={{
        width: "380px",
        padding: "12px 15px",
        borderRadius: "10px",
        border: "1px solid #cbd5e1",
        background: "#fff",
        outline: "none",
        fontSize: "14px",
        boxShadow:
          "0 2px 6px rgba(0,0,0,0.05)",
      }}
    />
  </div>

  {loading ? (
    <div
      style={{
        background: "#fff",
        padding: "40px",
        borderRadius: "12px",
        textAlign: "center",
        boxShadow:
          "0 4px 12px rgba(0,0,0,0.08)",
      }}
    >
      <h3>Loading...</h3>
    </div>
  ) : (
    <div
      style={{
        overflowX: "auto",
        background: "#fff",
        borderRadius: "12px",
        boxShadow:
          "0 4px 12px rgba(0,0,0,0.08)",
      }}
    >
      <table
        style={{
          width: "100%",
          borderCollapse: "collapse",
        }}
      >
        <thead>
          <tr
            style={{
              background: "#0f172a",
              color: "#fff",
            }}
          >
            <th style={thStyle}>ID</th>
            <th style={thStyle}>Business</th>
            <th style={thStyle}>Category</th>
            <th style={thStyle}>Owner</th>
            <th style={thStyle}>Email</th>
            <th style={thStyle}>Mobile</th>
            <th style={thStyle}>Role</th>
            <th style={thStyle}>
              Account Type
            </th>
            <th style={thStyle}>Status</th>
            <th style={thStyle}>Created</th>
            <th style={thStyle}>Actions</th>
          </tr>
        </thead>

        <tbody>
          {filteredRequests.map(
            (request) => (
              <tr
                key={request.id}
                style={{
                  borderBottom:
                    "1px solid #e2e8f0",
                }}
              >
                <td style={tdStyle}>
                  {request.id}
                </td>

                <td style={tdStyle}>
                  {
                    request.businessName
                  }
                </td>

                <td style={tdStyle}>
                  {
                    request.businessCategory
                  }
                </td>

                <td style={tdStyle}>
                  {request.fullName}
                </td>

                <td style={tdStyle}>
                  {request.email}
                </td>

                <td style={tdStyle}>
                  {request.mobile}
                </td>

                <td style={tdStyle}>
                  {request.role}
                </td>

                <td style={tdStyle}>
                  {
                    request.accountType
                  }
                </td>

                <td style={tdStyle}>
                  <span
                    style={{
                      padding:
                        "6px 12px",
                      borderRadius:
                        "20px",
                      background:
                        request.isApproved
                          ? "#dcfce7"
                          : "#fef3c7",
                      color:
                        request.isApproved
                          ? "#15803d"
                          : "#b45309",
                      fontWeight:
                        "600",
                      fontSize:
                        "12px",
                    }}
                  >
                    {request.status}
                  </span>
                </td>

                <td style={tdStyle}>
                  {new Date(
                    request.createdAt
                  ).toLocaleDateString()}
                </td>

                <td style={tdStyle}>
                  {!request.isApproved && (
  <div
    style={{
      display: "flex",
      gap: "8px",
      flexWrap: "wrap",
    }}
  >
    <button
      onClick={() =>
        handleApprove(request.id)
      }
      style={{
        background: "#22c55e",
        color: "#fff",
        border: "none",
        padding: "10px 16px",
        borderRadius: "8px",
        cursor: "pointer",
        fontWeight: "600",
      }}
    >
      Approve
    </button>

    <button
      onClick={() =>
        handleDecline(request.id)
      }
      style={{
        background: "#ef4444",
        color: "#fff",
        border: "none",
        padding: "10px 16px",
        borderRadius: "8px",
        cursor: "pointer",
        fontWeight: "600",
      }}
    >
      Decline
    </button>

    <button
      onClick={() =>
        handleRequestMoreInfo(
          request.id
        )
      }
      style={{
        background: "#f59e0b",
        color: "#fff",
        border: "none",
        padding: "10px 16px",
        borderRadius: "8px",
        cursor: "pointer",
        fontWeight: "600",
      }}
    >
      More Info
    </button>
  </div>
)}

                  {request.isApproved &&
                    !request.isAccountCreated && (
                      <button
                        onClick={() =>
                          handleResendInvite(
                            request.id
                          )
                        }
                        style={{
                          background:
                            "#3b82f6",
                          color:
                            "#fff",
                          border:
                            "none",
                          padding:
                            "10px 16px",
                          borderRadius:
                            "8px",
                          cursor:
                            "pointer",
                          fontWeight:
                            "600",
                        }}
                      >
                        Resend Invite
                      </button>
                    )}

                  {request.isAccountCreated && (
                    <span
                      style={{
                        color:
                          "#16a34a",
                        fontWeight:
                          "600",
                      }}
                    >
                      ✅ Account Created
                    </span>
                  )}
                </td>
              </tr>
            )
          )}

          {filteredRequests.length ===
            0 && (
            <tr>
              <td
                colSpan="11"
                align="center"
                style={{
                  padding: "30px",
                }}
              >
                No Data Found
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )}
</div>
  );
}

export default Dashboard;