"use client";

import React, { useEffect, useState } from "react";
import Modal from "@/components/Modal";
import ReactPaginate from "react-paginate";
import ModalEditarPerfil from "@/components/ModalEditProfile";
import { useFriendship } from "@/contexts/FriendshipContext";
import { useAuth } from "@/contexts/AuthContext";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

type ModalPerfilProps = {
  isOpen: boolean;
  onClose: () => void;
  userId: string | null;
};

type UserProfile = {
  id: string;
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
  const [pageNumber, setPageNumber] = useState(0);
  const [gamesPerPage, setGamesPerPage] = useState(5);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const { fetchUserProfile, fetchUserGameHistory, friends, removeFriend, sendFriendRequest } = useFriendship();
  const { userDetail, auth } = useAuth();
  const [isFriend, setIsFriend] = useState(false);


  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [friendToDelete, setFriendToDelete] = useState<UserProfile | null>(null);

  useEffect(() => {
    if (!isOpen || !userId) return;

    const fetchData = async () => {
      try {
        const profileData = await fetchUserProfile(userId);
        const historyData = await fetchUserGameHistory(userId);

        const formattedProfile: UserProfile = {
          id: userId,
          nickname: profileData.nickname,
          avatarUrl: profileData.avatarUrl,
          email: profileData.email,
        };
    
        setProfile(formattedProfile);
        setGameHistory(historyData);
        setIsFriend(friends.some((friend) => friend.id === userId));
      } catch (error) {
        console.error("Error fetching profile or game history:", error);
      }
    };

    fetchData();
  }, [isOpen, userId, fetchUserProfile, fetchUserGameHistory, friends]);

  const pageCount = Math.ceil(gameHistory.length / gamesPerPage);

  const changePage = ({ selected }: { selected: number }) => {
    setPageNumber(selected);
  };

  const handleGamesPerPageChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setGamesPerPage(Number(e.target.value));
    setPageNumber(0);
  };

  const handleAddFriend = async () => {
    if (!profile) return;
    try {
      await sendFriendRequest(profile.nickname);
      setIsFriend(true);
    } catch (error) {
      console.error("Error al enviar solicitud de amistad:", error);
    }
  };

   const openDeleteModal = (friend: UserProfile) => {
    setFriendToDelete(friend);
    setIsDeleteModalOpen(true);
  };

  const closeDeleteModal = () => {
    setFriendToDelete(null);
    setIsDeleteModalOpen(false);
  };

  const confirmRemoveFriend = async () => {
    if (!friendToDelete) return;
    try {
      await removeFriend(friendToDelete.id);
      setIsFriend(false);
    } catch (error) {
      console.error("Error al eliminar amigo:", error);
    }
    closeDeleteModal();
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
      const response = await fetch(`${API_URL}api/users/update`, {
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
        ...prev!,
        nickname: updatedProfile.nickname,
        email: updatedProfile.email,
        avatarUrl: updatedProfile.avatarUrl,
      }));

      setIsEditModalOpen(false);
    } catch (error: unknown) {
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

            {userDetail?.id !== undefined && userId !== null && String(userDetail.id) === userId ? (
              <button
                className="px-4 py-2 bg-blueLink text-white rounded hover:underline"
                onClick={() => setIsEditModalOpen(true)}
              >
                Modificar perfil
              </button>
            ) : isFriend ? (
              <button
                className="px-4 py-2 bg-redError text-white rounded hover:bg-red-600"
                onClick={() => openDeleteModal(profile)}
              >
                Eliminar amigo
              </button>
            ) : (
              <button
                className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
                onClick={handleAddFriend}
              >
                Agregar amigo
              </button>
            )}
          </div>

          <div>
            <h3 className="text-lg font-semibold text-gold mb-2">Historial de Partidas</h3>
            <div className="overflow-y-auto max-h-60">
              <ul className="divide-y divide-gray-600">
                {gameHistory.length > 0 ? (
                  gameHistory
                    .slice(pageNumber * gamesPerPage, (pageNumber + 1) * gamesPerPage)
                    .map((game) => (
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
                activeClassName={"text-dark font-bold"}
              />
            </div>
          </div>
        </div>
      </Modal>

      {isEditModalOpen && profile && (
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

      <Modal isOpen={isDeleteModalOpen} onClose={closeDeleteModal} title="Confirmar eliminación">
        <p>
          ¿Estás seguro de que deseas eliminar a
          <span className="font-bold"> {friendToDelete?.nickname} </span>
          de tu lista de amigos?
        </p>
        <div className="flex justify-end space-x-4 mt-4">
          <button onClick={closeDeleteModal} className="px-4 py-2 bg-gray-300 rounded hover:bg-gray-400">
            Cancelar
          </button>
          <button onClick={confirmRemoveFriend} className="px-4 py-2 bg-redError text-white rounded hover:bg-red-600">
            Confirmar
          </button>
        </div>
      </Modal>
    </>
  );
};

export default ModalPerfil;