"use client";

import { createContext, useCallback, useContext, useRef, useState } from "react";
import "./ToastContext.css";

type TipoToast = "sucesso" | "erro";

interface ToastItem {
    id: number;
    tipo: TipoToast;
    mensagem: string;
}

interface ToastContextType {
    sucesso: (mensagem: string) => void;
    erro: (mensagem: string) => void;
}

const ToastContext = createContext<ToastContextType>({
    sucesso: () => {},
    erro: () => {},
});

const DURACAO_MS = 4500;

export function ToastProvider({ children }: { children: React.ReactNode }) {
    const [toasts, setToasts] = useState<ToastItem[]>([]);
    const proximoId = useRef(0);

    const remover = useCallback((id: number) => {
        setToasts(atual => atual.filter(t => t.id !== id));
    }, []);

    const mostrar = useCallback((tipo: TipoToast, mensagem: string) => {
        const id = proximoId.current++;
        setToasts(atual => [...atual, { id, tipo, mensagem }]);
        setTimeout(() => remover(id), DURACAO_MS);
    }, [remover]);

    const sucesso = useCallback((mensagem: string) => mostrar("sucesso", mensagem), [mostrar]);
    const erro = useCallback((mensagem: string) => mostrar("erro", mensagem), [mostrar]);

    return (
        <ToastContext.Provider value={{ sucesso, erro }}>
            {children}

            <div className="toastStack">
                {toasts.map(t => (
                    <div key={t.id} className={`toastItem toastItem-${t.tipo}`} role="status">
                        <span className="toastIcone">{t.tipo === "sucesso" ? "✓" : "!"}</span>
                        <span>{t.mensagem}</span>
                        <button
                            type="button"
                            className="toastFechar"
                            aria-label="Fechar"
                            onClick={() => remover(t.id)}
                        >
                            ×
                        </button>
                    </div>
                ))}
            </div>
        </ToastContext.Provider>
    );
}

export function useToast() {
    return useContext(ToastContext);
}
