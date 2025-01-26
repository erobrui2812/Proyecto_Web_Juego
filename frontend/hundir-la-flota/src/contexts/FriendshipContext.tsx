"use client";

import { PendingRequest } from "@/types/friendship";
import React, { createContext, useContext, useEffect, useState } from "react";
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { useAuth } from "./AuthContext";

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

type Friend = {
  id: string;
  nickname: string;
  email: string;
  urlAvatar: string;
  status: string;
};

type FriendshipContextType = {
  friends: Friend[];
  setFriends: React.Dispatch<React.SetStateAction<Friend[]>>;
  searchResults: Friend[];
  sendFriendRequest: (nicknameOrEmail: string) => void;
  respondToFriendRequest: (senderId: string, accepted: boolean) => void;
  removeFriend: (friendId: string) => void;
  searchUsers: (query: string) => void;
  fetchPendingRequests: () => Promise<PendingRequest[]>;
  fetchFriends: () => void;
  fetchUserProfile: (id: string) => Promise<UserProfile>;
  fetchUserGameHistory: (id: string) => Promise<GameHistory[]>;
};

const FriendshipContext = createContext<FriendshipContextType | undefined>(
  undefined
);

export const FriendshipProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [friends, setFriends] = useState<Friend[]>([]);
  const [searchResults, setSearchResults] = useState<Friend[]>([]);
  const { auth, isAuthenticated } = useAuth();

const fetchFriends = async () => {
    if (!auth?.token) {
      console.warn("Token no disponible. No se pueden obtener amigos.");
      return;
    }
    try {
      const response = await fetch(
        "https://localhost:7162/api/Friendship/list",
        {
          headers: { Authorization: `Bearer ${auth.token}` },
        }
      );
      if (!response.ok) throw new Error(`Error: ${response.statusText}`);
      const result = await response.json();
      const mappedFriends = result.map((friend: any) => ({
        id: friend.friendId,
        nickname: friend.friendNickname,
        email: friend.friendMail,
        urlAvatar: friend.avatarUrl,
        status: friend.status || "Disconnected",
      }));
      setFriends(mappedFriends);
    } catch (error) {
      console.error("Error al obtener amigos:", error);
    }
  };

  useEffect(() => {
    if (isAuthenticated) {
      fetchFriends();
    }
  }, [isAuthenticated]);

  const sendFriendRequest = async (nicknameOrEmail: string) => {
    if (!auth?.token) return;
    try {
      const response = await fetch(
        "https://localhost:7162/api/Friendship/send",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify({
            Nickname: nicknameOrEmail,
            Email: "",
          }),
        }
      );
      if (!response.ok) {
        const text = await response.text();
        toast.error(`Error al enviar solicitud: ${text}`);
        return;
      }
      toast.success("Solicitud de amistad enviada");
    } catch (error) {
      console.error("Error al enviar solicitud:", error);
      toast.error("Error al enviar solicitud.");
    }
  };

  const respondToFriendRequest = async (friendId: string, accepted: boolean) => {
    if (!auth?.token) return;
    try {
      const response = await fetch(
        "https://localhost:7162/api/Friendship/respond",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify({
            SenderId: parseInt(friendId),
            Accept: accepted,
          }),
        }
      );

      if (!response.ok) {
        const text = await response.text();
        toast.error(`Error al responder: ${text}`);
        return;
      }
      toast.success(
        accepted
          ? "Has aceptado la solicitud de amistad."
          : "Has rechazado la solicitud."
      );
      fetchFriends();
    } catch (error) {
      console.error("Error al responder solicitud:", error);
      toast.error("Error al responder solicitud.");
    }
  };

  const removeFriend = async (friendId: string) => {
    if (!auth?.token) return;
    try {
      const response = await fetch(
        `https://localhost:7162/api/Friendship/remove/${friendId}`,
        {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        }
      );

      if (!response.ok) {
        const text = await response.text();
        toast.error(`Error al eliminar amigo: ${text}`);
        return;
      }
      toast.success("Amigo eliminado");
      fetchFriends();
    } catch (error) {
      console.error("Error al eliminar amigo:", error);
      toast.error("Error al eliminar amigo.");
    }
  };

  const searchUsers = async (query: string) => {
    if (!auth?.token) return;
    if (!query) {
      setSearchResults([]);
      return;
    }
    try {
      const response = await fetch(
        `https://localhost:7162/api/Friendship/search?nickname=${encodeURIComponent(
          query
        )}`,
        {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        }
      );
      if (response.status === 404) {
        setSearchResults([]);
        toast.info("No se encontraron usuarios con ese nickname");
        return;
      }
      if (!response.ok) throw new Error(`Error: ${response.statusText}`);
      const data = await response.json();
      const mapped = data.map((u: any) => ({
        id: u.id.toString(),
        nickname: u.nickname,
        email: "",
        urlAvatar: u.avatarUrl,
        status: "Disconnected",
      }));
      setSearchResults(mapped);
    } catch (error) {
      console.error("Error buscando usuarios:", error);
      toast.error("Error al buscar usuarios.");
    }
  };

  const fetchPendingRequests = async (): Promise<PendingRequest[]> => {
    if (!auth?.token) return [];
    try {
      const response = await fetch(
        "https://localhost:7162/api/Friendship/pending",
        {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        }
      );
      if (!response.ok) throw new Error("Error obteniendo pendientes");
      const data = await response.json();
      setPendingRequests(data);
      return data;
    } catch (error) {
      console.error("Error obteniendo solicitudes pendientes:", error);
      return [];
    }
  };

  const fetchUserProfile = async (userId: string): Promise<UserProfile> => {
    if (!auth?.token) {
      throw new Error("Token de autenticación no disponible.");
    }
    try {
      const response = await fetch(
        `https://localhost:7162/api/Users/perfil/${userId.toString()}`,
        {
          headers: { Authorization: `Bearer ${auth.token}` },
        }
      );

      if (!response.ok) {
        throw new Error(`Error al obtener el perfil: ${response.statusText}`);
      }

      const data = await response.json();
      return {
        nickname: data.data.nickname,
        avatarUrl: data.data.avatarUrl,
        email: data.data.email,
      };
    } catch (error) {
      console.error("Error en fetchUserProfile:", error);
      throw error;
    }
  };

  const fetchUserGameHistory = async (
    userId: string
  ): Promise<GameHistory[]> => {
    if (!auth?.token) {
      throw new Error("Token de autenticación no disponible.");
    }

    try {
      const response = await fetch(
        `https://localhost:7162/api/Users/historial/${userId}`,
        {
          headers: { Authorization: `Bearer ${auth.token}` },
        }
      );

      if (!response.ok) {
        throw new Error(
          `Error al obtener el historial: ${response.statusText}`
        );
      }

      const data = await response.json();
      return data.data.map((game: any) => ({
        gameId: game.gameId,
        player1Nickname: game.player1Nickname,
        player2Nickname: game.player2Nickname,
        datePlayed: game.datePlayed,
        result: game.result,
      }));
    } catch (error) {
      console.error("Error en fetchUserGameHistory:", error);
      throw error;
    }
  };

  return (
    <FriendshipContext.Provider
      value={{
        friends,
        setFriends,
        searchResults,
        sendFriendRequest,
        respondToFriendRequest,
        removeFriend,
        searchUsers,
        fetchFriends,
        fetchPendingRequests,
        fetchUserProfile,
        fetchUserGameHistory,
      }}
    >
      {children}
    </FriendshipContext.Provider>
  );
};

export const useFriendship = (): FriendshipContextType => {
  const context = useContext(FriendshipContext);
  if (!context) {
    throw new Error(
      "useFriendship debe ser usado dentro de un FriendshipProvider"
    );
  }
  return context;
};