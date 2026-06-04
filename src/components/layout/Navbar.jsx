import "../../assets/css/navbar.css";
import { Link, useNavigate } from "react-router-dom";
import Logo from "../../assets/images/Logo.png";

function Navbar() {
  const navigate = useNavigate();

  return (
    <nav className="navbar">

      <Link to="/" className="Logo">
       <img
  src={Logo}
  alt="Tummly"
  style={{
    height: "clamp(30px, 5vw, 50px)",
    width: "auto",
    maxWidth: "100%",
    objectFit: "contain",
    cursor: "pointer",
    display: "block",
  }}
/>
      </Link>

      <div className="nav-right">

        <button
          className="trial-btn"
          onClick={() => navigate("/request-trial")}
        >
          Request trial
        </button>

        <Link
          to="/login"
          className="login-btn"
          style={{
            textDecoration: "none",
          }}
        >
          Login
        </Link>

      </div>

    </nav>
  );
}

export default Navbar;