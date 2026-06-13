import "../../assets/css/navbar.css";
import { Link, useNavigate } from "react-router-dom";
import Logo from "../../assets/images/Logo.png";
import { Button } from "@/components/ui/button";

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
        <Button onClick={() => navigate("/request-trial")}>
          Request trial
        </Button>

        <Button variant="secondary" asChild>
          <Link to="/login">Login</Link>
        </Button>
      </div>
    </nav>
  );
}

export default Navbar;
