"use client";
import { DndContext } from "@dnd-kit/core";
import React, { useEffect, useState } from "react";
import { useWebsocket } from "../contexts/WebsocketContext";
import { useRouter } from "next/navigation";

const shipSizes = [5, 4, 3, 3, 2];

const generateRandomBoard = () => {
  const newGrid = Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      hasShip: false,
      isHit: false,
      isSunk: false,
    }))
  );

  const ships = [];
  for (const size of shipSizes) {
    let placed = false;
    while (!placed) {
      const x = Math.floor(Math.random() * 10);
      const y = Math.floor(Math.random() * 10);
      const orientation = Math.random() > 0.5 ? "horizontal" : "vertical";

      if (orientation === "horizontal") {
        if (
          x + size > 10 ||
          newGrid[y].slice(x, x + size).some((cell) => cell.hasShip)
        )
          continue;
        for (let i = 0; i < size; i++) newGrid[y][x + i].hasShip = true;
        ships.push({ x, y, size, orientation });
      } else {
        if (
          y + size > 10 ||
          newGrid.slice(y, y + size).some((row) => row[x].hasShip)
        )
          continue;
        for (let i = 0; i < size; i++) newGrid[y + i][x].hasShip = true;
        ships.push({ x, y, size, orientation });
      }
      placed = true;
    }
  }

  return { grid: newGrid, ships };
};

const GameGrid = ({ gameId, playerId }) => {
  const { socket, sendMessage } = useWebsocket();
  const [{ grid, ships }, setBoard] = useState(generateRandomBoard);
  const [isMyTurn, setIsMyTurn] = useState(false);
  const [shipsPlaced, setShipsPlaced] = useState(false);
  const [gameStarted, setGameStarted] = useState(false);
  const [isReady, setIsReady] = useState(false);
  const router = useRouter();

  useEffect(() => {
    if (!socket) return;
    // Unirse a la partida
    sendMessage("joinGame", `${gameId}|${playerId}`);

    const handleMessage = (event) => {
      try {
        const parts = event.data.split("|");
        const type = parts[0];
        const payload = parts.slice(1).join("|");

        console.log(`Evento recibido: ${type}`, payload);

        switch (type) {
          case "YourTurn":
            setIsMyTurn(true);
            break;
          case "GameStarted":
            setGameStarted(true);
            break;
          case "ShipsPlaced":
            setShipsPlaced(true);
            break;
          case "AttackResult":
            handleAttackResult(payload);
            break;
          case "GameOver":
            const timeout = setTimeout(() => {
              router.push(`/menu`);
            }, 5000);

            timeout;
            break;
          default:
            console.warn("Evento WebSocket no reconocido:", type);
        }
      } catch (error) {
        console.warn("Error procesando mensaje WebSocket:", event.data);
      }
    };

    socket.addEventListener("message", handleMessage);
    return () => {
      socket.removeEventListener("message", handleMessage);
    };
  }, [socket, gameId, playerId, sendMessage]);

  const handlePlaceShips = () => {
    const { grid: newGrid, ships } = generateRandomBoard();
    setBoard({ grid: newGrid, ships });

    if (!ships.length) return;

    const formattedShips = ships
      .map((ship) => `${ship.x},${ship.y},${ship.size},${ship.orientation}`)
      .join(";");
    sendMessage("placeShips", `${gameId}|${playerId}|${formattedShips}`);
  };

  const handleConfirmReady = () => {
    sendMessage("confirmReady", `${gameId}|${playerId}`);
    setIsReady(true);
  };

  const handlePassTurn = () => {
    if (isMyTurn) {
      sendMessage("passTurn", `${gameId}|${playerId}`);
      setIsMyTurn(false);
    }
  };

  const handleAttack = (x, y) => {
    if (isMyTurn) {
      sendMessage("Attack", `${gameId}|${playerId}|${x}|${y}`);
      setIsMyTurn(false);
    }
  };

  const handleAttackResult = (result) => {
    // Asegurarse de trabajar con un objeto (si viene en string, se parsea)
    const attackData = typeof result === "string" ? JSON.parse(result) : result;
    const { x, y, result: attackResult } = attackData;
    const updatedGrid = grid.map((row, rowIndex) =>
      row.map((cell, colIndex) =>
        rowIndex === y && colIndex === x ? { ...cell, isHit: true } : cell
      )
    );
    setBoard({ grid: updatedGrid, ships });

    if (attackResult === "hit") {
      console.log(`¡Acierto! En la posición (${x}, ${y})`);
    } else if (attackResult === "miss") {
      console.log(`Fallaste el ataque en (${x}, ${y})`);
    } else if (attackResult === "sunk") {
      console.log(`¡Barco hundido! En la posición (${x}, ${y})`);
    }
    setIsMyTurn(false);
  };

  return (
    <DndContext>
      <div className="mb-4 flex flex-col items-center">
        {!gameStarted ? (
          <>
            <button
              onClick={handlePlaceShips}
              className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600 transition"
              disabled={shipsPlaced}
            >
              {shipsPlaced
                ? "Barcos colocados"
                : "Colocar Barcos Aleatoriamente"}
            </button>
            {shipsPlaced && (
              <button
                onClick={handleConfirmReady}
                className="mt-2 bg-yellow-500 text-white px-4 py-2 rounded hover:bg-yellow-600 transition"
                disabled={isReady}
              >
                {isReady ? "Esperando al otro jugador..." : "Estoy Listo"}
              </button>
            )}
          </>
        ) : (
          <p className="text-white font-bold">¡El juego ha comenzado!</p>
        )}

        {gameStarted && (
          <button
            onClick={handlePassTurn}
            className={`mt-4 px-4 py-2 rounded text-white transition ${
              isMyTurn
                ? "bg-blue-500 hover:bg-blue-600"
                : "bg-gray-400 cursor-not-allowed"
            }`}
            disabled={!isMyTurn}
          >
            {isMyTurn ? "Pasar Turno" : "No es tu turno"}
          </button>
        )}

        {gameStarted && (
          <div className="grid grid-cols-10 gap-1 border-2 border-primary p-2 bg-dark">
            {grid.map((row, y) => (
              <React.Fragment key={y}>
                {row.map((cell, x) => (
                  <div
                    key={`${x}-${y}`}
                    className={`w-10 h-10 flex items-center justify-center border transition-all duration-300 ${
                      cell.isHit
                        ? "bg-red-500"
                        : cell.hasShip
                        ? "bg-gray-700"
                        : "bg-blue-500 hover:bg-blue-400"
                    }`}
                    onClick={() => handleAttack(x, y)}
                  >
                    {cell.isHit
                      ? "💥"
                      : cell.hasShip && !cell.isHit
                      ? "🚢"
                      : ""}
                  </div>
                ))}
              </React.Fragment>
            ))}
          </div>
        )}
      </div>
    </DndContext>
  );
};

export default GameGrid;
