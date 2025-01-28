"use client";

import Chat from "@/components/Chat";
import { useParams } from "next/navigation";

export default function GamePage() {
  const params = useParams();
  const gameId = params.gameId;

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Página de partida</h1>
      <p className="mb-4">El ID de la partida es: {gameId}</p>
      <Chat gameId={gameId} />
    </div>
  );
}
