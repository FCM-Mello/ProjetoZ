"use client";

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
