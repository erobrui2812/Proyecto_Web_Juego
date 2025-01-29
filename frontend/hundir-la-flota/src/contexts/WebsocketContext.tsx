"use client";

import { useRouter } from "next/navigation";
import React, { createContext, useContext, useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useAuth } from "./AuthContext";
import { useFriendship } from "./FriendshipContext";

type WebsocketContextType = {
  socket: WebSocket | null;
  sendMessage: (type: string, payload: any) => void;
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
      fetchFriends();
    };

    newSocket.onerror = (error) => {
      console.error("Error en WebSocket:", error);
    };

    newSocket.onmessage = (event) => {
      try {
        const parts = event.data.split("|");
        const action = parts[0];

        switch (action) {
          case "AttackResult":
            handleAttackResult(JSON.parse(parts[1]));
            break;

          case "GameOver":
            handleGameOver(parts[1]);
            break;

          case "FriendRequest":
            handleFriendRequest(parts[1]);
            break;

          case "FriendRequestResponse":
            handleFriendRequestResponse(parts[1]);
            break;

          case "FriendRemoved":
            handleFriendRemoved(parts[1]);
            break;

          case "UserStatus":
            handleUserStatus(parts[1]);
            break;

          case "GameInvitation":
            handleGameInvitation(parts[1], parts[2]);
            break;

          case "MatchFound":
            handleMatchFound(parts[1]);
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

  const sendMessage = (type, payload) => {
    if (socket && socket.readyState === WebSocket.OPEN) {
      if (typeof payload === "object") {
        const formattedPayload = Object.values(payload).join("|");
        socket.send(`${type}|${formattedPayload}`);
      } else {
        socket.send(`${type}|${payload}`);
      }
    }
  };

  const handleAttackResult = (data: {
    x: number;
    y: number;
    result: string;
  }) => {
    toast.info(`Ataque en (${data.x}, ${data.y}): ${data.result}`);
  };

  const handleGameOver = (message: string) => {
    toast.success(`Juego terminado: ${message}`);
  };

  const handleFriendRequest = async (senderId: string) => {
    if (!auth?.token) return;
    try {
      const nickname = await userIdANickname(senderId);
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

  const handleMatchFound = (gameId: string) => {
    toast.success("¡Emparejado con un oponente!");
    router.push(`/game/${gameId}`);
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
      const nickname = await userIdANickname(friendId);
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
      const nickname = await userIdANickname(hostId);
      toast(
        <div className="text-center">
          <p>¡Has recibido una invitación de {nickname} para jugar!</p>
          <div className="flex justify-center gap-6 mt-4">
            <button
              onClick={() => {
                handleGameInvitation(hostId, gameId);
                router.push(`/game/${gameId}`);
                toast.dismiss();
              }}
              className="bg-green-500 text-white px-6 py-2 w-32 rounded hover:bg-green-600 transition"
            >
              Aceptar
            </button>
            <button
              onClick={() => toast.dismiss()}
              className="!bg-red-500 dark:bg-red-600 text-white px-6 py-2 w-32 rounded hover:bg-red-600 transition"
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

  async function userIdANickname(userId: string): Promise<string> {
    try {
      const resp = await fetch(
        `https://localhost:7162/api/Friendship/get-nickname/${userId}`,
        {
          headers: { Authorization: `Bearer ${auth?.token}` },
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
    <WebsocketContext.Provider value={{ socket, sendMessage }}>
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
