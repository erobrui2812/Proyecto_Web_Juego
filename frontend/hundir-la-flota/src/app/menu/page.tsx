"use client";
import { LogOut, Search, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import Modal from "@/components/Modal";
import { useGlobalContext } from "@/contexts/GlobalContext";

type PendingRequest = {
  id: string;
  fromUserId: string;
  fromUserNickname: string;
  createdAt: string;
};

const MenuPage = () => {
  const { friendship, auth } = useGlobalContext();
  const { userDetail, cerrarSesion } = auth;
  const {
    friends,
    removeFriend,
    sendFriendRequest,
    searchUsers,
    searchResults,
  } = friendship;
  const [searchTerm, setSearchTerm] = useState("");
  const [isSearchModalOpen, setIsSearchModalOpen] = useState(false);
  const [isRequestsModalOpen, setIsRequestsModalOpen] = useState(false);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);

  const token = auth.auth.token;

  const filteredFriends = friends.filter((friend) =>
    friend.nickname
      .toLowerCase()
      .normalize("NFD")
      .replace(/\p{Diacritic}/gu, "")
      .includes(
        searchTerm
          .toLowerCase()
          .normalize("NFD")
          .replace(/\p{Diacritic}/gu, "")
      )
  );

  const handleRemoveFriend = (friendId: string) => {
    if (confirm("¿Estás seguro de que quieres eliminar a este amigo?")) {
      removeFriend(friendId);
    }
  };

  const fetchPendingRequests = async () => {
    try {
      const response = await fetch(
        "https://localhost:7162/api/friendship/pending",
        {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) throw new Error(`Error: ${response.statusText}`);

      const data = await response.json();
      setPendingRequests(data);
    } catch (error) {
      console.error(
        "Error al obtener solicitudes de amistad pendientes:",
        error
      );
    }
  };

  const handleSearch = () => {
    if (searchTerm.trim()) {
      searchUsers(searchTerm);
    }
  };

  useEffect(() => {
    if (isRequestsModalOpen) {
      fetchPendingRequests();
    }
  }, [isRequestsModalOpen]);

  return (
    <div className="max-w-6xl mx-auto mt-8 space-y-8">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <img
            src={userDetail?.avatarUrl}
            alt="User Avatar"
            className="w-12 h-12 rounded-full border-2 border-gold"
          />
          <div>
            <h1 className="text-xl font-bold text-gold">
              {userDetail?.nickname}
            </h1>
            <button
              className="text-blue-400 hover:underline"
              onClick={() => alert("Ir al perfil")}
            >
              Ver Perfil
            </button>
          </div>
        </div>
        <button
          className="text-red-500 hover:text-red-700 flex items-center space-x-2"
          onClick={cerrarSesion}
        >
          <LogOut size={20} />
          <span>Cerrar Sesión</span>
        </button>
      </div>

      <div className="flex items-center space-x-4">
        <input
          type="text"
          placeholder="Buscar amigos o usuarios..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="border p-2 rounded-md w-full focus:ring-2 focus:ring-primary"
        />
        <button
          onClick={() => {
            setIsSearchModalOpen(true);
            handleSearch();
          }}
          className="p-2 bg-blue-500 text-white rounded-md flex items-center space-x-2 hover:bg-blue-600"
        >
          <Search size={20} />
          <span>Buscar Usuarios</span>
        </button>
        <button
          onClick={() => setIsRequestsModalOpen(true)}
          className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
        >
          Solicitudes de Amistad
        </button>
      </div>

      <div className="space-y-4">
        {filteredFriends.map((friend) => (
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
                  friend.status === "Conectado"
                    ? "text-green-500"
                    : "text-gray-500"
                }`}
              >
                {friend.status}
              </span>
            </div>
            <button
              onClick={() => handleRemoveFriend(friend.id)}
              className="ml-auto text-red-500 hover:text-red-700"
            >
              <Trash2 size={20} />
            </button>
          </div>
        ))}
      </div>

      <button
        onClick={() => alert("Ir a Emparejamiento")}
        className="p-4 bg-primary text-white rounded-md w-full text-center hover:bg-wine"
      >
        Jugar
      </button>

      <Modal
        isOpen={isSearchModalOpen}
        onClose={() => setIsSearchModalOpen(false)}
        title="Buscar Usuarios"
      >
        <div className="space-y-4">
          {searchResults.map((user) => (
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
                onClick={() => sendFriendRequest(user.id)}
                className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
              >
                Enviar Solicitud
              </button>
            </div>
          ))}
        </div>
      </Modal>

      <Modal
        isOpen={isRequestsModalOpen}
        onClose={() => setIsRequestsModalOpen(false)}
        title="Solicitudes de Amistad"
      >
        <div className="space-y-4">
          {pendingRequests.map((request) => (
            <div
              key={request.id}
              className="flex items-center justify-between p-4 bg-gray-700 rounded-md"
            >
              <div>
                <p className="font-semibold text-gold">
                  {request.fromUserNickname}
                </p>
                <p className="text-sm text-silver">
                  Enviado el: {new Date(request.createdAt).toLocaleDateString()}
                </p>
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={() => {
                    friendship.respondToFriendRequest(request.fromUserId, true);
                    setPendingRequests((prev) =>
                      prev.filter((r) => r.id !== request.id)
                    );
                  }}
                  className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
                >
                  Aceptar
                </button>
                <button
                  onClick={() => {
                    friendship.respondToFriendRequest(
                      request.fromUserId,
                      false
                    );
                    setPendingRequests((prev) =>
                      prev.filter((r) => r.id !== request.id)
                    );
                  }}
                  className="p-2 bg-red-500 text-white rounded-md hover:bg-red-600"
                >
                  Rechazar
                </button>
              </div>
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
};

export default MenuPage;
