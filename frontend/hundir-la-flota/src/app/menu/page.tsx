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
      await searchUsers(query);
      setIsSearchModalOpen(true); 
    }
  };

  return (
    <div className="max-w-6xl mx-auto mt-8 flex flex-col gap-8">
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
        searchResults={searchResults}
      />

      <ModalSolicitudes
        isOpen={isRequestsModalOpen}
        onClose={() => setIsRequestsModalOpen(false)}
      />
    </div>
  );
};

export default MenuPage;
