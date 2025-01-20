"use client";

import { LogOut } from "lucide-react";
import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import ModalPerfil from "@/components/ModalPerfil";

const AvatarUsuario = () => {
  const { userDetail, cerrarSesion } = useAuth();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  const openProfileModal = () => {
    console.log("Abriendo modal de perfil...");
    setIsProfileModalOpen(true);
  };

  const closeProfileModal = () => {
    console.log("Cerrando modal de perfil...");
    setIsProfileModalOpen(false);
  };

  return (
    <div className="flex items-center">
      <img
        src={userDetail?.avatarUrl || "https://via.placeholder.com/150"}
        alt="User Avatar"
        className="w-12 h-12 rounded-full border-2 border-gold"
      />
      <div>
        <h1 className="text-xl font-bold text-gold">{userDetail?.nickname}</h1>
        <button
          className="text-blue-400 hover:underline"
          onClick={openProfileModal}
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

      
      {userDetail && (
        <ModalPerfil
          isOpen={isProfileModalOpen}
          onClose={closeProfileModal}
          userId={String(userDetail.id)}
        />
      )}
    </div>
  );
};

export default AvatarUsuario;
