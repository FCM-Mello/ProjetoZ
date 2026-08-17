"use client";

import { useSearchParams } from "next/navigation";
import { loginSteam } from "../../services/authApi";

export default function LoginClient() {
    const searchParams = useSearchParams();
    const banido = searchParams.get("erro") === "banido";

    return (
        <div className="center-card">
            <h2>ArkZ</h2>

            {banido && (
                <p style={{ color: "var(--color-danger-strong)", fontSize: 13 }}>
                    Essa conta está banida. Entre em contato com a administração se achar que isso é um engano.
                </p>
            )}

            <button
                className="buttonConfirm"
                onClick={loginSteam}
            >
                Entrar com Steam
            </button>
        </div>
    );
}
