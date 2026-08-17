import { Suspense } from "react";
import LoginClient from "./LoginClient";

export default function Page() {
    return (
        <Suspense fallback={<p>Carregando...</p>}>
            <LoginClient />
        </Suspense>
    );
}
