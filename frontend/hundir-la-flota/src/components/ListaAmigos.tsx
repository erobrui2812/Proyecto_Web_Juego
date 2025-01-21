"use client";

import Modal from "@/components/Modal";
import ModalPerfil from "@/components/ModalPerfil";
import { useFriendship } from "@/contexts/FriendshipContext";
import { Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import ReactPaginate from "react-paginate";

const translateStatus = (status: string) => {
  switch (status) {
    case "Connected":
      return "Conectado";
    case "Disconnected":
      return "Desconectado";
    case "Playing":
      return "Jugando";
    default:
      return "Desconocido";
  }
};

const ListaAmigos = () => {
  const { friends, removeFriend, fetchFriends } = useFriendship();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [selectedFriend, setSelectedFriend] = useState<{id: string, nickname: string} | null>(null);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  const [pageNumber, setPageNumber] = useState(0);
  const [friendsPerPage, setFriendsPerPage] = useState(3);

  const pagesVisited = pageNumber * friendsPerPage;
  const currentFriends = friends.slice(
    pagesVisited,
    pagesVisited + friendsPerPage
  );
  const pageCount = Math.ceil(friends.length / friendsPerPage);

  const changePage = (selectedItem: { selected: number }) => {
    setPageNumber(selectedItem.selected);
  };

  const openProfileModal = (userId: string) => {
    setSelectedUserId(userId);
    setIsProfileModalOpen(true);
  };

  const closeProfileModal = () => {
    setSelectedUserId(null);
    setIsProfileModalOpen(false);
  };

  const openDeleteModal = (friend: { id: string; nickname: string }) => {
    setSelectedFriend(friend);
    setIsModalOpen(true);
  };

  const closeDeleteModal = () => {
    setSelectedFriend(null);
    setIsModalOpen(false);
  };

  const confirmRemoveFriend = () => {
    if (selectedFriend) {
      removeFriend(selectedFriend.id);
    }
    closeDeleteModal();
  };

  useEffect(() => {
    fetchFriends();
  }, []); 

  useEffect(() => {
    console.log("Amigos actuales:", friends);
  }, [friends]);

  return (
    <div>
      <div className="flex items-center gap-2 mb-4">
        <label htmlFor="friendsPerPage" className="font-semibold">
          Amigos por página:
        </label>
        <select
          id="friendsPerPage"
          value={friendsPerPage}
          onChange={(e) => {
            setFriendsPerPage(parseInt(e.target.value));
            setPageNumber(0);
          }}
          className="p-1 border rounded"
        >
          <option value={3}>3</option>
          <option value={6}>6</option>
          <option value={9}>9</option>
          <option value={18}>18</option>
        </select>
      </div>

      {friends.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {currentFriends.map((friend) => (
            <div
              key={`${friend.id}`}
              className="flex flex-col p-4 bg-gray-800 rounded-md shadow-md"
            >
              <div className="flex items-center mb-2">
                <img
                  src={friend.urlAvatar || "https://via.placeholder.com/150"}
                  alt={`${friend.nickname}'s Avatar`}
                  className="w-10 h-10 rounded-full border-2 border-secondary mr-3"
                />
                <div>
                  <span className="font-semibold text-gold block">
                    {friend.nickname}
                  </span>
                  <span
                    className={`text-sm ${
                      friend.status === "Connected"
                        ? "text-green-400"
                        : friend.status === "Playing"
                        ? "text-blue-400"
                        : "text-gray-400"
                    }`}
                  >
                    {translateStatus(friend.status || "Disconnected")}
                  </span>
                </div>
              </div>

              <p className="text-sm text-gray-200 mb-2">
                {friend.email || "Correo no disponible"}
              </p>

              <div className="flex justify-between items-center mt-auto">
                <button
                  onClick={() => openProfileModal(friend.id)}
                  className="text-blue-400 hover:underline"
                >
                  Ver perfil
                </button>
                <button
                  onClick={() => openDeleteModal(friend)}
                  className="text-red-500 hover:text-red-700"
                >
                  <Trash2 size={20} />
                </button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-gray-500">No hay amigos disponibles.</p>
      )}

      <div className="mt-4">
        <ReactPaginate
          previousLabel={"< Anterior"}
          nextLabel={"Siguiente >"}
          pageCount={pageCount}
          onPageChange={changePage}
          forcePage={pageNumber}
          containerClassName={"flex justify-center items-center gap-2"}
          pageLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          previousLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          nextLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          disabledClassName={"opacity-50 cursor-not-allowed"}
          activeClassName={"bg-blue-500 text-white"}
        />
      </div>

      <ModalPerfil
        isOpen={isProfileModalOpen}
        onClose={closeProfileModal}
        userId={selectedUserId}
      />

      <Modal
        isOpen={isModalOpen}
        onClose={closeDeleteModal}
        title="Confirmar eliminación"
      >
        <p>
          ¿Estás seguro de que deseas eliminar a{" "}
          <span className="font-bold">{selectedFriend?.nickname}</span> de tu
          lista de amigos?
        </p>
        <div className="flex justify-end space-x-4 mt-4">
          <button
            onClick={closeDeleteModal}
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
