"use client";

import "./globals.css";
import { AuthProvider } from "./contexts/AuthContext";
import Header from "./Components/Header";
import Footer from "./Components/Footer";

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={'antialiased'}>
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
