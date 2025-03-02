"use client";
import Chat from "@/components/Chat";
import GameGrid from "@/components/GameGrid";
import { useAuth } from "@/contexts/AuthContext";
import { useWebsocket } from "@/contexts/WebsocketContext";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function GamePage() {
  const params = useParams();
  const gameId = params.gameId;
  const router = useRouter();
  const { auth, userDetail } = useAuth();
  const { socket } = useWebsocket();
  const [gameOver, setGameOver] = useState(false);
  const [gameOverMessage, setGameOverMessage] = useState("");

  const fetchGameState = async () => {
    if (!auth?.token || !userDetail?.id) return;
    try {
      const res = await fetch(`https://localhost:7162/api/game/${gameId}`, {
        headers: {
          Authorization: `Bearer ${auth.token}`,
        },
      });
      console.log("Status de la petición:", res.status);

      if (res.status === 404) {
        setGameOver(true);
        setGameOverMessage("La partida ya no existe.");
        return;
      }
      if (!res.ok) {
        console.error("Error en la petición:", res.status);
        setGameOver(true);
        setGameOverMessage("Error al obtener el estado de la partida.");
        return;
      }
      const data = await res.json();

      if (data.stateDescription === "La partida ha terminado.") {
        setGameOver(true);
        setGameOverMessage("La partida ha terminado.");
      }
    } catch (error) {
      console.error("Error fetching game state:", error);
      setGameOver(true);
      setGameOverMessage("Error al obtener el estado de la partida.");
    }
  };

  useEffect(() => {
    fetchGameState();

    const intervalId = setInterval(() => {
      fetchGameState();
    }, 300000);

    return () => clearInterval(intervalId);
  }, [auth, userDetail, gameId]);

  return (
    <div className="p-6 min-h-screen bg-[var(--background)]">
      {!auth?.token ? (
        <div className="p-6 text-red-500">
          Debes estar autenticado para jugar.
        </div>
      ) : !userDetail?.id ? (
        <div className="p-6 text-red-500">
          No se pudo obtener el ID del jugador. Verifica tu sesión.
        </div>
      ) : (
        <>
          <h1 className="text-2xl font-bold mb-4 text-[var(--foreground)]">
            Partida en curso
          </h1>
          <p className="mb-4 text-[var(--foreground)]">
            ID de la partida: {gameId}
          </p>
          {!socket && (
            <div className="text-red-500 mb-4">Conectando a la partida...</div>
          )}
          {gameOver ? (
            <div className="mt-4 p-4 bg-gray-800 text-[var(--foreground)] rounded-lg shadow">
              <p>{gameOverMessage}</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="md:col-span-2">
                <GameGrid gameId={gameId} playerId={userDetail.id} />
              </div>
              <div className="w-full">
                <Chat gameId={gameId} />
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
