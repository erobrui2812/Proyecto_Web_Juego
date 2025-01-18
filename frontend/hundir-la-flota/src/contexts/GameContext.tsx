"use client";
import { createContext, useContext, useEffect, useState } from "react";

type GameContextType = {
  Game: { token: string | null };
  iniciarSesion: (
    identificador: string,
    password: string,
    mantenerSesion: boolean
  ) => Promise<void>;
  cerrarSesion: () => void;
  setAuthenticated: React.Dispatch<React.SetStateAction<boolean>>;
  isAuthenticated: boolean;
  rol: string;
};

const GameContext = createContext<GameContextType | undefined>(undefined);

export const GameProvider = ({ children }: { children: React.ReactNode }) => {
  const [Game, setAuth] = useState<{ token: string | null }>({ token: null });
  const [isAuthenticated, setAuthenticated] = useState(false);
  const [rol, setRol] = useState<string>("usuario");

  // Asegurar que accedemos a sessionStorage y localStorage solo en el cliente
  useEffect(() => {
    if (typeof window !== "undefined") {
      const token =
        sessionStorage.getItem("token") || localStorage.getItem("token");
      setAuth({ token });

      if (token) {
        try {
          const payload = JSON.parse(atob(token.split(".")[1]));
          const roleDecoded =
            payload[
              "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ];
          setRol(roleDecoded || "usuario");
          setAuthenticated(true);
        } catch (error) {
          console.error("Error al decodificar el token:", error);
        }
      }
    }
  }, []);

  const iniciarSesion = async (
    identificador: string,
    password: string,
    mantenerSesion: boolean
  ) => {
    try {
      const response = await fetch(`/api/Auth/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ identificador, password }),
      });

      if (!response.ok) {
        throw new Error("Credenciales incorrectas o error de servidor");
      }

      const { token } = await response.json();

      if (token) {
        setAuth({ token });

        if (mantenerSesion) {
          localStorage.setItem("token", token);
        } else {
          sessionStorage.setItem("token", token);
        }

        try {
          const decoded = JSON.parse(atob(token.split(".")[1]));
          const roleDecoded =
            decoded[
              "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            ];
          setRol(roleDecoded || "usuario");
          setAuthenticated(true);
        } catch (error) {
          console.error("Error al decodificar el token:", error);
        }
      } else {
        throw new Error("Token no recibido del servidor");
      }
    } catch (error) {
      console.error("Error al iniciar sesión:", error);
      throw error;
    }
  };

  const cerrarSesion = () => {
    setAuth({ token: null });
    if (typeof window !== "undefined") {
      sessionStorage.removeItem("token");
      localStorage.removeItem("token");
    }
    setAuthenticated(false);
  };

  return (
    <GameContext.Provider
      value={{
        Game,
        iniciarSesion,
        cerrarSesion,
        setAuthenticated,
        isAuthenticated,
        rol,
      }}
    >
      {children}
    </GameContext.Provider>
  );
};

export const useGame = (): GameContextType => {
  const context = useContext(GameContext);
  if (!context) {
    throw new Error("useGame must be used within a GameProvider");
  }
  return context;
};
