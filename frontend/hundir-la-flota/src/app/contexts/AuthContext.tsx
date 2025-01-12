"use client";
import { createContext, useContext, useState } from "react";
import { toast } from "react-toastify";

type AuthContextType = {
  auth: { token: string | null };
  userDetail: { avatarUrl: string; nickname: string; mail: string } | null;
  iniciarSesion: (
    nicknameMail: string,
    password: string,
    mantenerSesion: boolean
  ) => Promise<void>;
  registrarUsuario: (
    nickname: string,
    email: string,
    password: string,
    confirmPassword: string,
    avatarUrl: string
  ) => Promise<void>;
  cerrarSesion: () => void;
  obtenerUserDetail: () => Promise<void>;
  setAuthenticated: React.Dispatch<React.SetStateAction<boolean>>;
  isAuthenticated: boolean;
  rol: string;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [auth, setAuth] = useState<{ token: string | null }>({
    token:
      sessionStorage.getItem("token") || localStorage.getItem("token") || null,
  });

  const [userDetail, setUserDetail] = useState<{
    avatarUrl: string;
    nickname: string;
    mail: string;
  } | null>(null);

  const [isAuthenticated, setAuthenticated] = useState(false);

  const [rol, setRol] = useState<string>(() => {
    try {
      const payload = JSON.parse(atob(auth?.token?.split(".")[1] || ""));
      return (
        payload[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ] || "usuario"
      );
    } catch (error) {
      if (!auth.token) return "usuario";
      console.error("Error al decodificar el token:", error);
      return "usuario";
    }
  });

  const iniciarSesion = async (
    nicknameMail: string,
    password: string,
    mantenerSesion: boolean
  ) => {
    try {
      const response = await fetch(`https://localhost:7162/api/Users/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ nicknameMail, password }),
      });

      if (!response.ok) {
        toast.error("Credenciales incorrectas o error de servidor");
      } else {
        toast.success("Se ha autenticado de forma exitosa.");
      }

      const { token } = await response.json();

      if (token) {
        setAuth({ token });
        const decoded = JSON.parse(atob(token.split(".")[1]));
        const roleDecoded =
          decoded[
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
          ];
        setRol(roleDecoded);

        if (mantenerSesion) {
          localStorage.setItem("token", token);
        } else {
          sessionStorage.setItem("token", token);
        }

        await obtenerUserDetail();
      } else {
        throw new Error("Token no recibido del servidor");
      }
    } catch (error: any) {
      console.error("Error al iniciar sesión:", error);
      throw error;
    }
  };

  const registrarUsuario = async (
    nickname: string,
    email: string,
    password: string,
    confirmPassword: string,
    avatarUrl: string
  ) => {
    try {
      const response = await fetch(
        `https://localhost:7162/api/Users/register`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            nickname,
            email,
            password,
            confirmPassword,
            avatarUrl,
          }),
        }
      );

      if (!response.ok) {
        throw new Error("Error al registrar usuario");
      }

      toast.success("Usuario registrado correctamente");
    } catch (error) {
      toast.error(`Error al registrar usuario:, ${error}`);
      throw error;
    }
  };

  const obtenerUserDetail = async () => {
    try {
      const response = await fetch(`https://localhost:7162/api/Users/detail`, {
        method: "GET",
        headers: {
          Authorization: `Bearer ${auth.token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Error al obtener los detalles del usuario");
      }

      const data = await response.json();
      setUserDetail(data);
    } catch (error) {
      toast.error(`Error al obtener detalles del usuario: ${error}`);
      setUserDetail(null);
      throw error;
    }
  };

  const cerrarSesion = () => {
    setAuth({ token: null });
    setUserDetail(null);
    sessionStorage.removeItem("token");
    localStorage.removeItem("token");
  };

  return (
    <AuthContext.Provider
      value={{
        auth,
        userDetail,
        iniciarSesion,
        registrarUsuario,
        cerrarSesion,
        obtenerUserDetail,
        setAuthenticated,
        isAuthenticated,
        rol,
      }}
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
