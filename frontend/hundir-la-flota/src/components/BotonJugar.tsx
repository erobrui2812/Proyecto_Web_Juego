"use client";

const BotonJugar = () => (
  <button
    onClick={() => alert("Ir a emparejamiento")}
    className="p-4 bg-primary text-white rounded-md w-full text-center hover:bg-wine"
  >
    Jugar
  </button>
);

export default BotonJugar;
