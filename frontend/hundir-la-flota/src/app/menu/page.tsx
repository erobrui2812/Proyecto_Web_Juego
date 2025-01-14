"use client";
import React, { useState } from "react";
import { Trash2, Search, LogOut, User } from "lucide-react";
import { useGlobalContext } from "../contexts/GlobalContext";
//import Modal from "../components/Modal";

const MenuPage = () => {
  const { friendship, auth } = useGlobalContext();
  const { userDetail, cerrarSesion } = auth;
  const { friends, removeFriend, sendFriendRequest, respondToFriendRequest } = friendship;
  const [searchTerm, setSearchTerm] = useState("");
  const [isSearchModalOpen, setIsSearchModalOpen] = useState(false);
  const [isRequestsModalOpen, setIsRequestsModalOpen] = useState(false);

  const filteredFriends = friends.filter(friend =>
    friend.nickname.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").includes(
      searchTerm.toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "")
    )
  );

  const handleRemoveFriend = (friendId: string) => {
    if (confirm("¿Estás seguro de que quieres eliminar a este amigo?")) {
      removeFriend(friendId);
    }
  };

  return (
    <div className="max-w-6xl mx-auto mt-8 space-y-8">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <img src={userDetail?.avatarUrl} alt="User Avatar" className="w-10 h-10 rounded-full" />
          <div>
            <h1 className="text-xl font-bold">{userDetail?.nickname}</h1>
            <button className="text-blue-500 hover:underline" onClick={() => alert("Ir al perfil")}>
              Ver Perfil
            </button>
          </div>
        </div>
        <button className="text-red-500 hover:text-red-700 flex items-center space-x-2" onClick={cerrarSesion}>
          <LogOut size={20} />
          <span>Cerrar Sesión</span>
        </button>
      </div>

      <div className="flex items-center space-x-4">
        <input
          type="text"
          placeholder="Buscar amigos..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="border p-2 rounded-md w-full"
        />
        <button
          onClick={() => setIsSearchModalOpen(true)}
          className="p-2 bg-blue-500 text-white rounded-md flex items-center space-x-2"
        >
          <Search size={20} />
          <span>Buscar Usuarios</span>
        </button>
        <button
          onClick={() => setIsRequestsModalOpen(true)}
          className="p-2 bg-green-500 text-white rounded-md"
        >
          Solicitudes de Amistad
        </button>
      </div>

    
      <div className="space-y-4">
        {filteredFriends.map((friend) => (
          <div key={friend.id} className="flex items-center space-x-4 p-4 border-b">
            <img src={friend.urlAvatar} alt={`${friend.nickname}'s Avatar`} className="w-8 h-8 rounded-full" />
            <div className="flex flex-col">
              <span className="font-semibold">{friend.nickname}</span>
              <span className="text-sm text-gray-500">{friend.email}</span>
              <span className={`text-sm ${friend.status === "Conectado" ? "text-green-500" : "text-gray-500"}`}>
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
      
      <div className="flex items-center justify-between p-4 border rounded-md">
        <span>Total de jugadores conectados: 123</span>
        <span>Partidas activas: 45</span>
        <span>Jugadores en partidas: 90</span>
      </div>

      <button
        onClick={() => alert("Ir a Emparejamiento")}
        className="p-4 bg-blue-500 text-white rounded-md w-full text-center"
      >
        Jugar
      </button>

      {/*
      <Modal isOpen={isSearchModalOpen} onClose={() => setIsSearchModalOpen(false)}>
        <h2 className="text-lg font-bold">Buscar Usuarios</h2>
        <p>Aquí irá la lógica para buscar usuarios y enviar solicitudes de amistad.</p>
      </Modal>
      <Modal isOpen={isRequestsModalOpen} onClose={() => setIsRequestsModalOpen(false)}>
        <h2 className="text-lg font-bold">Solicitudes de Amistad</h2>
        <p>Aquí irá la lista de solicitudes de amistad con opciones para aceptar o rechazar.</p>
      </Modal> */}
    </div>
  );
};

export default MenuPage;
