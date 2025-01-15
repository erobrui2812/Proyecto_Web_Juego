"use client";

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
  sendFriendRequest: (friendId: string) => void;
  respondToFriendRequest: (friendId: string, accepted: boolean) => void;
  removeFriend: (friendId: string) => void;
  searchUsers: (query: string) => void;
};

const FriendshipContext = createContext<FriendshipContextType | undefined>(
  undefined
);

const FriendRequestNotification: React.FC = () => {
  const { auth } = useAuth();
  const [socket, setSocket] = useState<WebSocket | null>(null);

  useEffect(() => {
    if (!auth?.token) return;

    const newSocket = new WebSocket(
      `wss://localhost:7162/ws/connect?token=${auth.token}`
    );

    newSocket.onopen = () => {
      console.log("Conexión establecida con WebSocket");
    };

    newSocket.onmessage = (event) => {
      const [action, payload] = event.data.split("|");

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
          break;
        default:
          console.error("Acción no reconocida:", action);
      }
    };

    newSocket.onclose = () => {
      console.log("Conexión cerrada con WebSocket");
    };

    newSocket.onerror = (error) => {
      console.error("Error en WebSocket:", error);
    };

    setSocket(newSocket);

    return () => {
      newSocket.close();
    };
  }, [auth?.token]);

  const handleFriendRequest = async (senderId: string) => {
    const nickname = await userIdANickname(senderId);
    toast(
      <div>
        <p>Nueva solicitud de amistad de: {nickname}</p>
        <div
          style={{
            display: "flex",
            justifyContent: "space-around",
            marginTop: "10px",
          }}
        >
          <button
            onClick={() => {
              respondToFriendRequest(senderId, true);
              toast.dismiss();
            }}
            style={{
              backgroundColor: "green",
              color: "white",
              border: "none",
              padding: "5px 10px",
              cursor: "pointer",
            }}
          >
            Aceptar
          </button>
          <button
            onClick={() => {
              respondToFriendRequest(senderId, false);
              toast.dismiss();
            }}
            style={{
              backgroundColor: "red",
              color: "white",
              border: "none",
              padding: "5px 10px",
              cursor: "pointer",
            }}
          >
            Rechazar
          </button>
        </div>
      </div>,
      { position: "top-right", autoClose: false, closeOnClick: false }
    );
  };

  const respondToFriendRequest = async (friendId: string, accepted: boolean) => {
    if (!auth?.token) return;

    try {
      const response = await fetch(
        "http://localhost:7162/api/Friendship/respond",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify({
            senderId: friendId,
            accept: accepted,
          }),
        }
      );

      if (!response.ok) throw new Error(`Error: ${response.statusText}`);
      toast.success(
        accepted
          ? "Solicitud de amistad aceptada."
          : "Solicitud de amistad rechazada."
      );
    } catch (error) {
      console.error("Error al responder a la solicitud de amistad:", error);
    }
  };


  const handleFriendRequestResponse = (response: string) => {
    const accepted = response === "Accepted";
    toast.info(
      accepted
        ? "Solicitud de amistad aceptada"
        : "Solicitud de amistad rechazada"
    );
  };

  const handleFriendRemoved = async (friendId: string) => {
    const nickname = await userIdANickname(friendId);
    toast.info(`Amigo eliminado: ${nickname}`);
  };

  return null;
};

const userIdANickname = async (userId: string): Promise<string> => {
  const { auth } = useAuth();
  if (!auth?.token) return "Usuario desconocido";

  try {
    const response = await fetch(
      `http://localhost:7162/api/Friendship/get-nickname/${userId}`,
      {
        headers: { Authorization: `Bearer ${auth.token}` },
      }
    );
    if (!response.ok) throw new Error(`Error: ${response.statusText}`);

    const data = await response.json();
    return data.nickname || "Usuario desconocido";
  } catch (error) {
    console.error("Error al obtener el nickname:", error);
    return "Usuario desconocido";
  }
};

export const FriendshipProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [friends, setFriends] = useState<Friend[]>([]);
  const [searchResults, setSearchResults] = useState<Friend[]>([]);
  const { auth, isAuthenticated } = useAuth();

  const fetchFriends = async () => {
    if (!auth?.token) return;

    try {
      const response = await fetch(
        "http://localhost:7162/api/Friendship/list",
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
        status: friend.status || "Desconocido",
      }));

      setFriends(mappedFriends);
    } catch (error) {
      console.error("Error al obtener amigos:", error);
    }
  };

  const searchUsers = async (query: string) => {
    if (!auth?.token || !query) return;

    try {
      const response = await fetch(
        `http://localhost:7162/api/Friendship/search?nickname=${query}`,
        {
          headers: { Authorization: `Bearer ${auth.token}` },
        }
      );

      if (!response.ok) throw new Error(`Error: ${response.statusText}`);

      const result = await response.json();
      const users = result.map((user: any) => ({
        id: user.id,
        nickname: user.nickname,
        email: "",
        urlAvatar: user.avatarUrl || "https://via.placeholder.com/150",
        status: "Desconocido",
      }));

      setSearchResults(users);
    } catch (error) {
      console.error("Error al buscar usuarios:", error);
    }
  };

  const sendFriendRequest = async (friendId: string) => {
    if (!auth?.token) return;

    try {
      const response = await fetch(
        `http://localhost:7162/api/Friendship/add-${friendId}`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${auth.token}` },
        }
      );

      if (!response.ok) throw new Error(`Error: ${response.statusText}`);

      toast.success("Solicitud de amistad enviada.");
    } catch (error) {
      console.error("Error al enviar solicitud de amistad:", error);
      toast.error("No se pudo enviar la solicitud de amistad.");
    }
  };

  const respondToFriendRequest = async (friendId: string, accepted: boolean) => {
    if (!auth?.token) return;
  
    try {
      const response = await fetch(
        "http://localhost:7162/api/Friendship/respond",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify({
            senderId: friendId,
            accept: accepted,
          }),
        }
      );
  
      if (!response.ok) throw new Error(`Error: ${response.statusText}`);
      toast.success(
        accepted
          ? "Solicitud de amistad aceptada."
          : "Solicitud de amistad rechazada."
      );
    } catch (error) {
      console.error("Error al responder a la solicitud de amistad:", error);
    }
  };
  

  const removeFriend = async (friendId: string) => {
    if (!auth?.token) return;

    try {
      const response = await fetch(
        "http://localhost:7162/api/Friendship/remove",
        {
          method: "DELETE",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify(friendId),
        }
      );

      if (!response.ok) throw new Error(`Error: ${response.statusText}`);

      toast.success("Amigo eliminado.");
      setFriends((prev) => prev.filter((f) => f.id !== friendId));
    } catch (error) {
      console.error("Error al eliminar amigo:", error);
      toast.error("No se pudo eliminar al amigo.");
    }
  };

  useEffect(() => {
    if (isAuthenticated) fetchFriends();
  }, [isAuthenticated]);

  return (
    <FriendshipContext.Provider
      value={{
        friends,
        searchResults,
        sendFriendRequest,
        respondToFriendRequest,
        removeFriend,
        searchUsers,
      }}
    >
      <FriendRequestNotification />
      {children}
    </FriendshipContext.Provider>
  );
}

export const useFriendship = (): FriendshipContextType => {
  const context = useContext(FriendshipContext);
  if (!context)
    throw new Error(
      "useFriendship debe ser usado dentro de un FriendshipProvider"
    );
  return context;
};
