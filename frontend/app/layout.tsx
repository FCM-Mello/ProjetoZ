import type { Metadata } from "next";
import { Rajdhani } from "next/font/google";
import "./globals.css";
import { AuthProvider } from "./contexts/AuthContext";
import Header from "./components/Header";
import Footer from "./components/Footer";

const rajdhani = Rajdhani({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-game",
});

const DESCRICAO = "ArkZ é a loja oficial de um servidor privado de DayZ: compre Az Coins, itens exclusivos e planos VIP, participe de sorteios e do ranking semanal de clipes.";

export const metadata: Metadata = {
  title: {
    default: "ArkZ",
    template: "%s — ArkZ",
  },
  description: DESCRICAO,
  applicationName: "ArkZ",
  verification: {
    google: "ViPt4D0ncZAuhp95aIxApbs-l8S-xUEei_twKrFklx4",
  },
  openGraph: {
    siteName: "ArkZ",
    title: "ArkZ",
    description: DESCRICAO,
    url: "https://arkz.dev.br",
    images: ["/Images/Logo.png"],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${rajdhani.variable} antialiased`}>
        <AuthProvider>
          <main className="background">
            <Header />
              <section className="content">
                {children}
              </section>
            <Footer />
          </main>
        </AuthProvider>
      </body>
    </html>
  );
}
