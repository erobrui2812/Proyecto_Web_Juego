"use client"
import ThemeToggle from "./components/ThemeToggle";
import { useState } from "react";
import Link from "next/link";
import Image from "next/image";
import "./globals.css";

const Header = () => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  return (
    <header className="flex items-center justify-between p-4 bg-primary text-white">
      {/* Imagen en el lateral */}
      <div className="flex-shrink-0">
        <Image
          src="/path-to-your-logo.png" 
          alt="Logo"
          width={50} 
          height={50}
        />
      </div>

      {/* Enlaces de navegación */}
      <nav className="flex space-x-4">
        
        <Link href="/" className="hover:text-secondary">Inicio</Link>
        <Link href="/juego" className="hover:text-secondary">Juego</Link>
        <Link href="/amigos" className="hover:text-secondary">Amigos</Link>
      </nav>

      {/* Caja de perfil o botón de iniciar sesión */}
      <div className="flex items-center space-x-4">
        {isAuthenticated ? (
          <div className="flex items-center space-x-2">
            <Image
              src="https://s3.amazonaws.com/comicgeeks/characters/avatars/23353.jpg" 
              alt="Perfil"
              width={30}
              height={30}
              className="rounded-full"
            />
            <span>Zeta</span>
          </div>
        ) : (
          <button
            onClick={() => setIsAuthenticated(true)} 
            className="bg-secondary px-4 py-2 rounded"
          >
            Iniciar Sesión
          </button>
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
      {/* <footer className="p-4 bg-secondary text-white">
        <p>Pie de página</p>
      </footer> */}
    </div>
  );
};

export default Layout;