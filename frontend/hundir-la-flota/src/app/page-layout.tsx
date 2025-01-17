"use client";

import "@fontsource/bebas-neue";
import "@fontsource/montserrat";

import Image from "next/image";
import Link from "next/link";
import ThemeToggle from "./components/ThemeToggle";
import { useGlobalContext } from "./contexts/GlobalContext";
import "./globals.css";

const Header = () => {
  const { auth } = useGlobalContext();

  return (
    <header className="flex items-center justify-between p-4 bg-primary text-white font-bebasneue">
      <div className="flex-shrink-0 w-1/3 flex justify-center">
        <Image src="/BattleshipsColle.webp" alt="Logo" width={300} height={1} />
      </div>

      <nav className="w-1/3 flex justify-center space-x-6 text-lg tracking-[.2em]">
        <Link href="/" className="hover:text-secondary hover:tracking-[.15em]">
          Inicio
        </Link>
        <Link
          href="/juego"
          className="hover:text-secondary hover:tracking-[.15em]"
        >
          Juego
        </Link>
        <Link
          href="/menu"
          className="hover:text-secondary hover:tracking-[.15em]"
        >
          Menú
        </Link>
      </nav>

      <div className="w-1/3 flex justify-center items-center space-x-4">
        {auth.isAuthenticated ? (
          <div className="flex items-center space-x-2">
            <Image
              src={auth.userDetail?.avatarUrl || "/user-no-photo.svg"}
              alt="Perfil"
              width={30}
              height={30}
              className="rounded-full"
            />

            <span className="text-lg">{auth.userDetail?.nickname}</span>
          </div>
        ) : (
          <a href="/login">
            <button className="bg-primary px-4 py-2 rounded border shadow hover:scale-105 hover:shadow-lg transition-all duration-200">
              Iniciar Sesión
            </button>
          </a>
        )}
        <ThemeToggle />
      </div>
    </header>
  );
};

const Layout = ({ children }: { children: React.ReactNode }) => {
  return (
    <div className="bg-background text-foreground min-h-screen flex flex-col transition-all duration-300">
      <Header />
      <main className="flex-1">{children}</main>
    </div>
  );
};

export default Layout;
