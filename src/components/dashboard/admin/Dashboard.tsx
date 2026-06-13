import "../../../assets/css/adminDashboard.css";

import { useEffect, useState } from "react";
import type { CSSProperties } from "react";
import {
  getTrialRequests,
  approveTrialRequest,
  resendInvite,
  updateStatus,
} from "../../../api/adminApi";
import type { AdminTrialRequest } from "../../../types/admin";
import { Button } from "@/components/ui/button";
import { FloatingLabelInput } from "@/components/ui/floating-label-input";

const thStyle: CSSProperties = {
  padding: "14px",
  textAlign: "left",
  fontSize: "14px",
};

const tdStyle: CSSProperties = {
  padding: "14px",
  fontSize: "14px",
  color: "#334155",
};

function Dashboard() {
  const [requests, setRequests] = useState<AdminTrialRequest[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    try {
      setLoading(true);
      const result = await getTrialRequests();
      setRequests(result);
    } catch (error) {
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const result = await getTrialRequests();
        if (active) {
          setRequests(result);
        }
      } catch (error) {
        if (active) {
          console.error(error);
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, []);

  const handleApprove = async (id: number) => {
    try {
      await approveTrialRequest(id);
      alert("Request Approved");
      loadData();
    } catch (error) {
      console.error(error);
      alert("Approval Failed");
    }
  };

  const handleDecline = async (id: number) => {
    try {
      await updateStatus({
        trialRequestId: id,
        status: "DECLINED",
        declineReason: "Not eligible",
        adminNotes: "Rejected by admin",
      });
      alert("Request Declined");
      loadData();
    } catch (error) {
      console.error(error);
      alert("Decline Failed");
    }
  };

  const handleRequestMoreInfo = async (id: number) => {
    try {
      await updateStatus({
        trialRequestId: id,
        status: "MORE_INFO_REQUESTED",
        moreInfoMessage: "Please provide required documents",
        adminNotes: "Need more info",
      });
      alert("More Info Email Sent");
      loadData();
    } catch (error) {
      console.error(error);
      alert("Failed");
    }
  };

  const handleResendInvite = async (id: number) => {
    try {
      await resendInvite(id);
      alert("Invite resent successfully");
      loadData();
    } catch (error) {
      console.error(error);
      alert("Failed to resend invite");
    }
  };

  const filteredRequests = requests.filter((request) => {
    return (
      request.businessName?.toLowerCase().includes(search.toLowerCase()) ||
      request.fullName?.toLowerCase().includes(search.toLowerCase()) ||
      request.email?.toLowerCase().includes(search.toLowerCase())
    );
  });

  const totalRequests = requests.length;
  const approvedCount = requests.filter((x) => x.isApproved).length;
  const emailVerifiedCount = requests.filter((x) => x.isEmailVerified).length;
  const accountCreatedCount = requests.filter((x) => x.isAccountCreated).length;

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

      <div
        style={{
          display: "flex",
          gap: "20px",
          marginBottom: "30px",
          flexWrap: "wrap",
        }}
      >
        {[
          { title: "Total Requests", value: totalRequests },
          { title: "Email Verified", value: emailVerifiedCount },
          { title: "Approved", value: approvedCount },
          { title: "Accounts Created", value: accountCreatedCount },
        ].map((item, index) => (
          <div
            key={index}
            style={{
              background: "#fff",
              padding: "25px",
              minWidth: "220px",
              borderRadius: "12px",
              boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
              border: "1px solid rgba(0,0,0,0.05)",
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

      <div style={{ marginBottom: "20px" }}>
        <FloatingLabelInput
          label="Search by business, owner or email"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {loading ? (
        <div
          style={{
            background: "#fff",
            padding: "40px",
            borderRadius: "12px",
            textAlign: "center",
            boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
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
            boxShadow: "0 4px 12px rgba(0,0,0,0.08)",
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
                <th style={thStyle}>Account Type</th>
                <th style={thStyle}>Status</th>
                <th style={thStyle}>Created</th>
                <th style={thStyle}>Actions</th>
              </tr>
            </thead>

            <tbody>
              {filteredRequests.map((request) => (
                <tr
                  key={request.id}
                  style={{
                    borderBottom: "1px solid #e2e8f0",
                  }}
                >
                  <td style={tdStyle}>{request.id}</td>
                  <td style={tdStyle}>{request.businessName}</td>
                  <td style={tdStyle}>{request.businessCategory}</td>
                  <td style={tdStyle}>{request.fullName}</td>
                  <td style={tdStyle}>{request.email}</td>
                  <td style={tdStyle}>{request.mobile}</td>
                  <td style={tdStyle}>{request.role}</td>
                  <td style={tdStyle}>{request.accountType}</td>
                  <td style={tdStyle}>
                    <span
                      style={{
                        padding: "6px 12px",
                        borderRadius: "20px",
                        background: request.isApproved ? "#dcfce7" : "#fef3c7",
                        color: request.isApproved ? "#15803d" : "#b45309",
                        fontWeight: "600",
                        fontSize: "12px",
                      }}
                    >
                      {request.status}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    {new Date(request.createdAt).toLocaleDateString()}
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
                        <Button onClick={() => handleApprove(request.id)}>
                          Approve
                        </Button>

                        <Button
                          variant="destructive-solid"
                          onClick={() => handleDecline(request.id)}
                        >
                          Decline
                        </Button>

                        <Button
                          variant="warning"
                          onClick={() => handleRequestMoreInfo(request.id)}
                        >
                          More Info
                        </Button>
                      </div>
                    )}

                    {request.isApproved && !request.isAccountCreated && (
                      <Button
                        variant="info"
                        onClick={() => handleResendInvite(request.id)}
                      >
                        Resend Invite
                      </Button>
                    )}

                    {request.isAccountCreated && (
                      <span
                        style={{
                          color: "#16a34a",
                          fontWeight: "600",
                        }}
                      >
                        ✅ Account Created
                      </span>
                    )}
                  </td>
                </tr>
              ))}

              {filteredRequests.length === 0 && (
                <tr>
                  <td colSpan={11} align="center" style={{ padding: "30px" }}>
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
