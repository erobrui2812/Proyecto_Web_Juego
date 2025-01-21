"use client";

import { useEffect, useState } from "react";

const GamePage = ({ params }: { params?: { gameId?: string } }) => {
  const [gameState, setGameState] = useState(null);
  const [placedShips, setPlacedShips] = useState([]);
  const [error, setError] = useState<string | null>(null);

  const fetchGameState = async (gameId: string) => {
    try {
      const response = await fetch(`/api/game/${gameId}`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
      });
      if (!response.ok)
        throw new Error("Error al obtener el estado del juego.");
      const data = await response.json();
      setGameState(data);
    } catch (error) {
      console.error(error);
      setError("Error al obtener el estado del juego.");
    }
  };

  const handleStartGame = async () => {
    try {
      if (!params?.gameId) {
        setError("No se encontró el ID del juego.");
        return;
      }

      const response = await fetch(`/api/game/${params.gameId}/place-ships`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${localStorage.getItem("token")}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ playerId: 1, ships: placedShips }),
      });
      if (!response.ok) throw new Error("Error al iniciar el juego.");
      fetchGameState(params.gameId);
    } catch (error) {
      console.error(error);
      setError("Error al iniciar el juego.");
    }
  };

  useEffect(() => {
    if (!params?.gameId) {
      setError("No se encontró el ID del juego.");
      return;
    }

    fetchGameState(params.gameId);
  }, [params?.gameId]);

  if (error) {
    return <div>{error}</div>;
  }

  return (
    <div>
      <h1>Juego: {params?.gameId}</h1>
      <button onClick={handleStartGame}>Colocar Barcos</button>
      <pre>{JSON.stringify(gameState, null, 2)}</pre>
    </div>
  );
};

export default GamePage;
