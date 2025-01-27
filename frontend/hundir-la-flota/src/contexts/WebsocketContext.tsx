"use client";

import { useRouter } from "next/navigation";
import React, { createContext, useContext, useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useAuth } from "./AuthContext";
import { useFriendship } from "./FriendshipContext";

type WebsocketContextType = {
  socket: WebSocket | null;
};

const WebsocketContext = createContext<WebsocketContextType | undefined>(
  undefined
);

export const WebsocketProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { auth } = useAuth();
  const { respondToFriendRequest, fetchFriends, setFriends } = useFriendship();
  const [socket, setSocket] = useState<WebSocket | null>(null);
  const router = useRouter();

  useEffect(() => {
    if (!auth?.token) return;

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

    newSocket.onclose = (event) => {
      console.warn(
        `Conexión cerrada: Código ${event.code}, Razón: ${event.reason}`
      );
      fetchFriends();
    };

    newSocket.onmessage = (event) => {
      try {
        const parts = event.data.split("|");
        const action = parts[0];
        switch (action) {
          case "FriendRequest":
            handleFriendRequest(parts[1]);
            break;

          case "FriendRequestResponse":
            handleFriendRequestResponse(parts[1]);
            break;

          case "FriendRemoved":
            handleFriendRemoved(parts[1]);
            break;

          case "ChatMessage":
            
            break;

          case "UserStatus":
            handleUserStatus(parts[1]);
            break;

          case "GameInvitation":
            handleGameInvitation(parts[1], parts[2]);
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

  const handleUserStatus = (payload: string) => {
    const [userId, newStatus] = payload.split(":");
    setFriends((prev) =>
      prev.map((friend) =>
        String(friend.id) === String(userId)
          ? { ...friend, status: newStatus }
          : friend
      )
    );
  };

  const handleGameInvitation = async (hostId: string, gameId: string) => {
    if (!auth?.token) return;
    try {
      const nickname = await userIdANickname(hostId, auth.token);
      toast(
        <div className="text-center">
          <p>¡Has recibido una invitación de {nickname} para jugar!</p>
          <div className="flex justify-center gap-6 mt-4">
            <button
              onClick={() => {
                acceptGameInvitation(gameId);
                router.push(`/game/${gameId}`);
                toast.dismiss();
              }}
              className="bg-green-500 text-white px-6 py-2 w-32 rounded hover:bg-green-600 transition"
            >
              Aceptar
            </button>
            <button
              onClick={() => toast.dismiss()}
              className="bg-redError text-white px-6 py-2 w-32 rounded hover:bg-red-600 transition"
            >
              Rechazar
            </button>
          </div>
        </div>,
        { autoClose: false }
      );
    } catch (error) {
      console.error("Error mostrando la invitación de juego:", error);
    }
  };

  async function acceptGameInvitation(gameId: string) {
    if (!auth?.token) return;
    try {
      const response = await fetch(
        "https://localhost:7162/api/game/accept-invitation",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${auth.token}`,
          },
          body: JSON.stringify(gameId),
        }
      );
      if (!response.ok) {
        const msg = await response.text();
        toast.error(msg);
        return;
      }
      const okMsg = await response.text();
      toast.success(`Te has unido a la partida: ${okMsg}`);
    } catch (error) {
      console.error("Error aceptando la invitación de juego:", error);
      toast.error("Error aceptando la invitación de juego.");
    }
  }

  async function userIdANickname(
    userId: string,
    token: string
  ): Promise<string> {
    try {
      const resp = await fetch(
        `https://localhost:7162/api/Friendship/get-nickname/${userId}`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (!resp.ok) throw new Error(`Error: ${resp.statusText}`);
      const data = await resp.json();
      return data.nickname || "Usuario desconocido";
    } catch (error) {
      console.error("Error obteniendo nickname:", error);
      return "Usuario desconocido";
    }
  }

  return (
    <WebsocketContext.Provider value={{ socket }}>
      {children}
    </WebsocketContext.Provider>
  );
};

export const useWebsocket = (): WebsocketContextType => {
  const context = useContext(WebsocketContext);
  if (!context) {
    throw new Error(
      "useWebsocket debe ser usado dentro de un WebsocketProvider"
    );
  }
  return context;
};
