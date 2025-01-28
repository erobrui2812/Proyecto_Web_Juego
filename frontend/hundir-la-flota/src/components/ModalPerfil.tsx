"use client";

import React, { useEffect, useState } from "react";
import Modal from "@/components/Modal";
import ReactPaginate from "react-paginate";
import ModalEditarPerfil from "@/components/ModalEditProfile";
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
  const [filteredHistory, setFilteredHistory] = useState<GameHistory[]>([]);
  const [pageNumber, setPageNumber] = useState(0);
  const [gamesPerPage, setGamesPerPage] = useState(5);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const { fetchUserProfile, fetchUserGameHistory } = useFriendship();
  const { userDetail,auth } = useAuth();

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

  useEffect(() => {
    const startIndex = pageNumber * gamesPerPage;
    const endIndex = startIndex + gamesPerPage;
    setFilteredHistory(gameHistory.slice(startIndex, endIndex));
  }, [pageNumber, gamesPerPage, gameHistory]);

  const pageCount = Math.ceil(gameHistory.length / gamesPerPage);

  const changePage = ({ selected }: { selected: number }) => {
    setPageNumber(selected);
  };

  const handleGamesPerPageChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setGamesPerPage(Number(e.target.value));
    setPageNumber(0);
  };

  const handleEditSubmit = async (data: {
    nickname: string;
    email: string;
    avatar: File | null;
    currentPassword?: string;
    newPassword?: string;
  }) => {
    try {
      const formData = new FormData();
      formData.append("nickname", data.nickname);
      formData.append("email", data.email);

      if (data.avatar) {
        formData.append("avatar", data.avatar);
      }

      if (data.currentPassword) {
        formData.append("currentPassword", data.currentPassword);
        formData.append("newPassword", data.newPassword || "");
      }

      const token = auth.token;
      const response = await fetch("https://localhost:7162/api/users/update", {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || "Error al actualizar el perfil.");
      }

      const updatedProfile = await response.json();
      setProfile((prev) => ({
        ...prev,
        nickname: updatedProfile.nickname,
        email: updatedProfile.email,
        avatarUrl: updatedProfile.avatarUrl,
      }));

      setIsEditModalOpen(false);
    } catch (error: any) {
      console.error(error);
      alert(error.message || "Error al actualizar el perfil.");
    }
  };

  if (!profile) {
    return (
      <Modal title="Cargando perfil..." isOpen={isOpen} onClose={onClose}>
        <p className="text-center">Cargando...</p>
      </Modal>
    );
  }

  return (
    <>
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
                onClick={() => setIsEditModalOpen(true)}
              >
                Modificar perfil
              </button>
            )}
          </div>

          <div>
            <h3 className="text-lg font-semibold text-gold mb-2">Historial de Partidas</h3>
            <div className="overflow-y-auto max-h-60">
              <ul className="divide-y divide-gray-600">
                {filteredHistory.length > 0 ? (
                  filteredHistory.map((game) => (
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

            <div className="mt-4 flex justify-between items-center">
              <select
                value={gamesPerPage}
                onChange={handleGamesPerPageChange}
                className="px-3 py-2 border rounded"
              >
                <option value={5}>5</option>
                <option value={10}>10</option>
                <option value={15}>15</option>
              </select>

              <ReactPaginate
                previousLabel={"<"}
                nextLabel={">"}
                pageCount={pageCount}
                onPageChange={changePage}
                forcePage={pageNumber}
                containerClassName={"flex gap-2"}
                pageLinkClassName={"px-2 py-1 border rounded hover:bg-gray-300 transition"}
                previousLinkClassName={"px-2 py-1 border rounded hover:bg-gray-300 transition"}
                nextLinkClassName={"px-2 py-1 border rounded hover:bg-gray-300 transition"}
                disabledClassName={"opacity-50 cursor-not-allowed"}
                activeClassName={"text-dark font-bold"}
              />
            </div>
          </div>
        </div>
      </Modal>

      {isEditModalOpen && (
        <ModalEditarPerfil
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          initialData={{
            nickname: profile.nickname,
            email: profile.email,
            avatarUrl: profile.avatarUrl,
          }}
          onSubmit={handleEditSubmit}
        />
      )}
    </>
  );
};

export default ModalPerfil;
