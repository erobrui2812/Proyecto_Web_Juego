"use client";
import FinalStatsModal from "@/components/FinalStatsModal";
import { useAuth } from "@/contexts/AuthContext";
import { DndContext } from "@dnd-kit/core";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { useWebsocket } from "../contexts/WebsocketContext";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

const shipSizes = [5, 4, 3, 3, 2];

const generateEmptyGrid = () => {
  return Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      hasShip: false,
      isHit: false,
      isSunk: false,
      attacked: false,
    }))
  );
};

const generateRandomBoard = () => {
  const newGrid = generateEmptyGrid();
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

        for (let i = 0; i < size; i++) {
          newGrid[y][x + i].hasShip = true;
        }
        ships.push({ x, y, size, orientation });
        placed = true;
      } else {
        if (
          y + size > 10 ||
          newGrid.slice(y, y + size).some((row) => row[x].hasShip)
        )
          continue;

        for (let i = 0; i < size; i++) {
          newGrid[y + i][x].hasShip = true;
        }
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

const generateEmptyOpponentBoard = () => {
  return Array.from({ length: 10 }, () =>
    Array.from({ length: 10 }, () => ({
      attacked: false,
      result: null,
    }))
  );
};

const GameGrid = ({ gameId, playerId }) => {
  const { socket, sendMessage } = useWebsocket();
  const { auth } = useAuth();
  const router = useRouter();

  const [manualPlacementMode, setManualPlacementMode] = useState(false);
  const [manualGrid, setManualGrid] = useState(generateEmptyGrid());
  const [shipsToPlace, setShipsToPlace] = useState([...shipSizes]);
  const [orientation, setOrientation] = useState("horizontal");
  const [shipPositions, setShipPositions] = useState([]);

  const [myBoard, setMyBoard] = useState(generateRandomBoard());
  const [opponentBoard, setOpponentBoard] = useState(
    generateEmptyOpponentBoard()
  );
  const [isMyTurn, setIsMyTurn] = useState(false);
  const [shipsPlaced, setShipsPlaced] = useState(false);
  const [gameStarted, setGameStarted] = useState(false);
  const [isReady, setIsReady] = useState(false);
  const [gameOver, setGameOver] = useState(false);
  const [gameOverMessage, setGameOverMessage] = useState("");
  const [gameSummary, setGameSummary] = useState(null);
  const [showFinalStatsModal, setShowFinalStatsModal] = useState(false);

  const handleCloseModal = () => setShowFinalStatsModal(false);

  useEffect(() => {
    if (gameOver) {
      setShowFinalStatsModal(true);
    }
  }, [gameOver]);

  useEffect(() => {
    if (!socket) return;
    sendMessage("joinGame", `${gameId}|${playerId}`);

    const handleMessage = (event) => {
      try {
        const parts = event.data.split("|");
        const type = parts[0];
        const payload = parts.slice(1).join("|");

        // console.log(`Evento recibido: ${type}`, payload);

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

  const handlePlaceShipsRandomly = () => {
    const newBoard = generateRandomBoard();
    setMyBoard(newBoard);

    if (!newBoard.ships.length) return;

    const formattedShips = newBoard.ships
      .map((ship) => `${ship.x},${ship.y},${ship.size},${ship.orientation}`)
      .join(";");

    sendMessage("placeShips", `${gameId}|${playerId}|${formattedShips}`);
    setShipsPlaced(true);
  };

  const toggleManualPlacement = () => {
    setManualPlacementMode((prev) => !prev);
    if (manualPlacementMode) {
      setManualGrid(generateEmptyGrid());
      setShipsToPlace([...shipSizes]);
      setShipPositions([]);
    }
  };

  const canPlaceShip = (grid, x, y, size, orientation) => {
    if (orientation === "horizontal") {
      if (x + size > 10) return false;
      for (let i = 0; i < size; i++) {
        if (grid[y][x + i].hasShip) return false;
      }
    } else {
      if (y + size > 10) return false;
      for (let i = 0; i < size; i++) {
        if (grid[y + i][x].hasShip) return false;
      }
    }
    return true;
  };

  const placeShipOnGrid = (grid, x, y, size, orientation) => {
    const newGrid = grid.map((row) => row.map((cell) => ({ ...cell })));
    if (orientation === "horizontal") {
      for (let i = 0; i < size; i++) {
        newGrid[y][x + i].hasShip = true;
      }
    } else {
      for (let i = 0; i < size; i++) {
        newGrid[y + i][x].hasShip = true;
      }
    }
    return newGrid;
  };

  const handleManualCellClick = (x, y) => {
    if (!shipsToPlace.length) return;

    const size = shipsToPlace[0];
    if (canPlaceShip(manualGrid, x, y, size, orientation)) {
      const updatedGrid = placeShipOnGrid(manualGrid, x, y, size, orientation);
      setManualGrid(updatedGrid);

      const newShipPositions = [...shipPositions, { x, y, size, orientation }];
      setShipPositions(newShipPositions);

      const newShipsToPlace = shipsToPlace.slice(1);
      setShipsToPlace(newShipsToPlace);
    } else {
      alert(
        "No se puede colocar el barco aquí. Intenta otra posición u orientación."
      );
    }
  };

  const handleConfirmManualShips = () => {
    const manualBoardData = {
      grid: manualGrid,
      ships: shipPositions,
    };
    setMyBoard(manualBoardData);

    const formattedShips = shipPositions
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

    setOpponentBoard((prevBoard) => {
      const newBoard = prevBoard.map((row) => row.slice());
      newBoard[y][x] = { attacked: true, result: attackOutcome };
      return newBoard;
    });

    if (attackOutcome === "hit" || attackOutcome === "sunk") {
      setIsMyTurn(true);
    } else {
      setIsMyTurn(false);
    }
  };

  const handleEnemyAttack = (payload) => {
    const attackData =
      typeof payload === "string" ? JSON.parse(payload) : payload;
    const { x, y, result: attackOutcome } = attackData;

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
    if (!auth?.token) return;
    const res = await fetch(`${API_URL}api/game/rematch`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.token}`,
      },
      body: JSON.stringify({ GameId: gameId, PlayerId: playerId }),
    });
    if (res.ok) {
      const data = await res.json();
      router.push(`/game/${data.gameId}`);
    } else {
      console.error("Error en rematch", res.status);
    }
  };

  const invertBoard = (boardArray) => boardArray.slice().reverse();

  return (
    <DndContext>
      <div className="mb-4 flex flex-col items-center space-y-4">
        {!gameStarted ? (
          <>
            <div className="flex space-x-4">
              <button
                onClick={toggleManualPlacement}
                className={`btn-primary px-4 py-2 rounded-lg shadow transition-colors ${
                  manualPlacementMode ? "bg-red-500" : ""
                }`}
              >
                {manualPlacementMode
                  ? "Salir Modo Manual"
                  : "Entrar Modo Manual"}
              </button>

              {!manualPlacementMode && (
                <button
                  onClick={handlePlaceShipsRandomly}
                  className="btn-primary px-4 py-2 rounded-lg shadow transition-colors disabled:opacity-50"
                  disabled={shipsPlaced}
                >
                  {shipsPlaced
                    ? "Barcos colocados (Aleatorio)"
                    : "Colocar Barcos Aleatoriamente"}
                </button>
              )}
            </div>

            {manualPlacementMode && (
              <div className="w-full max-w-md p-4 bg-gray-800 rounded-lg shadow mt-4">
                <div className="flex justify-between mb-2">
                  <span className="text-[var(--foreground)]">
                    Barcos restantes:
                  </span>
                  <span className="text-[var(--foreground)] font-bold">
                    {shipsToPlace.join(", ")}
                  </span>
                </div>

                <div className="flex space-x-4 mb-4">
                  <button
                    className={`px-4 py-2 rounded-lg text-[var(--foreground)] shadow transition-colors ${
                      orientation === "horizontal"
                        ? "bg-blue-600"
                        : "bg-gray-700 hover:bg-gray-600"
                    }`}
                    onClick={() => setOrientation("horizontal")}
                  >
                    Horizontal
                  </button>
                  <button
                    className={`px-4 py-2 rounded-lg text-[var(--foreground)] shadow transition-colors ${
                      orientation === "vertical"
                        ? "bg-blue-600"
                        : "bg-gray-700 hover:bg-gray-600"
                    }`}
                    onClick={() => setOrientation("vertical")}
                  >
                    Vertical
                  </button>
                </div>

                <div className="grid grid-cols-10 gap-1 border-2 border-[var(--primary)] p-2 bg-dark rounded-lg shadow">
                  {invertBoard(manualGrid).map((row, displayedY) => {
                    const actualY = manualGrid.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            onClick={() => handleManualCellClick(x, actualY)}
                            className={`w-8 sm:w-10 h-8 sm:h-10 md:w-12 md:h-12 flex items-center justify-center border rounded-md transition-transform duration-300 cursor-pointer ${
                              cell.hasShip
                                ? "bg-gray-700"
                                : "bg-blue-500 hover:scale-105"
                            }`}
                          >
                            {cell.hasShip ? "🚢" : ""}
                          </div>
                        ))}
                      </React.Fragment>
                    );
                  })}
                </div>

                <div className="mt-4">
                  {!shipsPlaced && shipsToPlace.length === 0 && (
                    <button
                      onClick={handleConfirmManualShips}
                      className="btn-primary px-4 py-2 rounded-lg shadow transition-colors w-full"
                    >
                      Confirmar Barcos Manuales
                    </button>
                  )}
                  {shipsPlaced && (
                    <p className="text-green-400 text-center mt-2">
                      ¡Barcos colocados manualmente!
                    </p>
                  )}
                </div>
              </div>
            )}

            {shipsPlaced && (
              <button
                onClick={handleConfirmReady}
                className="btn-secondary px-4 py-2 rounded-lg shadow transition-colors disabled:opacity-50 mt-4"
                disabled={isReady}
              >
                {isReady ? "Esperando al otro jugador..." : "Estoy Listo"}
              </button>
            )}

            {shipsPlaced && (
              <div className="w-full max-w-md mt-4">
                <p className="text-[var(--foreground)] mb-2 text-center">
                  Tus barcos:
                </p>
                <div className="grid grid-cols-10 gap-1 border-2 border-[var(--primary)] p-2 bg-dark rounded-lg shadow">
                  {invertBoard(myBoard.grid).map((row, displayedY) => {
                    const actualY = myBoard.grid.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            className={`w-8 sm:w-10 h-8 sm:h-10 md:w-12 md:h-12 flex items-center justify-center border rounded-md transition-transform duration-300 ${
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
          <p className="text-[var(--foreground)] font-bold">
            ¡El juego ha comenzado!
          </p>
        )}
        {gameStarted && !gameOver && (
          <>
            <button
              onClick={handlePassTurn}
              className={`px-4 py-2 rounded-lg text-[var(--foreground)] shadow transition-colors ${
                isMyTurn
                  ? "bg-blue-500 hover:bg-blue-600"
                  : "bg-gray-400 cursor-not-allowed"
              }`}
              disabled={!isMyTurn}
            >
              {isMyTurn ? "Pasar Turno" : "No es tu turno"}
            </button>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 w-full">
              <div>
                <p className="text-[var(--foreground)] font-bold mb-2 text-center">
                  Tu Tablero
                </p>
                <div className="grid grid-cols-10 gap-1 border-2 border-[var(--primary)] p-2 bg-dark rounded-lg shadow">
                  {invertBoard(myBoard.grid).map((row, displayedY) => {
                    const actualY = myBoard.grid.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            className={`w-8 sm:w-10 h-8 sm:h-10 md:w-12 md:h-12 flex items-center justify-center border rounded-md transition-transform duration-300 ${
                              cell.attacked
                                ? cell.hasShip
                                  ? "bg-red-500"
                                  : "bg-gray-400"
                                : cell.hasShip
                                ? "bg-gray-700"
                                : "bg-blue-500 hover:scale-105 cursor-pointer"
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
                <p className="text-[var(--foreground)] font-bold mb-2 text-center">
                  Tablero del Rival
                </p>
                <div className="grid grid-cols-10 gap-1 border-2 border-[var(--primary)] p-2 bg-dark rounded-lg shadow">
                  {invertBoard(opponentBoard).map((row, displayedY) => {
                    const actualY = opponentBoard.length - 1 - displayedY;
                    return (
                      <React.Fragment key={actualY}>
                        {row.map((cell, x) => (
                          <div
                            key={`${x}-${actualY}`}
                            onClick={() => handleAttack(x, actualY)}
                            className={`w-8 sm:w-10 h-8 sm:h-10 md:w-12 md:h-12 flex items-center justify-center border rounded-md transition-transform duration-300 cursor-pointer ${
                              !cell.attacked
                                ? "bg-blue-500 hover:bg-blue-400 hover:scale-105"
                                : cell.result === "pending"
                                ? "bg-yellow-500"
                                : cell.result === "miss"
                                ? "bg-gray-400"
                                : cell.result === "hit" ||
                                  cell.result === "sunk"
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
          <div className="mt-4 p-4 bg-gray-800 text-[var(--foreground)] rounded-lg shadow w-full max-w-lg">
            {gameSummary ? (
              <>
                {showFinalStatsModal && (
                  <FinalStatsModal
                    isOpen={showFinalStatsModal}
                    onClose={handleCloseModal}
                    summary={
                      gameSummary || {
                        winner: gameOverMessage,
                        totalTurns: 0,
                        shipsRemaining: "N/A",
                      }
                    }
                    onRematch={handleRematch}
                  />
                )}
              </>
            ) : (
              <>
                <p>{gameOverMessage}</p>
                <button
                  onClick={handleRematch}
                  className="btn-primary px-4 py-2 rounded-lg shadow transition-colors mt-4"
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
