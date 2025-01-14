"use client";
// Importaciones
import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import React, { createContext, useContext, useEffect, useState } from "react";
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { useAuth } from "./AuthContext";

// Tipos
type Friend = {
  id: string;
  nickname: string;
  email: string;
  urlAvatar: string;
};

type FriendshipContextType = {
  friends: Friend[];
  sendFriendRequest: (friendId: string) => void;
  respondToFriendRequest: (friendId: string, accepted: boolean, token: any) => void;
  removeFriend: (friendId: string) => void;
};

// Contexto
const FriendshipContext = createContext<FriendshipContextType | undefined>(
  undefined
);

// Mostrar toast con botones
const showFriendRequestToast = (
  senderId: string,
  onAccept: () => void,
  onDecline: () => void
) => {
  toast(
    <div>
      <p>Nueva solicitud de amistad de: {senderId}</p>
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

// Componente de notificaciones
const FriendRequestNotification: React.FC = () => {
  const { isAuthenticated, auth } = useAuth();
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      console.log("El usuario no está autenticado. No se iniciará SignalR.");
      return;
    }

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
          //console.log(`Solicitud aceptada de: ${senderId}`);
          respondToFriendRequest(senderId, true, auth?.token); // Pasar el token
        },
        () => {
          //console.log(`Solicitud rechazada de: ${senderId}`);
          respondToFriendRequest(senderId, false, auth?.token); // Pasar el token
        }
      );
    });

    newConnection.on("FriendRequestResponse", (accepted: boolean) => {
      toast.info(
        accepted
          ? "Solicitud de amistad aceptada"
          : "Solicitud de amistad rechazada"
      );
    });

    newConnection.on("FriendRemoved", (friendId: string) => {
      toast.info(`Amigo eliminado: ${friendId}`);
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

const respondToFriendRequest = async (
  friendId: string,
  accepted: boolean,
  token: string | null
) => {
  if (!token) {
    console.error("El token de autenticación no está definido.");
    return;
  }

  // console.log(
  //   `${accepted ? "Aceptando" : "Rechazando"} solicitud de amistad de: ${friendId}`
  // );

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

    if (!response.ok) {
      const errorDetails = await response.text();
      console.error(`Error en la respuesta: ${response.statusText}, Detalles: ${errorDetails}`);
      throw new Error(`Error en la respuesta: ${response.statusText}`);
    }

    const result = await response.json();
    //console.log("Respuesta del servidor:", result);

    if (accepted) {
      //console.log(`Amistad aceptada con: ${friendId}`);
    } else {
      //console.log(`Solicitud de amistad rechazada para: ${friendId}`);
    }
  } catch (error) {
    console.error("Error al responder a la solicitud de amistad:", error);
  }
};

// Proveedor de contexto
export const FriendshipProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [friends, setFriends] = useState<Friend[]>([]);

  const sendFriendRequest = (friendId: string) => {
    //console.log(`Enviando solicitud de amistad a: ${friendId}`);
    // Lógica para enviar solicitud
  };

  const removeFriend = (friendId: string) => {
    //console.log(`Eliminando amigo con id: ${friendId}`);
    setFriends((prevFriends) =>
      prevFriends.filter((friend) => friend.id !== friendId)
    );
  };

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

// Hook para usar el contexto
export const useFriendship = (): FriendshipContextType => {
  const context = useContext(FriendshipContext);
  if (!context) {
    throw new Error(
      "useFriendship debe ser usado dentro de un FriendshipProvider"
    );
  }
  return context;
};