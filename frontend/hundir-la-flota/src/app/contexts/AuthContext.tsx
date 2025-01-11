import { createContext, useContext, useState } from "react";

type AuthContextType = {
  auth: { token: string | null };
  iniciarSesion: (identificador: string, password: string, mantenerSesion: boolean) => Promise<void>;
  cerrarSesion: () => void;
  setAuthenticated: React.Dispatch<React.SetStateAction<boolean>>;
  isAuthenticated: boolean;
  rol: string;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  
  const [auth, setAuth] = useState<{ token: string | null }>({
    token: sessionStorage.getItem("token") || localStorage.getItem("token") || null,
  });
  const [isAuthenticated, setAuthenticated] = useState(false);
  const [rol, setRol] = useState<string>(() => {
    try {
      const payload = JSON.parse(atob(auth?.token?.split(".")[1] || ""));
      return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "usuario";
    } catch (error) {
      if (!auth.token) return "usuario";
      console.error("Error al decodificar el token:", error);
      return "usuario";
    }
  });

  const iniciarSesion = async (identificador: string, password: string, mantenerSesion: boolean) => {
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
        const decoded = JSON.parse(atob(token.split(".")[1]));
        const roleDecoded = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
        setRol(roleDecoded);

        if (mantenerSesion) {
          localStorage.setItem("token", token);
        } else {
          sessionStorage.setItem("token", token);
        }

        try {
          const responseCarrito = await fetch(`/api/Carrito/verificar-o-crear`, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${token}`,
            },
          });

          if (!responseCarrito.ok) {
            throw new Error("Error al verificar o crear el carrito.");
          }
          // console.log("Carrito verificado o creado correctamente.");
        } catch (error) {
          console.error("Error al verificar o crear el carrito:", error);
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
    sessionStorage.removeItem("token");
    localStorage.removeItem("token");
  };

  return (
    <AuthContext.Provider
      value={{ auth, iniciarSesion, cerrarSesion, setAuthenticated, isAuthenticated, rol }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
