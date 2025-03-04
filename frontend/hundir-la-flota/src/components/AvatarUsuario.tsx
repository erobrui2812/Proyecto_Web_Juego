"use client";

import { LogOut } from "lucide-react";
import { useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import ModalPerfil from "@/components/ModalPerfil";

const AvatarUsuario = () => {
  const { userDetail, cerrarSesion } = useAuth();
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  const openProfileModal = () => {
    setIsProfileModalOpen(true);
  };

  const closeProfileModal = () => {
    setIsProfileModalOpen(false);
  };

  return (
    <div>
      <div className="flex items-center space-x-4">
        {userDetail ? (
          <>
            <img
              src={userDetail.avatarUrl}
              alt="User Avatar"
              className="w-12 h-12 rounded-full border-2 border-gold"
            />
            <div>
              <h1 className="text-xl font-bold text-gold">{userDetail.nickname}</h1>
              <button
                className="text-blueLink hover:underline"
                onClick={openProfileModal}
              >
                Ver Perfil
              </button>
            </div>
            <button
              className="text-redError hover:underline flex items-center space-x-2"
              onClick={cerrarSesion}
            >
              <LogOut size={20} />
              <span>Cerrar Sesión</span>
            </button>
          </>
        ) : (
          <p>Cargando...</p>
        )}
      </div>




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
