"use client";

import { Search } from "lucide-react";
import { useState } from "react";

interface BuscadorUsuariosProps {
  onSearch: (query: string) => void;
  onOpenRequestsModal: () => void;
}

const BuscadorUsuarios: React.FC<BuscadorUsuariosProps> = ({
  onSearch,
  onOpenRequestsModal,
}) => {
  const [searchTerm, setSearchTerm] = useState("");

  const handleSearchClick = () => {
    onSearch(searchTerm);
  };

  return (
    <div className="flex items-center space-x-4">
      <input
        type="text"
        placeholder="Buscar amigos o usuarios..."
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        className="border p-2 rounded-md w-full focus:ring-2 focus:ring-primary"
      />
      <button
        onClick={handleSearchClick}
        className="p-2 bg-blue-500 text-white rounded-md flex items-center space-x-2 hover:bg-blue-600"
      >
        <Search size={20} />
        <span>Buscar Usuarios</span>
      </button>
      <button
        onClick={onOpenRequestsModal}
        className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
      >
        Solicitudes de Amistad
      </button>
    </div>
  );
};

export default BuscadorUsuarios;
