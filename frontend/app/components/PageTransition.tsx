"use client";

import { usePathname } from "next/navigation";
import "./PageTransition.css";

// Troca de key força o React a remontar o container a cada navegação, o que
// dispara a animação de entrada do zero — não é um crossfade de verdade
// (isso exigiria orquestrar a saída da página antiga, o tipo de coisa que
// normalmente vem de uma lib como Framer Motion, que este projeto não usa),
// mas já tira o "corte seco" entre páginas sem adicionar dependência nova.
export default function PageTransition({ children }: { children: React.ReactNode }) {
    const pathname = usePathname();

    return (
        <div key={pathname} className="pageTransition">
            {children}
        </div>
    );
}
