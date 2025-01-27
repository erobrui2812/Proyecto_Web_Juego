"use client"

import "@fontsource/bebas-neue";
import "@fontsource/montserrat";

import Image from "next/image";
import Link from "next/link";
import ThemeToggle from "@/components/ThemeToggle";
import { useGlobalContext } from "@/contexts/GlobalContext";
import "@/app/globals.css";

const Header = () => {
  const { auth } = useGlobalContext();

  return (
    <header className="flex flex-wrap items-center justify-between p-4 bg-primary text-white font-bebasneue">
      <div className="w-full sm:w-1/3 flex justify-center sm:justify-start mb-4 sm:mb-0">
        <Image
          src="/BattleshipsColle.webp"
          alt="Logo"
          width={200}
          height={50}
          priority={true}
          className="max-w-full h-auto"
        />
      </div>

      <nav className="w-full sm:w-1/3 flex justify-center space-x-6 text-lg tracking-[.2em] mb-4 sm:mb-0">
        <Link
          href="/"
          className="hover:text-secondary hover:tracking-[.15em] transition-all"
        >
          Inicio
        </Link>
        {/* <Link
          href="/juego"
          className="hover:text-secondary hover:tracking-[.15em] transition-all"
        >
          Juego
        </Link> */}
        <Link
          href="/menu"
          className="hover:text-secondary hover:tracking-[.15em] transition-all"
        >
          Menú
        </Link>
      </nav>

      <div className="w-full sm:w-1/3 flex justify-center sm:justify-end items-center space-x-4">
        {auth.isAuthenticated && auth.userDetail ? (
          <div className="flex items-center space-x-2">
            <img
              src={auth.userDetail.avatarUrl}
              alt="Perfil"
              width={30}
              height={30}
              className="rounded-full"
            />

            <span className="text-lg truncate max-w-[150px] sm:max-w-none">
              {auth.userDetail.nickname}
            </span>
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

export default Header;