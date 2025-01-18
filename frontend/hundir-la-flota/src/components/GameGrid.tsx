"use client";

import { DndContext, useDraggable, useDroppable } from "@dnd-kit/core";
import { useState } from "react";

const GameGrid = () => {
  const [grid, setGrid] = useState(
    Array.from({ length: 10 }, () =>
      Array.from({ length: 10 }, () => ({ hasShip: false, isOver: false }))
    )
  );

  const handleDragEnd = (event: any) => {
    const { over } = event;
    if (over) {
      const [x, y] = over.id.split("-").map(Number);
      setGrid((prevGrid) => {
        const newGrid = [...prevGrid];
        newGrid[y][x] = { ...newGrid[y][x], hasShip: true, isOver: false };
        return newGrid;
      });
    }
  };

  return (
    <DndContext onDragEnd={handleDragEnd}>
      <div className="grid grid-cols-10 gap-1 border-2 border-primary p-2 bg-gray-900">
        {grid.map((row, y) =>
          row.map((cell, x) => (
            <BoardSquare
              key={`${x}-${y}`}
              id={`${x}-${y}`}
              hasShip={cell.hasShip}
            />
          ))
        )}
      </div>
      <div className="mt-4">
        <DraggableShip id="ship-1" />
      </div>
    </DndContext>
  );
};

const BoardSquare = ({ id, hasShip }: { id: string; hasShip: boolean }) => {
  const { setNodeRef, isOver } = useDroppable({ id });

  const style = isOver
    ? "bg-green-400 border-green-500"
    : hasShip
    ? "bg-primary border-primary"
    : "bg-gray-800 border-gray-600";

  return (
    <div
      ref={setNodeRef}
      className={`${style} w-10 h-10 flex items-center justify-center border-2 rounded`}
    >
      {hasShip && "🚢"}
    </div>
  );
};

const DraggableShip = ({ id }: { id: string }) => {
  const { attributes, listeners, setNodeRef, transform } = useDraggable({ id });

  const style = transform
    ? { transform: `translate(${transform.x}px, ${transform.y}px)` }
    : undefined;

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...listeners}
      {...attributes}
      className="w-32 h-8 bg-blue-500 text-white flex items-center justify-center rounded shadow-lg cursor-grab"
    >
      🚢🚢🚢🚢
    </div>
  );
};

export default GameGrid;
