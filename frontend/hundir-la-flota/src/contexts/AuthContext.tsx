"use client";
import React from "react";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
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
    avatar: File | null
  ) => Promise<void>;
  cerrarSesion: () => void;
  obtenerUserDetail: () => Promise<void>;
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
  const [rol, setRol] = useState("");

  useEffect(() => {
    const token =
      typeof window !== "undefined"
        ? sessionStorage.getItem("token") || localStorage.getItem("token")
        : null;
    setAuth({ token });
  }, []);

  const obtenerUserDetail = useCallback(async () => {
    if (!auth.token) {
      return;
    }
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

      const result = await response.json();
      const data = result.data;

      console.log("Datos obtenidos de la API:", data);

      if (!data.id || !data.nickname || !data.avatarUrl) {
        throw new Error("Datos incompletos del usuario");
      }

      setRol(data.role);

      setUserDetail({
        id: data.id,
        avatarUrl: data.avatarUrl,
        nickname: data.nickname,
        mail: data.email,
        rol: data.role,
      });
      
    } catch (error: any) {
      console.error("Error obteniendo detalles del usuario:", error);
      toast.error(
        error.message || "Error al obtener los detalles del usuario."
      );
      setUserDetail(null);
    }
  }, [auth.token]);

  useEffect(() => {
    const decodeToken = (token: string) => {
      try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        return {
          userId: payload["id"] || 0,
        };
      } catch {
        return null;
      }
    };

    if (auth.token) {
      const decoded = decodeToken(auth.token);
      if (decoded) {
        setAuthenticated(true);
        obtenerUserDetail();
      } else {
        toast.error(
          "El token es inválido o ha expirado. Por favor, inicia sesión nuevamente."
        );
        cerrarSesion();
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
      setAuthenticated(true);
      await obtenerUserDetail();
    } catch (error: any) {
      toast.error(error.message || "Error desconocido");
      throw error;
    }
  };

  const registrarUsuario = async (
    nickname: string,
    email: string,
    password: string,
    confirmPassword: string,
    avatar: File | null
  ) => {
    try {
      const formData = new FormData();
      formData.append("nickname", nickname);
      formData.append("email", email);
      formData.append("password", password);
      formData.append("confirmPassword", confirmPassword);
      if (avatar) {
        formData.append("avatar", avatar);
      }
      const response = await fetch(
        "https://localhost:7162/api/Users/register",
        {
          method: "POST",
          body: formData,
        }
      );
      if (!response.ok) {
        throw new Error("Error al registrar usuario.");
      }
      toast.success("Usuario registrado correctamente");
      await obtenerUserDetail();
    } catch (error) {
      console.error("Error registrando usuario:", error);
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
    throw new Error("useAuth debe usarse dentro de un AuthProvider");
  }
  return context;
};
