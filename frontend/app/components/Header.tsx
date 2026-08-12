import "./Header.css"
import Link from "next/link";
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
                    <Link href="/Home" className={linkClass("/Home")}>LOJA</Link>
                    <Link href="/Inventario" className={linkClass("/Inventario")}>INVENTARIO</Link>
                    <Link href="/Az" className={linkClass("/Az")}>AZ</Link>
                    <Link href="/Vip" className={linkClass("/Vip")}>VIP</Link>
                    <Link href="/Historico" className={linkClass("/Historico")}>HISTÓRICO</Link>
                    <Link href="/Sorteios" className={linkClass("/Sorteios")}>SORTEIOS</Link>
                    <Link href="/Clipes" className={linkClass("/Clipes")}>CLIPES</Link>
                    {user?.isAdmin && (
                        <Link href="/Admin" className={linkClass("/Admin")}>ADMIN</Link>
                    )}
                </nav>

            {!loading && !user && (
                 <nav className="navHeader">
                    <a href="/api/auth/steam/login">LOGIN</a>
                </nav>
            )}

            {user && (
                <div className="navUser">
                    <Link href="/Az" className="coinBadge" title="Comprar Az Coins">
                        🪙 {user.coins}
                    </Link>

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
