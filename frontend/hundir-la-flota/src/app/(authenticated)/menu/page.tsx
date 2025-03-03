"use client";
import AvatarUsuario from "@/components/AvatarUsuario";
import BotonJugar from "@/components/BotonJugar";
import BuscadorUsuarios from "@/components/BuscadorUsuarios";
import ListaAmigos from "@/components/ListaAmigos";
import ModalBusqueda from "@/components/ModalBusqueda";
import ModalSolicitudes from "@/components/ModalSolicitudes";
import { useFriendship } from "@/contexts/FriendshipContext";
import "@fontsource/bebas-neue";
import { useEffect, useState } from "react";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

const MenuPage = () => {
  const { searchUsers, searchResults } = useFriendship();
  const [isSearchModalOpen, setIsSearchModalOpen] = useState(false);
  const [isRequestsModalOpen, setIsRequestsModalOpen] = useState(false);
  const [onlineUsers, setOnlineUsers] = useState("0");
  const [playersInGame, setPlayersInGame] = useState<unknown[]>([]);
  const [activeGames, setActiveGames] = useState("0");
  const [leaderboard, setLeaderboard] = useState([]);

  useEffect(() => {
    const fetchOnlineUsers = async () => {
      try {
        const response = await fetch(`${API_URL}api/stats/global`);
        const data = await response.json();
        setOnlineUsers(data.onlineUsers);
      } catch (error) {
        console.error("Error al obtener usuarios en línea:", error);
      }
    };

    const fetchActiveGames = async () => {
      try {
        const response = await fetch(`${API_URL}api/stats/games`);
        const data = await response.json();
        setActiveGames(data.activeGames || data.length.toString());
      } catch (error) {
        console.error("Error al obtener partidas en curso:", error);
      }
    };

    const fetchPlayersInGame = async () => {
      try {
        const response = await fetch(
          `${API_URL}api/stats/players`
        );
        const data = await response.json();
        setPlayersInGame(data);
      } catch (error) {
        console.error("Error al obtener jugadores en partida:", error);
      }
    };

    const fetchLeaderboard = async () => {
      try {
        const response = await fetch(
          `${API_URL}api/stats/leaderboard`
        );
        const data = await response.json();
        setLeaderboard(data);
      } catch (error) {
        console.error("Error al obtener leaderboard:", error);
      }
    };

    fetchOnlineUsers();
    fetchActiveGames();
    fetchPlayersInGame();
    fetchLeaderboard();
  }, []);

  const handleSearch = async (query: string) => {
    if (query.trim()) {
      await searchUsers(query);
      setIsSearchModalOpen(true);
    }
  };

  return (
    <div className="w-full min-h-screen bg-white">
      <div className="bg-dark w-full">
        <div className="max-w-7xl mx-auto py-8 px-4 md:px-6 lg:px-8 text-black">
          <div className="flex flex-col md:flex-row md:gap-4">
            <div className="w-full md:w-3/5 mb-8 md:mb-0 md:pr-4 flex flex-col gap-6 p-4 rounded-md">
              <div>
                <AvatarUsuario />
              </div>
              <div>
                <BuscadorUsuarios
                  onSearch={handleSearch}
                  onOpenRequestsModal={() => setIsRequestsModalOpen(true)}
                />
              </div>
              <div className="flex justify-center">
                <div className="w-40">
                  <BotonJugar />
                </div>
              </div>
            </div>
            <div className="w-full md:w-2/5 p-4 flex flex-col gap-6 border-2 border-gold rounded-md text-background">
              <div>
                <h2 className="text-xl font-bold mb-2">Jugadores Online</h2>
                <p>{onlineUsers}</p>
              </div>
              <div>
                <h2 className="text-xl font-bold mb-2">Partidas en Curso</h2>
                <p>{activeGames}</p>
              </div>
              <div>
                <h2 className="text-xl font-bold mb-2">Jugadores en Partida</h2>
                {playersInGame.length > 0 ? (
                  playersInGame.length
                ) : (
                  <p>No hay partidas activas</p>
                )}
              </div>
              <div>
                <h2 className="text-xl font-bold mb-2">Top 3 del Servidor</h2>
                <ul className="list-decimal list-inside">
                  {leaderboard.slice(0, 3).map((item: unknown) => (
                    <li key={item.userId}>
                      {item.nickname} ({item.gamesWon})
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="relative h-16 bg-dark">
        <div className="custom-shape-divider-bottom-1737375958">
          <svg
            data-name="Layer 1"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 1200 120"
            preserveAspectRatio="none"
          >
            <path
              d="M1200 0L0 0 892.25 114.72 1200 0z"
              className="shape-fill"
            ></path>
          </svg>
        </div>
      </div>
      <div className="max-w-7xl mx-auto px-4 md:px-6 lg:px-8 pb-20 text-black">
        <h2 className="font-bebasneue text-2xl font-bold my-4">Mis Amigos</h2>
        <ListaAmigos />
      </div>
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
