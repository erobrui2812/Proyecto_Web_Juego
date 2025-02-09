"use client";
import { DndContext } from "@dnd-kit/core";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { useWebsocket } from "../contexts/WebsocketContext";
import GameSummary from "./GameSummary";

const shipSizes = [5, 4, 3, 3, 2];

const generateRandomBoard = () => {
  const newGrid = Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      hasShip: false,
      isHit: false,
      isSunk: false,
      attacked: false,
    }))
  );
  const ships = [];
  for (const size of shipSizes) {
    let placed = false;
    let attempts = 0;
    while (!placed && attempts < 1000) {
      attempts++;
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
        placed = true;
      } else {
        if (
          y + size > 10 ||
          newGrid.slice(y, y + size).some((row) => row[x].hasShip)
        )
          continue;
        for (let i = 0; i < size; i++) newGrid[y + i][x].hasShip = true;
        ships.push({ x, y, size, orientation });
        placed = true;
      }
    }
    if (!placed) {
      console.error(
        `No se pudo colocar el barco de tamaño ${size} después de muchos intentos.`
      );
    }
  }
  return { grid: newGrid, ships };
};

const generateEmptyBoard = () => {
  return Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      attacked: false,
      result: null, 
    }))
  );
};

const GameGrid = ({ gameId, playerId }) => {
  const { socket, sendMessage } = useWebsocket();
  const [myBoard, setMyBoard] = useState(generateRandomBoard());
  const [opponentBoard, setOpponentBoard] = useState(generateEmptyBoard());
  const [isMyTurn, setIsMyTurn] = useState(false);
  const [shipsPlaced, setShipsPlaced] = useState(false);
  const [gameStarted, setGameStarted] = useState(false);
  const [isReady, setIsReady] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [gameOverMessage, setGameOverMessage] = useState("");
  const [gameSummary, setGameSummary] = useState(null);
  const router = useRouter();

  useEffect(() => {
    if (!socket) return;
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
          case "EnemyAttack":
            handleEnemyAttack(payload);
            break;
          case "GameOver":
            setGameOver(true);
            try {
              const summary = JSON.parse(payload);
              setGameSummary(summary);
            } catch (error) {
              setGameOverMessage(payload);
            }
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
    const newBoard = generateRandomBoard();
    setMyBoard(newBoard);
    if (!newBoard.ships.length) return;
    const formattedShips = newBoard.ships
      .map((ship) => `${ship.x},${ship.y},${ship.size},${ship.orientation}`)
      .join(";");
    sendMessage("placeShips", `${gameId}|${playerId}|${formattedShips}`);
    setShipsPlaced(true);
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
    if (!isMyTurn) return;
    if (opponentBoard[y][x].attacked) return;
    setOpponentBoard((prevBoard) => {
      const newBoard = prevBoard.map((row) => row.slice());
      newBoard[y][x] = { attacked: true, result: "pending" };
      return newBoard;
    });
    sendMessage("Attack", `${gameId}|${x}|${y}`);
  };

  const handleAttackResult = (result) => {
    const attackData = typeof result === "string" ? JSON.parse(result) : result;
    const { x, y, result: attackOutcome } = attackData;
    console.log("Actualizando ataque result en coordenadas reales:", x, y, attackOutcome);
    setOpponentBoard((prevBoard) => {
      const newBoard = prevBoard.map((row) => row.slice());
      newBoard[y][x] = { attacked: true, result: attackOutcome };
      return newBoard;
    });
    if (attackOutcome === "hit" || attackOutcome === "sunk") {
      console.log("Ataque exitoso. Continúa tu turno.");
      setIsMyTurn(true);
    } else {
      console.log("Ataque fallido. Fin del turno.");
      setIsMyTurn(false);
    }
  };

  const handleEnemyAttack = (payload) => {
    const attackData = typeof payload === "string" ? JSON.parse(payload) : payload;
    const { x, y, result: attackOutcome } = attackData;
    console.log("Recibiendo ataque enemigo en coordenadas reales:", x, y, attackOutcome);
    setMyBoard((prevBoard) => {
      const newGrid = prevBoard.grid.map((row) => row.slice());
      const cell = newGrid[y][x];
      if (cell.hasShip) {
        newGrid[y][x] = { ...cell, attacked: true, isHit: true };
      } else {
        newGrid[y][x] = { ...cell, attacked: true };
      }
      return { ...prevBoard, grid: newGrid };
    });
  };

  const handleRematch = async () => {
    const res = await fetch("https://localhost:7162/api/game/rematch", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ GameId: gameId, PlayerId: playerId }),
    });
    if (res.ok) {
      const data = await res.json();
      router.push(`/game/${data.gameId}`);
    }
  };

  const invertBoard = (boardArray) => boardArray.slice().reverse();

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
            {shipsPlaced && (
              <div className="mt-4">
                <p className="text-white mb-2">Tus barcos:</p>
                <div className="grid grid-cols-10 gap-1 border-2 border-primary p-2 bg-dark">
                  {invertBoard(myBoard.grid).map((row, displayedY) => {
                    const actualY = myBoard.grid.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            className={`w-10 h-10 flex items-center justify-center border transition-all duration-300 ${
                              cell.attacked
                                ? cell.hasShip
                                  ? "bg-red-500"
                                  : "bg-gray-400"
                                : cell.hasShip
                                ? "bg-gray-700"
                                : "bg-blue-500"
                            }`}
                          >
                            {cell.attacked && cell.hasShip
                              ? "💥"
                              : cell.hasShip
                              ? "🚢"
                              : ""}
                          </div>
                        ))}
                      </React.Fragment>
                    );
                  })}
                </div>
              </div>
            )}
          </>
        ) : (
          <p className="text-white font-bold">¡El juego ha comenzado!</p>
        )}

        {gameStarted && !gameOver && (
          <>
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
            <div className="mt-4 flex space-x-4">
              <div>
                <p className="text-white font-bold mb-2">Tu Tablero</p>
                <div className="grid grid-cols-10 gap-1 border-2 border-primary p-2 bg-dark">
                  {invertBoard(myBoard.grid).map((row, displayedY) => {
                    const actualY = myBoard.grid.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            className={`w-10 h-10 flex items-center justify-center border transition-all duration-300 ${
                              cell.attacked
                                ? cell.hasShip
                                  ? "bg-red-500"
                                  : "bg-gray-400"
                                : cell.hasShip
                                ? "bg-gray-700"
                                : "bg-blue-500"
                            }`}
                          >
                            {cell.attacked && cell.hasShip
                              ? "💥"
                              : cell.hasShip
                              ? "🚢"
                              : ""}
                          </div>
                        ))}
                      </React.Fragment>
                    );
                  })}
                </div>
              </div>

              <div>
                <p className="text-white font-bold mb-2">
                  Tablero del Rival
                </p>
                <div className="grid grid-cols-10 gap-1 border-2 border-primary p-2 bg-dark">
                  {invertBoard(opponentBoard).map((row, displayedY) => {
                    const actualY = opponentBoard.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            onClick={() => handleAttack(x, actualY)}
                            className={`w-10 h-10 flex items-center justify-center border transition-all duration-300 cursor-pointer ${
                              !cell.attacked
                                ? "bg-blue-500 hover:bg-blue-400"
                                : cell.result === "pending"
                                ? "bg-yellow-500"
                                : cell.result === "miss"
                                ? "bg-gray-400"
                                : cell.result === "hit" || cell.result === "sunk"
                                ? "bg-red-500"
                                : "bg-blue-500"
                            }`}
                          >
                            {cell.attacked &&
                            (cell.result === "hit" || cell.result === "sunk")
                              ? "💥"
                              : cell.attacked && cell.result === "miss"
                              ? "⭕"
                              : ""}
                          </div>
                        ))}
                      </React.Fragment>
                    );
                  })}
                </div>
              </div>
            </div>
          </>
        )}

        {gameOver && (
          <div className="mt-4 p-4 bg-gray-800 text-white rounded">
            {gameSummary ? (
              <GameSummary summary={gameSummary} />
            ) : (
              <>
                <p>{gameOverMessage}</p>
                <button
                  onClick={handleRematch}
                  className="mt-2 bg-green-500 px-4 py-2 rounded hover:bg-green-600 transition"
                >
                  Revancha
                </button>
              </>
            )}
          </div>
        )}
      </div>
    </DndContext>
  );
};

export default GameGrid;