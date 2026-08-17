"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useRequireAdmin } from "../hooks/useRequireAdmin";
import "./page.css";

const ABAS = [
    { href: "/Admin", nome: "Usuários" },
    { href: "/Admin/Notificacoes", nome: "Notificações" },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
    useRequireAdmin();

    const pathname = usePathname();

    return (
        <div className="containerAdmin">
            <h2 className="section-title">Administração</h2>

            <nav className="adminTabs">
                {ABAS.map(aba => (
                    <Link
                        key={aba.href}
                        href={aba.href}
                        className={`adminTab ${pathname === aba.href ? "adminTab-ativa" : ""}`}
                    >
                        {aba.nome}
                    </Link>
                ))}
            </nav>

            {children}
        </div>
    );
}
