import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { AUTH_API_BASE_URL } from "../../config/api";

interface ValidateInviteResponse {
  success: boolean;
  message?: string;
  accountType?: string;
}

function SetupAccountPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  useEffect(() => {
    if (!token) {
      return;
    }

    let active = true;

    const validateInvite = async () => {
      try {
        const response = await fetch(`${AUTH_API_BASE_URL}/validate-invite`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ token }),
        });

        const data = (await response.json()) as ValidateInviteResponse;

        if (!active) {
          return;
        }

        console.log("VALIDATE RESPONSE", data);

        if (!data.success) {
          alert(data.message || "Invalid invite.");
          navigate("/login");
          return;
        }

        if (data.accountType === "Single") {
          navigate(`/register/single?token=${token}`);
          return;
        }

        navigate(`/register/multi?token=${token}`);
      } catch (error) {
        if (!active) {
          return;
        }
        console.error(error);
        alert("Unable to validate invite.");
      }
    };

    void validateInvite();

    return () => {
      active = false;
    };
  }, [token, navigate]);

  return (
    <div
      style={{
        padding: "50px",
        textAlign: "center",
      }}
    >
      <h2>Validating Invitation...</h2>
      <p>Please wait...</p>
    </div>
  );
}

export default SetupAccountPage;
