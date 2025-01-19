"use client";

import { Trash2 } from "lucide-react";
import { useFriendship } from "@/contexts/FriendshipContext";
import { useState } from "react";
import Modal from "@/components/Modal";

const translateStatus = (status: string) => {
  switch (status) {
    case "Connected":
      return "Conectado";
    case "Disconnected":
      return "Desconectado";
    case "Playing":
      return "Jugando";
    default:
      return status;
  }
};

const ListaAmigos = () => {
  const { friends, removeFriend } = useFriendship();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedFriend, setSelectedFriend] = useState<{ id: string; nickname: string } | null>(null);

  const openModal = (friend: { id: string; nickname: string }) => {
    setSelectedFriend(friend);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setSelectedFriend(null);
    setIsModalOpen(false);
  };

  const confirmRemoveFriend = () => {
    if (selectedFriend) {
      removeFriend(selectedFriend.id);
    }
    closeModal();
  };

  return (
    <div className="space-y-4">
      {friends.map((friend) => (
        <div
          key={friend.id}
          className="flex items-center space-x-4 p-4 bg-gray-800 rounded-md shadow-md"
        >
          <img
            src={friend.urlAvatar}
            alt={`${friend.nickname}'s Avatar`}
            className="w-10 h-10 rounded-full border-2 border-secondary"
          />
          <div className="flex flex-col">
            <span className="font-semibold text-gold">{friend.nickname}</span>
            <span
              className={`text-sm ${
                friend.status === "Connected"
                  ? "text-green-500"
                  : friend.status === "Playing"
                  ? "text-blue-500"
                  : "text-gray-500"
              }`}
            >
              {translateStatus(friend.status)}
            </span>
          </div>
          <button
            onClick={() => openModal(friend)}
            className="ml-auto text-red-500 hover:text-red-700"
          >
            <Trash2 size={20} />
          </button>
        </div>
      ))}

      <Modal
        isOpen={isModalOpen}
        onClose={closeModal}
        title="Confirmar eliminación"
      >
        <p>
          ¿Estás seguro de que deseas eliminar a{" "}
          <span className="font-bold">{selectedFriend?.nickname}</span> de tu
          lista de amigos?
        </p>
        <div className="flex justify-end space-x-4 mt-4">
          <button
            onClick={closeModal}
            className="px-4 py-2 bg-gray-300 rounded hover:bg-gray-400"
          >
            Cancelar
          </button>
          <button
            onClick={confirmRemoveFriend}
            className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600"
          >
            Confirmar
          </button>
        </div>
      </Modal>
    </div>
  );
};

export default ListaAmigos;
