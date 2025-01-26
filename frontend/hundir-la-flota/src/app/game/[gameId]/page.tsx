// app/game/[id]/page.tsx
"use client";

import { useParams } from "next/navigation";

export default function GamePage() {
  const params = useParams();
  const gameId = params.gameId;

  return (
    <div>
      <h1>Página de partida</h1>
      <p>El ID de la partida es: {gameId}</p>
      {/* Aquí podrías hacer fetch a tu API para mostrar el estado del juego */}
    </div>
  );
}
