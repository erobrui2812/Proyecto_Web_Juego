"use client";

import Modal from "@/components/Modal";
import { useFriendship } from "@/contexts/FriendshipContext";
import { useState } from "react";
import ModalPerfil from "@/components/ModalPerfil";

interface ModalBusquedaProps {
  isOpen: boolean;
  onClose: () => void;
  searchResults: { id: string; nickname: string; urlAvatar: string }[];
}

const ModalBusqueda: React.FC<ModalBusquedaProps> = ({
  isOpen,
  onClose,
  searchResults,
}) => {
  const { sendFriendRequest } = useFriendship();
  const [id, setId] = useState("");
  const handleSendRequest = async (nickname: string, e: React.MouseEvent) => {
    e.stopPropagation(); 
    try {
      await sendFriendRequest(nickname);
    } catch (error) {
      console.error("Error al enviar solicitud de amistad:", error);
    }
  };

  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

  const openProfileModal = () => {
    setIsProfileModalOpen(true);
  };

  const handleIdPerson = (id: string) => {
    setId(id);
    openProfileModal();
  };

  const closeProfileModal = () => {
    setIsProfileModalOpen(false);
  };

  return (
    <Modal title="Buscar Usuarios" isOpen={isOpen} onClose={onClose}>
      <div className="flex flex-col gap-4">
        {searchResults.length > 0 ? (
          searchResults.map((user) => (
            <div
              key={user.id}
              className="flex items-center justify-between p-4 bg-gray-700 rounded-md"
            >
              <div className="flex items-center space-x-4">
                <img
                  src={user.urlAvatar}
                  alt={`${user.nickname}'s Avatar`}
                  className="w-8 h-8 rounded-full"
                />
                <p className="font-semibold text-gold">{user.nickname}</p>
              </div>
              <button
                  onClick={() => handleIdPerson(user.id)}
                  className="text-blueLink font-bold hover:underline"
                >
                  Ver perfil
                </button>
              <button
                onClick={(e) => handleSendRequest(user.nickname, e)}
                className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
              >
                Enviar Solicitud
              </button>
            </div>
          ))
        ) : (
          <p className="text-center text-silver">
            No hay resultados de búsqueda.
          </p>
        )}
        <ModalPerfil
            isOpen={isProfileModalOpen}
            onClose={closeProfileModal}
            userId={id}
        />
      </div>
    </Modal>
  );
};

export default ModalBusqueda;
