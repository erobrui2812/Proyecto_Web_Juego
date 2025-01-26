"use client";

import React, { createContext, useContext, useEffect, useState } from "react";
import "react-toastify/dist/ReactToastify.css";
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
          throw new Error("Formato de mensaje WebSocket erroneo");
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

          case "ChatMessage":
           
            break;

          case "UserStatus":
            handleUserStatus(payload);
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
  }, [auth.token]);

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
              className="bg-redError text-white px-6 py-2 w-32 rounded hover:bg-red-600 transition"
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
    console.log("Evento UserStatus recibido:", { userId, newStatus });

    setFriends((prevFriends) =>
      prevFriends.map((friend) =>
        String(friend.id) === String(userId)
          ? { ...friend, status: newStatus }
          : friend
      )
    );
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

  return (
    <WebsocketContext.Provider value={{ socket }}>
      {children}
    </WebsocketContext.Provider>
  );
};

export const useWebsocket = (): WebsocketContextType => {
  const context = useContext(WebsocketContext);
  if (!context) {
    throw new Error("useWebsocket debe ser usado dentro de un WebsocketProvider");
  }
  return context;
};