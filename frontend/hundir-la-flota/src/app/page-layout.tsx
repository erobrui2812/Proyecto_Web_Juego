"use client"
import ThemeToggle from "./components/ThemeToggle";
import Link from "next/link";
import Image from "next/image";
import "./globals.css";
import { useGlobalContext } from "./contexts/GlobalContext";

const Header = () => {
  const { auth } = useGlobalContext();

  return (
    <header className="flex items-center justify-between p-4 bg-primary text-white">
      <div className="flex-shrink-0 w-1/3 flex justify-center">
        <Image
          src="/BattleshipsColle.webp"
          alt="Logo"
          width={300}
          height={1}
        />
      </div>

      <nav className="w-1/3 flex justify-center space-x-6 text-lg">
        <Link href="/" className="hover:text-secondary">Inicio</Link>
        <Link href="/juego" className="hover:text-secondary">Juego</Link>
        <Link href="/menu" className="hover:text-secondary">Menú</Link>
      </nav>

      <div className="w-1/3 flex justify-center items-center space-x-4">
        {auth.isAuthenticated ? (
          <div className="flex items-center space-x-2">
            <Image
              src={auth.userDetail?.avatarUrl || '/user-no-photo.svg'}
              alt="Perfil"
              width={30}
              height={30}
              className="rounded-full"
            />

            <button onClick={auth.cerrarSesion} type="button">
            Cerrar sesión
            </button>

            <span className="text-lg">{auth.userDetail?.nickname}</span>
          </div>
        ) : (
          <a href="/login">
            <button
              className="bg-secondary px-4 py-2 rounded"
            >
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
    <div className="bg-background text-foreground min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">
        {children}
      </main>

    </div>
  );
};

export default Layout;