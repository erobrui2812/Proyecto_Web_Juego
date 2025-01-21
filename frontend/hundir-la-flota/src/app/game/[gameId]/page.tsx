"use client";

import { useState } from "react";
import { DndProvider, useDrag, useDrop } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";

const CELL_SIZE = 40;
const GRID_SIZE = 10;

// Configuración de los barcos
const shipConfig = [
  { id: "1", length: 4, name: "Acorazado", count: 1 },
  { id: "2", length: 3, name: "Crucero", count: 2 },
  { id: "3", length: 2, name: "Destructor", count: 3 },
  { id: "4", length: 1, name: "Submarino", count: 4 },
];

const Cell = ({ x, y, status, onDrop, onClick }) => {
  const [{ isOver }, drop] = useDrop(() => ({
    accept: "SHIP",
    drop: (item) => onDrop && onDrop(item, x, y),
    collect: (monitor) => ({
      isOver: !!monitor.isOver(),
    }),
  }));

  const getCellStyle = () => {
    if (status === "ship") return "bg-gray-500";
    if (status === "hit") return "bg-red-500";
    if (status === "miss") return "bg-blue-500";
    return "bg-white";
  };

  return (
    <div
      ref={drop}
      onClick={onClick}
      className={`border border-black ${getCellStyle()}`}
      style={{
        width: CELL_SIZE,
        height: CELL_SIZE,
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        backgroundColor: isOver ? "lightgreen" : "",
        cursor: onClick ? "pointer" : "default",
      }}
    ></div>
  );
};

const Ship = ({ id, length, orientation, onRotate }) => {
  const [{ isDragging }, drag] = useDrag(() => ({
    type: "SHIP",
    item: { id, length, orientation },
    collect: (monitor) => ({
      isDragging: !!monitor.isDragging(),
    }),
  }));

  return (
    <div
      ref={drag}
      style={{
        width: orientation === "horizontal" ? length * CELL_SIZE : CELL_SIZE,
        height: orientation === "horizontal" ? CELL_SIZE : length * CELL_SIZE,
        backgroundColor: "gray",
        opacity: isDragging ? 0.5 : 1,
        cursor: "grab",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        color: "white",
      }}
      onDoubleClick={() => onRotate(id)}
    >
      🚢
    </div>
  );
};

const Board = ({ boardState, onDrop, onCellClick }) => {
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: `repeat(${GRID_SIZE}, ${CELL_SIZE}px)`,
        gridTemplateRows: `repeat(${GRID_SIZE}, ${CELL_SIZE}px)`,
        gap: 2,
      }}
    >
      {boardState.map((row, y) =>
        row.map((cell, x) => (
          <Cell
            key={`${x}-${y}`}
            x={x}
            y={y}
            status={cell}
            onDrop={onDrop}
            onClick={() => onCellClick && onCellClick(x, y)}
          />
        ))
      )}
    </div>
  );
};

const GamePage = ({ params }) => {
  const [playerBoard, setPlayerBoard] = useState(
    Array(GRID_SIZE)
      .fill(null)
      .map(() => Array(GRID_SIZE).fill(null))
  );

  const [opponentBoard, setOpponentBoard] = useState(
    Array(GRID_SIZE)
      .fill(null)
      .map(() => Array(GRID_SIZE).fill(null))
  );

  const [placedShips, setPlacedShips] = useState([]);
  const [shipOrientations, setShipOrientations] = useState(
    shipConfig.reduce((acc, ship) => ({ ...acc, [ship.id]: "horizontal" }), {})
  );
  const [isPlayerTurn, setIsPlayerTurn] = useState(true);
  const [message, setMessage] = useState("Coloca tus barcos para empezar.");
  const [remainingShips, setRemainingShips] = useState(
    shipConfig.reduce((acc, ship) => ({ ...acc, [ship.id]: ship.count }), {})
  );

  const validatePlacement = (item, x, y) => {
    const orientation = shipOrientations[item.id];
    for (let i = 0; i < item.length; i++) {
      const nx = orientation === "horizontal" ? x + i : x;
      const ny = orientation === "vertical" ? y + i : y;

      if (nx >= GRID_SIZE || ny >= GRID_SIZE || playerBoard[ny][nx] !== null) {
        return false;
      }

      const neighbors = [
        [nx - 1, ny - 1],
        [nx, ny - 1],
        [nx + 1, ny - 1],
        [nx - 1, ny],
        [nx + 1, ny],
        [nx - 1, ny + 1],
        [nx, ny + 1],
        [nx + 1, ny + 1],
      ];

      if (
        neighbors.some(
          ([cx, cy]) =>
            cx >= 0 &&
            cy >= 0 &&
            cx < GRID_SIZE &&
            cy < GRID_SIZE &&
            playerBoard[cy][cx] === "ship"
        )
      ) {
        return false;
      }
    }
    return true;
  };

  const handleDrop = (item, x, y) => {
    if (remainingShips[item.id] <= 0) {
      setMessage(`Ya colocaste todos tus ${item.name}s.`);
      return;
    }

    if (!validatePlacement(item, x, y)) {
      setMessage("No se puede colocar el barco aquí.");
      return;
    }

    const orientation = shipOrientations[item.id];
    const newBoard = [...playerBoard];

    for (let i = 0; i < item.length; i++) {
      const nx = orientation === "horizontal" ? x + i : x;
      const ny = orientation === "vertical" ? y + i : y;
      newBoard[ny][nx] = "ship";
    }

    setPlayerBoard(newBoard);
    setPlacedShips((prev) => [...prev, { ...item, x, y, orientation }]);
    setRemainingShips((prev) => ({
      ...prev,
      [item.id]: prev[item.id] - 1,
    }));
    setMessage("Barco colocado. Coloca el siguiente.");
  };

  const checkWinCondition = () => {
    const allPlayerShipsSunk = playerBoard.every((row) =>
      row.every((cell) => cell !== "ship")
    );
    const allBotShipsSunk = opponentBoard.every((row) =>
      row.every((cell) => cell !== "ship")
    );

    if (allPlayerShipsSunk) {
      setMessage("¡Perdiste! El bot hundió todos tus barcos.");
      return true;
    }

    if (allBotShipsSunk) {
      setMessage("¡Ganaste! Hundiste todos los barcos del bot.");
      return true;
    }

    return false;
  };

  const handleAttack = (x, y) => {
    if (!isPlayerTurn || opponentBoard[y][x] !== null) return;

    const newBoard = [...opponentBoard];
    const hit = Math.random() > 0.5;

    newBoard[y][x] = hit ? "hit" : "miss";
    setOpponentBoard(newBoard);

    if (checkWinCondition()) return;

    setMessage(
      hit ? "¡Acierto! Dispara de nuevo." : "Fallaste. Turno del bot."
    );

    if (!hit) {
      setIsPlayerTurn(false);
      setTimeout(() => {
        botAttack();
      }, 1000);
    }
  };

  const botAttack = () => {
    const newBoard = [...playerBoard];
    let x, y;

    do {
      x = Math.floor(Math.random() * GRID_SIZE);
      y = Math.floor(Math.random() * GRID_SIZE);
    } while (newBoard[y][x] === "hit" || newBoard[y][x] === "miss");

    const hit = newBoard[y][x] === "ship";
    newBoard[y][x] = hit ? "hit" : "miss";

    setPlayerBoard(newBoard);

    if (checkWinCondition()) return;

    setMessage(
      hit ? "El bot acertó. Dispara de nuevo." : "El bot falló. Tu turno."
    );

    if (!hit) {
      setIsPlayerTurn(true);
    } else {
      setTimeout(() => {
        botAttack();
      }, 1000);
    }
  };

  const handleRotateShip = (id) => {
    setShipOrientations((prev) => ({
      ...prev,
      [id]: prev[id] === "horizontal" ? "vertical" : "horizontal",
    }));
  };

  return (
    <DndProvider backend={HTML5Backend}>
      <div className="p-4">
        <h1 className="text-xl font-bold">Hundir la Flota</h1>
        <p>{message}</p>

        <div className="flex gap-4 mt-4">
          <div>
            <h2 className="text-lg font-semibold">Tu tablero</h2>
            <Board boardState={playerBoard} onDrop={handleDrop} />
          </div>

          <div>
            <h2 className="text-lg font-semibold">Tablero del bot</h2>
            <Board boardState={opponentBoard} onCellClick={handleAttack} />
          </div>
        </div>

        <div className="mt-4">
          <h2 className="text-lg font-semibold">Barcos</h2>
          <div className="flex flex-col gap-2">
            {shipConfig.map((ship) => (
              <Ship
                key={ship.id}
                id={ship.id}
                length={ship.length}
                orientation={shipOrientations[ship.id]}
                onRotate={handleRotateShip}
              />
            ))}
          </div>
        </div>
      </div>
    </DndProvider>
  );
};

export default GamePage;
