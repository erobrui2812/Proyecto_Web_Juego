"use client";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

const GameSummary = ({ summary, onRematch }) => {
  const { userDetail } = useAuth();
  const router = useRouter();
  const [mensajeIndividual, setMensajeIndividual] = useState("");
  const [mensajeIndividual2, setMensajeIndividual2] = useState("");

  useEffect(() => {
    if (summary && userDetail) {
      const mensaje =
        userDetail.id === summary
          ? "¡Has ganado!"
          : "¡Has perdido!";
      setMensajeIndividual(mensaje);
      const mensaje2 =
        userDetail.id === summary
          ? "Eres el vencedor, bien jugado."
          : "Sigue jugando, suerte en las siguientes partidas.";
      setMensajeIndividual2(mensaje2);
    }
  }, [summary, userDetail]);

  return (
    <div className="p-6">
      <div className="bg-gray-800 text-white rounded p-4 mb-4">
        <h2 className="text-xl font-bold">Resumen de la partida</h2>
        <p className="text-xl font-bold">
          {mensajeIndividual}
        </p>
        <p>
          {mensajeIndividual2}
        </p>
      </div>

      <button
        onClick={onRematch || (() => router.push("/game/rematch"))}
        className="mt-4 bg-green-500 px-4 py-2 rounded hover:bg-green-600 transition"
      >
        Revancha
      </button>
    </div>
  );
};

export default GameSummary;
