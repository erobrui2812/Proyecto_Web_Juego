"use client";

import { PendingRequest } from "@/types/friendship";
import React, { createContext, useContext, useEffect, useState } from "react";
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { useAuth } from "./AuthContext";

type Friend = {
  id: string;
  nickname: string;
  email: string;
  urlAvatar: string;
  status: string;
};

type FriendshipContextType = {
  friends: Friend[];
  searchResults: Friend[];
  sendFriendRequest: (nicknameOrEmail: string) => void;
  respondToFriendRequest: (senderId: string, accepted: boolean) => void;
  removeFriend: (friendId: string) => void;
  searchUsers: (query: string) => void;
  fetchPendingRequests: () => Promise<PendingRequest[]>;
  fetchFriends: () => void;
};

const FriendshipContext = createContext<FriendshipContextType | undefined>(
  undefined
);

const FriendRequestNotification: React.FC = () => {
  const { auth } = useAuth();
  const { fetchFriends, respondToFriendRequest } = useFriendship();
  const [socket, setSocket] = useState<WebSocket | null>(null);

  useEffect(() => {
    if (!auth?.token) {
      return;
    }

    const newSocket = new WebSocket(
      `wss://localhost:7162/ws?token=${auth.token}`
    );

    newSocket.onopen = () => {
      console.log("Conexión establecida con WebSocket");
    };

    newSocket.onclose = (event) => {
      console.warn(
        `Conexión cerrada: Código ${event.code}, Razón: ${event.reason}`
      );
    };

    newSocket.onerror = (error) => {
      console.error("Error en WebSocket:", error);
    };

    newSocket.onmessage = (event) => {
      try {
        const [action, payload] = event.data.split("|");
        if (!action || !payload) {
          throw new Error("Formato de mensaje WebSocket inválido");
        }

        switch (action) {
          case "FriendRequest":
            handleFriendRequest(payload);
            break;
          case "FriendRequestResponse":
            handleFriendRequestResponse(payload);
            break;
          case "FriendRemoved":
            handleFriendRemoved(payload);
            break;
          case "UserStatus":
            console.log(`Estado del usuario actualizado: ${payload}`);
            fetchFriends();
            break;
          default:
            console.warn("Acción no reconocida:", action);
        }
      } catch (error) {
        console.error("Error procesando el mensaje WebSocket:", error);
      }
    };

    setSocket(newSocket);

    return () => {
      if (newSocket.readyState === WebSocket.OPEN) {
        newSocket.close();
        console.log("WebSocket cerrado correctamente.");
      }
    };
  }, [auth?.token]);

  const handleFriendRequest = async (senderId: string) => {
    if (!auth?.token) return;

    try {
      const nickname = await userIdANickname(senderId, auth.token);
      toast(
        <div className="text-center">
          <p>Nueva solicitud de amistad de: {nickname}</p>
          <div className="flex justify-center gap-6 mt-4">
            <button
              onClick={() => {
                respondToFriendRequest(senderId, true);
                toast.dismiss();
              }}
              className="bg-green-500 text-white px-6 py-2 w-32 rounded hover:bg-green-600 transition"
            >
              Aceptar
            </button>
            <button
              onClick={() => {
                respondToFriendRequest(senderId, false);
                toast.dismiss();
              }}
              className="bg-red-500 text-white px-6 py-2 w-32 rounded hover:bg-red-600 transition"
            >
              Rechazar
            </button>
          </div>
        </div>,
        { autoClose: false }
      );
    } catch (error) {
      console.error("Error manejando solicitud de amistad:", error);
    }
  };

  const handleFriendRequestResponse = (response: string) => {
    const accepted = response === "Accepted";
    toast.info(
      accepted
        ? "Solicitud de amistad aceptada"
        : "Solicitud de amistad rechazada"
    );
    fetchFriends();
  };

  const handleFriendRemoved = async (friendId: string) => {
    if (!auth?.token) return;

    try {
      const nickname = await userIdANickname(friendId, auth.token);
      toast.info(`Amigo eliminado: ${nickname}`);
      fetchFriends();
    } catch (error) {
      console.error("Error manejando la eliminación de amigo:", error);
    }
  };

  return null;
};

async function userIdANickname(userId: string, token: string): Promise<string> {
  try {
    const response = await fetch(
      `https://localhost:7162/api/Friendship/get-nickname/${userId}`,
      {
        headers: { Authorization: `Bearer ${token}` },
      }
    );

    if (!response.ok) throw new Error(`Error: ${response.statusText}`);
    const data = await response.json();
    return data.nickname || "Usuario desconocido";
  } catch (error) {
    console.error("Error obteniendo nickname:", error);
    return "Usuario desconocido";
  }
}

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
        urlAvatar: friend.avatarUrl || "https://via.placeholder.com/150",
        status: friend.status,
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

  const respondToFriendRequest = async (
    friendId: string,
    accepted: boolean
  ) => {
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
        urlAvatar: u.avatarUrl || "https://via.placeholder.com/150",
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

  return (
    <FriendshipContext.Provider
      value={{
        friends,
        searchResults,
        sendFriendRequest,
        respondToFriendRequest,
        removeFriend,
        searchUsers,
        fetchFriends,
        fetchPendingRequests,
      }}
    >
      {auth.token && <FriendRequestNotification />}
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
