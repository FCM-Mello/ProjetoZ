import Link from "next/link";
import "./EstadoVazio.css";

interface Props {
    icone: string;
    titulo: string;
    descricao?: string;
    acaoLabel?: string;
    acaoHref?: string;
    compacto?: boolean;
}

export default function EstadoVazio({ icone, titulo, descricao, acaoLabel, acaoHref, compacto }: Props) {
    return (
        <div className={`estadoVazio ${compacto ? "estadoVazio-compacto" : ""}`}>
            <span className="estadoVazioIcone" aria-hidden="true">{icone}</span>
            <p className="estadoVazioTitulo">{titulo}</p>
            {descricao && <p className="estadoVazioDescricao">{descricao}</p>}
            {acaoLabel && acaoHref && (
                <Link href={acaoHref} className="estadoVazioAcao">{acaoLabel}</Link>
            )}
        </div>
    );
}
