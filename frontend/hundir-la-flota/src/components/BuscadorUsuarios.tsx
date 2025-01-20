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
    if (searchTerm.trim()) {
      onSearch(searchTerm);
    }
  };

  return (
    <div className="w-full">
      <div>
        <input
          type="text"
          placeholder="Buscar amigos o usuarios..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="text-black border-2 border-gold  p-2 rounded-md w-full focus:ring-2 focus:ring-primary"
        />
      </div>

      <div className="flex justify-center items-center space-x-4 mt-4">
        <button
          onClick={handleSearchClick}
          className="border-2 border-gold p-2 bg-primary text-white rounded-md flex items-center space-x-2 hover:bg-blue-600"
        >
          <Search size={20} />
          <span>Buscar Usuarios</span>
        </button>
        <button
          onClick={onOpenRequestsModal}
          className="border-2 border-gold p-2 bg-secondary text-white rounded-md hover:bg-green-600"
        >
          Solicitudes de Amistad
        </button>
      </div>
    </div>
  );
};

export default BuscadorUsuarios;
