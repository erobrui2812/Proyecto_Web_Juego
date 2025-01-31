import React from "react";

const Home: React.FC = () => {
  return (
    <div className="relative min-h-screen flex flex-col items-center justify-center bg-fondo-mar bg-cover bg-center text-white font-montserrat">
      <div className="absolute inset-0 bg-black bg-opacity-10"></div>
      <div className="relative z-10">
        <section className="w-full max-w-3xl p-6 bg-opacity-80 bg-gray-800 text-gold shadow-lg rounded-md :scale-105 transition-all duration-200 border-2 border-gold">
          <h1 className="text-5xl font-extrabold mb-6 text-center font-bebasneue text-gold">
            Hundir la Flota
          </h1>
          <div className="flex flex-col md:flex-row items-center gap-6">
            <img
              src="./tablero-battleship.webp"
              alt="Ejemplo de tablero"
              className="w-full md:w-1/2 rounded-md border-2 border-primary"
            />
            <div className="p-4 bg-black bg-opacity-60 text-white rounded-md shadow-lg">
              <p className="mb-4">
                Bienvenido al juego Hundir la Flota. El objetivo es hundir todos
                los barcos del oponente antes de que ellos hundan los tuyos.
              </p>
              <strong className="text-xl font-bebasneue text-gold">
                Cómo jugar:
              </strong>
              <ul className="list-disc pl-6">
                <li>
                  Cada jugador tiene un tablero con sus barcos dispuestos.
                </li>
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
        </section>
      </div>
    </div>
  );
};

export default Home;
