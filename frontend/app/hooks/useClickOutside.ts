"use client";

import { useEffect, RefObject } from "react";

// Fecha um dropdown/menu ao clicar fora dele — usado pelo dropdown de
// perfil e pelo sino de notificações no Header.
export function useClickOutside(ref: RefObject<HTMLElement | null>, aoClicarFora: () => void) {
    useEffect(() => {
        function handler(event: MouseEvent) {
            if (ref.current && !ref.current.contains(event.target as Node)) {
                aoClicarFora();
            }
        }

        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, [ref, aoClicarFora]);
}
