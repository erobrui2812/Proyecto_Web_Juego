"use client";

import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
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
  sendFriendRequest: (friendId: string) => void;
  respondToFriendRequest: (friendId: string, accepted: boolean, token: any) => void;
  removeFriend: (friendId: string) => void;
};

const FriendshipContext = createContext<FriendshipContextType | undefined>(undefined);

const showFriendRequestToast = (
  senderId: string,
  onAccept: () => void,
  onDecline: () => void
) => {
  toast(
    <div>
      <p>Nueva solicitud de amistad de: {userIdANickname(senderId)}</p>
      <div
        style={{
          display: "flex",
          justifyContent: "space-around",
          marginTop: "10px",
        }}
      >
        <button
          onClick={() => {
            onAccept();
            toast.dismiss();
          }}
          style={{
            backgroundColor: "green",
            color: "white",
            border: "none",
            padding: "5px 10px",
            cursor: "pointer",
          }}
          className="border p-2 rounded-md w-1/3"
        >
          Aceptar
        </button>
        <button
          onClick={() => {
            onDecline();
            toast.dismiss();
          }}
          style={{
            backgroundColor: "red",
            color: "white",
            border: "none",
            padding: "5px 10px",
            cursor: "pointer",
          }}
          className="border p-2 rounded-md w-1/3"
        >
          Rechazar
        </button>
      </div>
    </div>,
    {
      position: "top-right",
      autoClose: false,
      closeOnClick: false,
    }
  );
};

const FriendRequestNotification: React.FC = () => {
  const { isAuthenticated, auth } = useAuth();
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    const newConnection = new HubConnectionBuilder()
      .withUrl("https://localhost:7162/notificationHub", {
        accessTokenFactory: () => auth?.token || "",
      })
      .configureLogging(LogLevel.Information)
      .build();

    setConnection(newConnection);

    newConnection.on("ReceiveFriendRequest", (senderId: string) => {
      showFriendRequestToast(
        senderId,
        () => {
          respondToFriendRequest(senderId, true, auth?.token);
        },
        () => {
          respondToFriendRequest(senderId, false, auth?.token);
        }
      );
    });

    newConnection.on("FriendRequestResponse", (accepted: boolean) => {
      toast.info(
        accepted ? "Solicitud de amistad aceptada" : "Solicitud de amistad rechazada"
      );
    });

    newConnection.on("FriendRemoved", (friendId: string) => {
      
      toast.info(`Amigo eliminado: ${userIdANickname(friendId)}`);
    });

    newConnection
      .start()
      .then(() => console.log("Conexión establecida con SignalR"))
      .catch((err) => console.error("Error al iniciar la conexión SignalR:", err));

    return () => {
      newConnection.stop().then(() => console.log("Conexión cerrada."));
    };
  }, [isAuthenticated, auth?.token]);

  return null;
};

const userIdANickname = async (userId: string): Promise<string> => {
  const parsedUserId = parseInt(userId);

  if (isNaN(parsedUserId)) {
    console.error("El userId proporcionado no es un número válido.");
    return "Usuario desconocido";
  }

  try {
    const response = await fetch(`https://localhost:7162/api/Friendship/get-nickname/${parsedUserId}`, {
      method: "GET",
    });

    if (!response.ok) {
      console.error(`Error en la respuesta: ${response.statusText}`);
      return "Usuario desconocido";
    }

    const data = await response.json();
    return data.nickname || "Usuario desconocido";
  } catch (error) {
    console.error("Error al obtener el nickname:", error);
    return "Usuario desconocido";
  }
};


const respondToFriendRequest = async (
  friendId: string,
  accepted: boolean,
  token: string | null
) => {
  if (!token) return;

  try {
    const response = await fetch("https://localhost:7162/api/Friendship/respond", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        senderId: friendId,
        accept: accepted,
      }),
    });

    if (!response.ok) throw new Error(`Error en la respuesta: ${response.statusText}`);
  } catch (error) {
    console.error("Error al responder a la solicitud de amistad:", error);
  }
};

export const FriendshipProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [friends, setFriends] = useState<Friend[]>([]);
  const { isAuthenticated, auth } = useAuth();

  const fetchFriends = async () => {
    if (!auth?.token) return;
  
    try {
      const response = await fetch("https://localhost:7162/api/Friendship/list", {
        method: "GET",
        headers: {
          Authorization: `Bearer ${auth.token}`,
        },
      });
  
      if (!response.ok) throw new Error(`Error en la respuesta: ${response.statusText}`);
  
      const result = await response.json();
      console.log("Respuesta del backend (sin mapear):", result);
  
      const mappedFriends = result.map((friend: any) => ({
        id: friend.friendId,
        nickname: friend.friendNickname,
        email: friend.friendMail,
        urlAvatar: friend.avatarUrl,
      }));
  
      console.log("Amigos mapeados:", mappedFriends);
      setFriends(mappedFriends);
    } catch (error) {
      console.error("Error al obtener la lista de amigos:", error);
    }
  };  

  const sendFriendRequest = async (friendId: string) => {
    if (!auth?.token) return;

    try {
      const response = await fetch("https://localhost:7162/api/Friendship/send", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${auth.token}`,
        },
        body: JSON.stringify({ nickname: friendId }),
      });

      if (!response.ok) throw new Error(`Error en la respuesta: ${response.statusText}`);

      const result = await response.json();
      toast.success(result.message || "Solicitud de amistad enviada");
    } catch (error) {
      console.error("Error al enviar solicitud de amistad:", error);
    }
  };

  const removeFriend = async (friendId: string) => {
    if (!auth?.token) return;

    try {
      const response = await fetch("https://localhost:7162/api/Friendship/remove", {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${auth.token}`,
        },
        body: JSON.stringify(friendId),
      });

      if (!response.ok) throw new Error(`Error en la respuesta: ${response.statusText}`);

      toast.success("Amigo eliminado");
      setFriends((prevFriends) =>
        prevFriends.filter((friend) => friend.id !== friendId)
      );
    } catch (error) {
      console.error("Error al eliminar amigo:", error);
    }
  };

  useEffect(() => {
    if (isAuthenticated) fetchFriends();
  }, [isAuthenticated]);

  return (
    <FriendshipContext.Provider
      value={{
        friends,
        sendFriendRequest,
        respondToFriendRequest,
        removeFriend,
      }}
    >
      <FriendRequestNotification />
      {children}
    </FriendshipContext.Provider>
  );
};

export const useFriendship = (): FriendshipContextType => {
  const context = useContext(FriendshipContext);
  if (!context) {
    throw new Error("useFriendship debe ser usado dentro de un FriendshipProvider");
  }
  return context;
};