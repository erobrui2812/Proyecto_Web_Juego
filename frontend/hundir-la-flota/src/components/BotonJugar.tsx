"use client";

const BotonJugar = () => (
  <button
    onClick={() => alert("Ir a emparejamiento")}
    className="bg-primary p-4 border-2 border-gold text-white rounded-md w-full text-center hover:bg-wine"
  >
    Jugar
  </button>
);

export default BotonJugar;
