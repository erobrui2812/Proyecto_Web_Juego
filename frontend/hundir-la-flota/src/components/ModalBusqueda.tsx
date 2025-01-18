"use client";

import Modal from "@/components/Modal";
import { useFriendship } from "@/contexts/FriendshipContext";

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

  const handleSendRequest = async (nickname: string) => {
    try {
      await sendFriendRequest(nickname);
    } catch (error) {
      console.error("Error al enviar solicitud de amistad:", error);
    }
  };
  return (
    <Modal title="Buscar Usuarios" isOpen={isOpen} onClose={onClose}>
      <div className="space-y-4">
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
                onClick={() => handleSendRequest(user.nickname)}
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
      </div>
    </Modal>
  );
};

export default ModalBusqueda;
