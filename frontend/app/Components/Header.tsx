import "./Css/Header.css"
import { useAuth } from "../contexts/AuthContext";

export default function Header() {
    const { user, loading } = useAuth();
    
    return (
        <header className="header">
            <div className="containerHeader">
                <div className="logo"></div>

                <nav className="navHeader">
                    <a href="/">LOJA</a>
                    <a href="#">INVENTARIO</a>
                    <a href="#">Az</a>
                </nav>

            {!user && (
                 <nav className="navHeader">
                    <a href="/api/auth/steam/login">LOGIN</a>
                </nav>
            )}

            {user && (
                <a href={user.profile.profileUrl}>
                <nav className="navHeader noneBorder">
                    <img className="profile"
                        src={user.profile.avatar}
                    />
                    <span>
                        {user.profile.name}
                    </span>
                </nav>
                </a>
            )}

            </div>
        </header>
    );
}