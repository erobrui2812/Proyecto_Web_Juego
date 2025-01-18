"use client";

import { useState } from "react";
import AvatarUsuario from "@/components/AvatarUsuario";
import BuscadorUsuarios from "@/components/BuscadorUsuarios";
import ListaAmigos from "@/components/ListaAmigos";
import ModalBusqueda from "@/components/ModalBusqueda";
import ModalSolicitudes from "@/components/ModalSolicitudes";
import BotonJugar from "@/components/BotonJugar";
import { useFriendship } from "@/contexts/FriendshipContext";

const MenuPage = () => {
  const { searchUsers, searchResults } = useFriendship();
  const [isSearchModalOpen, setIsSearchModalOpen] = useState(false);
  const [isRequestsModalOpen, setIsRequestsModalOpen] = useState(false);

  const handleSearch = async (query: string) => {
    if (query.trim()) {
      await searchUsers(query); // Ejecuta la búsqueda desde el contexto
      setIsSearchModalOpen(true); // Abre el modal
    }
  };

  return (
    <div className="max-w-6xl mx-auto mt-8 space-y-8">
      <AvatarUsuario />

      <BuscadorUsuarios
        onSearch={handleSearch}
        onOpenRequestsModal={() => setIsRequestsModalOpen(true)}
      />

      <ListaAmigos />

      <BotonJugar />

      <ModalBusqueda
        isOpen={isSearchModalOpen}
        onClose={() => setIsSearchModalOpen(false)}
        searchResults={searchResults} // Pasar resultados de búsqueda como prop
      />

      <ModalSolicitudes
        isOpen={isRequestsModalOpen}
        onClose={() => setIsRequestsModalOpen(false)}
      />
    </div>
  );
};

export default MenuPage;
