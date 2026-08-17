"use client";

import { useEffect, useRef } from "react";

// Adiciona "in-view" a cada item de uma lista/grid assim que ele entra na
// viewport, junto com a classe "reveal" (definida em globals.css). Um único
// observer cuida de todos os itens do container, em vez de um por elemento.
export function useScrollReveal<T extends HTMLElement>(dependencia: unknown) {
    const containerRef = useRef<T>(null);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const itens = container.querySelectorAll(".reveal");

        if (typeof IntersectionObserver === "undefined") {
            itens.forEach(item => item.classList.add("in-view"));
            return;
        }

        const observer = new IntersectionObserver(
            entries => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("in-view");
                        observer.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.15 }
        );

        itens.forEach(item => observer.observe(item));

        return () => observer.disconnect();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [dependencia]);

    return containerRef;
}
