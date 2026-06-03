import "../../assets/css/navbar.css";
import { Link, useNavigate } from "react-router-dom";
import Logo from "../../assets/images/Logo.png";

function Navbar() {
  const navigate = useNavigate();

  return (
    <nav className="navbar">

      <Link to="/" className="logo">
        <img
          src={logo}
          alt="Tummly"
          style={{
            height: "50px",
            transform: "translateY(8px)",
            cursor: "pointer",
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