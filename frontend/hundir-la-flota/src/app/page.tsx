"use client";
import React, { useState } from "react";

const Home: React.FC = () => {
  const [isPlaying, setIsPlaying] = useState(false);

  const startGame = () => {
    setIsPlaying(true);
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-background text-foreground">
      <header className="text-3xl font-bold mb-6">Hundir la Flota</header>

      <section className="w-full max-w-2xl p-4 bg-white shadow-lg rounded-md hover:scale-105 hover:shadow-lg transition-all duration-200">
        <h2 className="text-xl font-semibold mb-4">Instrucciones</h2>
        <p className="mb-4">
          Bienvenido al juego Hundir la Flota. El objetivo del juego es hundir
          todos los barcos del oponente antes de que ellos hundan los tuyos.
          Tienes un tablero con coordenadas donde debes intentar adivinar las
          posiciones de los barcos enemigos.
        </p>
        <div className="mb-4">
          <strong>Cómo jugar:</strong>
          <ul className="list-disc pl-6">
            <li>Cada jugador tiene un tablero con sus barcos dispuestos.</li>
            <li>
              En cada turno, debes seleccionar una coordenada en el tablero del
              oponente para disparar.
            </li>
            <li>
              Si aciertas, el barco se hunde y te queda un turno más. Si fallas,
              le toca al oponente.
            </li>
            <li>
              El juego termina cuando un jugador hunde todos los barcos del
              contrario.
            </li>
          </ul>
        </div>
        <button
          onClick={startGame}
          className="mt-4 bg-primary text-white px-6 py-2 rounded-md shadow-lg hover:bg-secondary transition duration-300"
        >
          Iniciar juego
        </button>
      </section>

      {isPlaying && (
        <section className="mt-8 text-center">
          <h2 className="text-xl font-semibold">¡El juego ha comenzado!</h2>
          <p>Juego comenzado</p>
        </section>
      )}
    </div>
  );
};

export default Home;
