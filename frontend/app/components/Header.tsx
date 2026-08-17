"use client";

import "./Header.css"
import Link from "next/link";
import { useRef, useState } from "react";
import { usePathname } from "next/navigation";
import { useAuth } from "../contexts/AuthContext";
import { useClickOutside } from "../hooks/useClickOutside";
import NotificationBell from "./NotificationBell";

export default function Header() {
    const { user, loading } = useAuth();
    const pathname = usePathname();

    const [menuAberto, setMenuAberto] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);
    useClickOutside(menuRef, () => setMenuAberto(false));

    function linkClass(href: string) {
        return pathname === href ? "active" : "";
    }

    return (
        <header className="header">
            <div className="containerHeader">
                <div className="logo"></div>

                <nav className="navHeader navMain">
                    <Link href="/Home" className={linkClass("/Home")}>LOJA</Link>
                    <Link href="/Az" className={linkClass("/Az")}>AZ</Link>
                    <Link href="/Vip" className={linkClass("/Vip")}>VIP</Link>
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

                    <NotificationBell />

                    <div className="perfilMenu" ref={menuRef}>
                        <button className="userLink userLinkBotao" onClick={() => setMenuAberto(a => !a)}>
                            <img className="profile"
                                src={user.profile.avatar}
                            />
                            <span className="userName">
                                {user.profile.name}
                            </span>
                        </button>

                        {menuAberto && (
                            <div className="perfilDropdown">
                                <Link href="/Inventario" onClick={() => setMenuAberto(false)}>Inventário</Link>
                                <Link href="/Historico" onClick={() => setMenuAberto(false)}>Histórico</Link>
                                <Link href="/Seguros" onClick={() => setMenuAberto(false)}>Seguros</Link>
                                <a href={user.profile.profileUrl} target="_blank" rel="noreferrer">Perfil Steam</a>
                            </div>
                        )}
                    </div>
                </div>
            )}

            </div>
        </header>
    );
}
