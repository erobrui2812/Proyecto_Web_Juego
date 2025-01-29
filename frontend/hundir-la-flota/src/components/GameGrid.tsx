"use client";
import { DndContext } from "@dnd-kit/core";
import React, { useEffect, useState } from "react";
import { useWebsocket } from "../contexts/WebsocketContext";

const shipSizes = [5, 4, 3, 3, 2];

type Ship = {
  x: number;
  y: number;
  size: number;
  orientation: "horizontal" | "vertical";
};

const getRandomOrientation = (): "horizontal" | "vertical" =>
  Math.random() > 0.5 ? "horizontal" : "vertical";

const generateRandomBoard = (): { grid: any[][]; ships: Ship[] } => {
  const newGrid = Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      hasShip: false,
      isHit: false,
      isSunk: false,
    }))
  );

  const ships: Ship[] = [];

  for (const size of shipSizes) {
    let placed = false;

    while (!placed) {
      const x = Math.floor(Math.random() * 10);
      const y = Math.floor(Math.random() * 10);
      const orientation = getRandomOrientation();

      if (orientation === "horizontal") {
        if (x + size > 10) continue;
        if (newGrid[y].slice(x, x + size).some((cell) => cell.hasShip))
          continue;
        for (let i = 0; i < size; i++) newGrid[y][x + i].hasShip = true;
        ships.push({ x, y, size, orientation });
      } else {
        if (y + size > 10) continue;
        if (newGrid.slice(y, y + size).some((row) => row[x].hasShip)) continue;
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

  useEffect(() => {
    if (!socket) return;

    // Unirse a la partida cuando se monta el componente
    sendMessage("joinGame", `${gameId}|${playerId}`);

    const handleYourTurn = () => {
      setIsMyTurn(true);
    };

    const handleMessage = (event) => {
      try {
        const parsedData = JSON.parse(event.data);

        if (!parsedData || typeof parsedData !== "object") {
          console.warn(
            "Mensaje WebSocket recibido, pero no es un objeto JSON:",
            event.data
          );
          return;
        }

        const { type, ...data } = parsedData;

        switch (type) {
          case "YourTurn":
            handleYourTurn();
            break;
          case "ShipsPlaced":
            setShipsPlaced(true);
            break;
          default:
            console.warn("Evento WebSocket no reconocido:", type);
        }
      } catch (error) {
        console.warn("Mensaje WebSocket no es JSON válido:", event.data);
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

    if (!ships.length) {
      console.error("No hay barcos generados, no se puede enviar al servidor.");
      return;
    }

    // Formatear los barcos en el formato esperado por el backend
    const formattedShips = ships
      .map((ship) => `${ship.x},${ship.y},${ship.size},${ship.orientation}`)
      .join(";");

    console.log(
      "Enviando mensaje placeShips:",
      `placeShips|${gameId}|${playerId}|${formattedShips}`
    );

    sendMessage("placeShips", `${gameId}|${playerId}|${formattedShips}`);
  };

  if (!grid) {
    return <div className="text-white">Cargando tablero...</div>;
  }

  return (
    <DndContext>
      <div className="mb-4 flex flex-col items-center">
        <button
          onClick={handlePlaceShips}
          className="bg-green-500 text-white px-4 py-2 rounded hover:bg-green-600 transition"
          disabled={shipsPlaced}
        >
          {shipsPlaced ? "Barcos colocados" : "Colocar Barcos Aleatoriamente"}
        </button>
      </div>

      <div className="grid grid-cols-11 gap-1 border-2 border-primary p-2 bg-dark">
        <div className="w-10 h-10"></div>
        {Array.from({ length: 10 }, (_, i) => (
          <div
            key={`col-${i}`}
            className="w-10 h-10 flex items-center justify-center text-white font-bold"
          >
            {String.fromCharCode(65 + i)}
          </div>
        ))}

        {grid.map((row, y) => (
          <React.Fragment key={y}>
            <div className="w-10 h-10 flex items-center justify-center text-white font-bold">
              {y + 1}
            </div>

            {row.map((cell, x) => (
              <div
                key={`${x}-${y}`}
                className={`w-10 h-10 flex items-center justify-center border transition-all duration-300 ${
                  cell.hasShip
                    ? "bg-gray-700 border border-gray-500"
                    : "bg-blue-500 hover:bg-blue-400 transition"
                }`}
              >
                {cell.hasShip ? "🚢" : ""}
              </div>
            ))}
          </React.Fragment>
        ))}
      </div>
    </DndContext>
  );
};

export default GameGrid;
