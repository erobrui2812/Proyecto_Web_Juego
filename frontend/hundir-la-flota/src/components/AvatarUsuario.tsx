"use client";

import { LogOut } from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";

const AvatarUsuario = () => {
  const { userDetail, cerrarSesion } = useAuth();

  return (
    <div className="flex items-center space-x-4">
      <img
        src={userDetail?.avatarUrl}
        alt="User Avatar"
        className="w-12 h-12 rounded-full border-2 border-gold"
      />
      <div>
        <h1 className="text-xl font-bold text-gold">{userDetail?.nickname}</h1>
        <button
          className="text-blue-400 hover:underline"
          onClick={() => alert("Ir al perfil")}
        >
          Ver Perfil
        </button>
      </div>
      <button
        className="text-red-500 hover:text-red-700 flex items-center space-x-2"
        onClick={cerrarSesion}
      >
        <LogOut size={20} />
        <span>Cerrar Sesión</span>
      </button>
    </div>
  );
};

export default AvatarUsuario;
