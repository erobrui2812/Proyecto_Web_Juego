"use client";
import Chat from "@/components/Chat";
import GameGrid from "@/components/GameGrid";
import { useAuth } from "@/contexts/AuthContext";
import { useWebsocket } from "@/contexts/WebsocketContext";
import { useParams, useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";

export default function GamePage() {
  const params = useParams();
  const gameId = params.gameId;
  const router = useRouter();
  const { auth, userDetail } = useAuth();
  const { socket } = useWebsocket();
  const [gameOver, setGameOver] = useState(false);
  const [gameOverMessage, setGameOverMessage] = useState("");

  useEffect(() => {
    if (!auth?.token || !userDetail?.id) return;
    const fetchGameState = async () => {
      try {
        const res = await fetch(`https://localhost:7162/api/game/${gameId}`, {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        });
        console.log("Status de la petición:", res.status);
        // Si la respuesta tiene status 404, marcamos la partida como inexistente.
        if (res.status === 404) {
          setGameOver(true);
          setGameOverMessage("La partida ya no existe.");
          return;
        }
        // Si la respuesta es 401 o cualquier otro error, se puede manejar aquí también.
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
    fetchGameState();
  }, [auth, userDetail, gameId]);

  return (
    <div className="p-6">
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
          <h1 className="text-2xl font-bold mb-4">Partida en curso</h1>
          <p className="mb-4">ID de la partida: {gameId}</p>
          {!socket && (
            <div className="text-red-500">Conectando a la partida...</div>
          )}
          {gameOver ? (
            <div className="mt-4 p-4 bg-gray-800 text-white rounded">
              <p>{gameOverMessage}</p>
            </div>
          ) : (
            <>
              <div className="mb-6">
                <GameGrid gameId={gameId} playerId={userDetail.id} />
              </div>
              <Chat gameId={gameId} />
            </>
          )}
        </>
      )}
    </div>
  );
}
