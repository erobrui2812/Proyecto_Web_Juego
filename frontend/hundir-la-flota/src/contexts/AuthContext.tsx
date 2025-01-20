"use client";

import { createContext, useContext, useEffect, useState, useCallback } from "react";
import { toast } from "react-toastify";

type AuthContextType = {
  auth: { token: string | null };
  userDetail: {
    id: number;
    avatarUrl: string;
    nickname: string;
    mail: string;
    rol: string;
  } | null;
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
  const [auth, setAuth] = useState<{ token: string | null }>({ token: null });
  const [userDetail, setUserDetail] = useState<{
    id: number;
    avatarUrl: string;
    nickname: string;
    mail: string;
    rol: string;
  } | null>(null);
  const [isAuthenticated, setAuthenticated] = useState(false);
  const [rol, setRol] = useState<string>("usuario");

  useEffect(() => {
    const token =
      typeof window !== "undefined"
        ? sessionStorage.getItem("token") || localStorage.getItem("token")
        : null;
    setAuth({ token });
  }, []);

  const obtenerUserDetail = useCallback(async () => {
    if (!auth.token) return;

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

      setUserDetail((prev) => {
        return prev
          ? { ...prev, ...data }
          : {
              id: data.id,
              avatarUrl: data.avatarUrl,
              nickname: data.nickname,
              mail: data.mail,
              rol: data.rol || "usuario",
            };
      });
    } catch (error) {
      console.error("Error al obtener detalles del usuario:", error);
      setUserDetail(null);
    }
  }, [auth.token]);

  useEffect(() => {
    const decodeToken = (token: string) => {
      try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        console.log("Payload del token:", payload);
        return {
          role: payload["role"] || "usuario",
          userId: payload["id"] || 0, 
        };
      } catch (error) {
        console.error("Error al decodificar el token:", error);
        return null;
      }
    };

    if (auth.token) {
      setAuthenticated(true);

      const decoded = decodeToken(auth.token);

      if (decoded) {
        setRol(decoded.role);
        obtenerUserDetail();
        setUserDetail((prevDetail) => {
          return prevDetail
            ? {
                ...prevDetail,
                id: decoded.userId,
                rol: decoded.role,
              }
            : {
                id: decoded.userId,
                rol: decoded.role,
                avatarUrl: "",
                nickname: "",
                mail: "",
              };
        });
      }
    } else {
      setAuthenticated(false);
      setUserDetail(null);
      setRol("usuario");
    }
  }, [auth.token, obtenerUserDetail]);

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
        throw new Error("Credenciales incorrectas o error en el servidor.");
      }

      const { token } = await response.json();
      setAuth({ token });

      if (mantenerSesion) {
        localStorage.setItem("token", token);
      } else {
        sessionStorage.setItem("token", token);
      }
    } catch (error: any) {
      console.error("Error al iniciar sesión:", error.message);
      toast.error(error.message || "Error desconocido");
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
        throw new Error("Error al registrar usuario.");
      }

      toast.success("Usuario registrado correctamente");
    } catch (error) {
      console.error("Error al registrar usuario:", error);
      toast.error("Error al registrar usuario.");
    }
  };

  const cerrarSesion = () => {
    setAuth({ token: null });
    sessionStorage.removeItem("token");
    localStorage.removeItem("token");
    setAuthenticated(false);
    setUserDetail(null);
    setRol("usuario");
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