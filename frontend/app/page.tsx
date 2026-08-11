import Link from "next/link";

export default function Home() {
  return (
    <div className="center-card">
      <h2>ArkZ</h2>
      <p>
        Bem-vindo à loja.
      </p>
      <Link href="/Home" className="buttonConfirm">Confirmar</Link>
    </div>
  );
}