import Link from "next/link";
import "./Footer.css";

export default function Footer() {
    return (
        <footer className="footer">
            <span>© {new Date().getFullYear()} ArkZ</span>
            <Link href="/Privacidade" className="footerLink">Política de Privacidade</Link>
            <Link href="/Termos" className="footerLink">Termos de Serviço</Link>
        </footer>
    );
}
