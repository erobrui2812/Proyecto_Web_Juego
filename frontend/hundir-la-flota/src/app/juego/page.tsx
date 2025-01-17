"use client";
import { DndContext, useDraggable } from "@dnd-kit/core";
import { useState } from "react";

const Ship = ({ id }: { id: string }) => {
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
      className="w-16 h-16 bg-primary text-white flex items-center justify-center rounded shadow-md cursor-grab"
    >
      🚢
    </div>
  );
};

const Juego = () => {
  const [dropped, setDropped] = useState(false);

  return (
    <DndContext onDragEnd={() => setDropped(true)}>
      <div className="min-h-screen bg-background text-foreground flex flex-col items-center justify-center">
        <h1 className="text-4xl font-bold mb-6">
          Arrastra tu barco al tablero
        </h1>
        <div className="w-64 h-64 border-4 border-dashed border-secondary flex items-center justify-center">
          {dropped ? (
            <span className="text-lg">Barco colocado</span>
          ) : (
            <Ship id="barco" />
          )}
        </div>
      </div>
    </DndContext>
  );
};

export default Juego;
