"use client";
import React, { useState } from "react";

const Home: React.FC = () => {
  const [isPlaying, setIsPlaying] = useState(false);

  const startGame = () => {
    setIsPlaying(true);
  };

  return (
    <div className="relative min-h-screen flex flex-col items-center justify-center bg-fondo-mar bg-cover bg-center text-white font-montserrat">
      <div className="absolute inset-0 bg-black bg-opacity-10"></div>
      <div className="relative z-10">
        <section className="w-full max-w-3xl p-6 bg-white text-black shadow-lg rounded-md hover:scale-105 hover:shadow-2xl transition-all duration-200">
        <h1 className="text-5xl font-extrabold mb-6 text-center  font-bebasneue">
          Hundir la Flota
        </h1>
          <div className="flex flex-col md:flex-row items-center gap-6">
            <img
              src="./tablero-battleship.png"
              alt="Ejemplo de tablero"
              className="w-full md:w-1/2 rounded-md"
            />
            <div>
              <p className="mb-4">
                Bienvenido al juego Hundir la Flota. El objetivo es hundir todos
                los barcos del oponente antes de que ellos hundan los tuyos.
              </p>
              <strong className="text-xl font-bebasneue">Cómo jugar:</strong>
              <ul className="list-disc pl-6">
                <li>Cada jugador tiene un tablero con sus barcos dispuestos.</li>
                <li>
                  En cada turno, selecciona una coordenada en el tablero del
                  oponente para disparar.
                </li>
                <li>
                  Si aciertas, el barco se daña y puedes disparar de nuevo.
                </li>
                <li>
                  Gana quien hunda todos los barcos del contrario primero.
                </li>
              </ul>
            </div>
          </div>
          <button
            onClick={startGame}
            className="mt-6 w-full bg-blue-600 text-white px-6 py-3 rounded-md shadow-lg hover:bg-blue-700 transition duration-300  font-bebasneue text-xl"
          >
            Iniciar juego
          </button>
        </section>

        {isPlaying && (
          <section className="mt-8 text-center">
            <h2 className="text-2xl font-semibold">¡El juego ha comenzado!</h2>
            <p>Buena suerte, capitán.</p>
          </section>
        )}
      </div>
    </div>
  );
};

export default Home;
