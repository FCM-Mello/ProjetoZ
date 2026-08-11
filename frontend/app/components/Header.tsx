import "./Header.css"
import { usePathname } from "next/navigation";
import { useAuth } from "../contexts/AuthContext";

export default function Header() {
    const { user, loading } = useAuth();
    const pathname = usePathname();

    function linkClass(href: string) {
        return pathname === href ? "active" : "";
    }

    return (
        <header className="header">
            <div className="containerHeader">
                <div className="logo"></div>

                <nav className="navHeader navMain">
                    <a href="/Home" className={linkClass("/Home")}>LOJA</a>
                    <a href="/Inventario" className={linkClass("/Inventario")}>INVENTARIO</a>
                    <a href="/Az" className={linkClass("/Az")}>AZ</a>
                    <a href="/Historico" className={linkClass("/Historico")}>HISTÓRICO</a>
                    <a href="/Sorteios" className={linkClass("/Sorteios")}>SORTEIOS</a>
                </nav>

            {!loading && !user && (
                 <nav className="navHeader">
                    <a href="/api/auth/steam/login">LOGIN</a>
                </nav>
            )}

            {user && (
                <div className="navUser">
                    <a href="/Az" className="coinBadge" title="Comprar Az Coins">
                        🪙 {user.coins}
                    </a>

                    <a href={user.profile.profileUrl} className="userLink">
                        <img className="profile"
                            src={user.profile.avatar}
                        />
                        <span className="userName">
                            {user.profile.name}
                        </span>
                    </a>
                </div>
            )}

            </div>
        </header>
    );
}
