import type { Metadata } from "next";

const DESCRICAO = "Veja seus veículos e itens segurados no servidor DayZ ArkZ, com a posição sincronizada em tempo real no mapa do Chernarus.";

export const metadata: Metadata = {
  title: "Seguros",
  description: DESCRICAO,
  openGraph: {
    title: "Seguros — ArkZ",
    description: DESCRICAO,
    url: "https://arkz.dev.br/Seguros",
  },
};

export default function SegurosLayout({ children }: { children: React.ReactNode }) {
  return children;
}
