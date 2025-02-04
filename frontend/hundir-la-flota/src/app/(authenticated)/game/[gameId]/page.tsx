"use client";
import Chat from "@/components/Chat";
import GameGrid from "@/components/GameGrid";
import { useAuth } from "@/contexts/AuthContext";
import { useWebsocket } from "@/contexts/WebsocketContext";
import { useParams } from "next/navigation";
import React from "react";
export default function GamePage() {
  const params = useParams();
  const gameId = params.gameId;
  const { auth, userDetail } = useAuth();
  const { socket } = useWebsocket();
  if (!auth?.token) {
    return (
      <div className="p-6 text-red-500">
        Debes estar autenticado para jugar.
      </div>
    );
  }
  const playerId = userDetail?.id;
  if (!playerId) {
    return (
      <div className="p-6 text-red-500">
        No se pudo obtener el ID del jugador. Verifica tu sesión.
      </div>
    );
  }
  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Partida en curso</h1>
      <p className="mb-4">ID de la partida: {gameId}</p>
      {!socket && (
        <div className="text-red-500">Conectando a la partida...</div>
      )}
      <div className="mb-6">
        <GameGrid gameId={gameId} playerId={playerId} />
      </div>
      <Chat gameId={gameId} />
    </div>
  );
}
