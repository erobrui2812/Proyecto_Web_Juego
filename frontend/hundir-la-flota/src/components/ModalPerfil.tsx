"use client";

import React, { useEffect, useState } from "react";
import Modal from "@/components/Modal";
import { useFriendship } from "@/contexts/FriendshipContext";
import { useAuth } from "@/contexts/AuthContext";

type ModalPerfilProps = {
  isOpen: boolean;
  onClose: () => void;
  userId: string | null;
};

type UserProfile = {
  nickname: string;
  avatarUrl: string;
  email: string;
};

type GameHistory = {
  gameId: string;
  player1Nickname: string;
  player2Nickname: string;
  datePlayed: string;
  result: string;
};

const ModalPerfil: React.FC<ModalPerfilProps> = ({ isOpen, onClose, userId }) => {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [gameHistory, setGameHistory] = useState<GameHistory[]>([]);
  const { fetchUserProfile, fetchUserGameHistory } = useFriendship();
  const { userDetail } = useAuth();

  useEffect(() => {
    if (!isOpen || !userId) return;

    const fetchData = async () => {
      try {
        const profileData = await fetchUserProfile(userId);
        const historyData = await fetchUserGameHistory(userId);

        setProfile(profileData);
        setGameHistory(historyData);
      } catch (error) {
        console.error("Error fetching profile or game history:", error);
      }
    };

    fetchData();
  }, [isOpen, userId, fetchUserProfile, fetchUserGameHistory]);

  if (!profile) {
    return (
      <Modal title="Cargando perfil..." isOpen={isOpen} onClose={onClose}>
        <p className="text-center">Cargando...</p>
      </Modal>
    );
  }

  return (
    <Modal title={`Perfil de ${profile.nickname}`} isOpen={isOpen} onClose={onClose}>
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <img
              src={profile.avatarUrl}
              alt="Avatar"
              className="w-20 h-20 rounded-full border-2 border-gold"
            />
            <div>
              <h2 className="text-2xl font-bold text-gold">{profile.nickname}</h2>
              <p className="text-gray-400">{profile.email}</p>
            </div>
          </div>
          
          {userDetail?.id !== undefined && userId !== null && String(userDetail.id) === userId && (
            <button
              className="px-4 py-2 bg-blueLink text-white rounded opacity-1 hover:underline"
              onClick={() => alert("Abrir formulario para modificar perfil")}
            >
              Modificar perfil
            </button>
          )}
        </div>

        <div>
          <h3 className="text-lg font-semibold text-gold mb-2">Historial de Partidas</h3>
          <ul className="divide-y divide-gray-600">
            {gameHistory.length > 0 ? (
              gameHistory.map((game) => (
                <li key={game.gameId} className="py-2">
                  <p>
                    <span className="font-semibold">{game.player1Nickname}</span> vs{" "}
                    <span className="font-semibold">{game.player2Nickname}</span>
                  </p>
                  <p className="text-sm text-gray-400">
                    Fecha: {new Date(game.datePlayed).toLocaleDateString()} - Resultado:{" "}
                    {game.result}
                  </p>
                </li>
              ))
            ) : (
              <p className="text-gray-400">No hay partidas registradas.</p>
            )}
          </ul>
        </div>
      </div>
    </Modal>
  );
};

export default ModalPerfil;
