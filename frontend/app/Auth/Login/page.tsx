"use client";

import { loginSteam } from "../../services/authApi";

export default function Login() {

    return (
        <div className="center-card">
            <h2>ArkZ</h2>

            <button
                className="buttonConfirm"
                onClick={loginSteam}
            >
                Entrar com Steam
            </button>
        </div>
    );
}