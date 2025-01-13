import React, { createContext, useContext, useEffect } from "react";
import { HttpTransportType, HubConnectionBuilder, LogLevel } from "@microsoft/signalr"; // Nueva biblioteca
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

//implementar metodos faltantes
//crear array de amigos como objeto(id, nombre,mail,urlAvatar)
//websocket pausado por ahora

// Tipos
type FriendshipContextType = {
  sendFriendRequest: (friendId: string) => void;
  acceptFriendRequest: (friendId: string) => void;
  declineFriendRequest: (friendId: string) => void;
};

// Contexto
const FriendshipContext = createContext<FriendshipContextType | undefined>(undefined);

// Mostrar toast con botones
const showFriendRequestToast = (senderId: string, onAccept: () => void, onDecline: () => void) => {
  toast(
    <div>
      <p>Nueva solicitud de amistad de: {senderId}</p>
      <div style={{ display: "flex", justifyContent: "space-around", marginTop: "10px" }}>
        <button
          onClick={() => {
            onAccept();
            toast.dismiss(); // Cierra el toast
          }}
          style={{ backgroundColor: "green", color: "white", border: "none", padding: "5px 10px", cursor: "pointer" }}
        >
          Aceptar
        </button>
        <button
          onClick={() => {
            onDecline();
            toast.dismiss(); // Cierra el toast
          }}
          style={{ backgroundColor: "red", color: "white", border: "none", padding: "5px 10px", cursor: "pointer" }}
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
const FriendRequestNotification = () => {
  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl("https://localhost:7162/notificationHub", {
        skipNegotiation: true, // Desactiva la negociación para WebSockets si SignalR lo permite
        transport: HttpTransportType.WebSockets, // Fuerza el uso de WebSocket
      })
      .configureLogging(LogLevel.Information)
      .build();


    // Métodos del servidor
    connection.on("ReceiveFriendRequest", (senderId: string) => {
      showFriendRequestToast(
        senderId,
        () => {
          console.log(`Solicitud aceptada de: ${senderId}`);
          connection.invoke("AcceptFriendRequest", senderId).catch((err) => console.error(err));
        },
        () => {
          console.log(`Solicitud rechazada de: ${senderId}`);
          connection.invoke("DeclineFriendRequest", senderId).catch((err) => console.error(err));
        }
      );
    });

    connection.on("FriendRequestResponse", (accepted: boolean) => {
      toast.info(accepted ? "Solicitud de amistad aceptada" : "Solicitud de amistad rechazada");
    });

    connection.on("FriendRemoved", (friendId: string) => {
      toast.info(`Amigo eliminado: ${friendId}`);
    });

    // Iniciar conexión
    connection
      .start()
      .then(() => console.log("Conexión establecida con SignalR"))
      .catch((err) => console.error("Error de conexión: ", err));

    return () => {
      connection.stop(); // Detener conexión al desmontar el componente
    };
  }, []);

  return null; // Este componente no renderiza nada
};

// Proveedor de contexto
export const FriendshipProvider = ({ children }: { children: React.ReactNode }) => {
  const sendFriendRequest = (friendId: string) => {
    console.log(`Enviando solicitud de amistad a: ${friendId}`);
    // Llama a tu endpoint o lógica para enviar solicitudes
  };

  const acceptFriendRequest = (friendId: string) => {
    console.log(`Aceptando solicitud de amistad de: ${friendId}`);
    // Llama a tu endpoint o lógica para aceptar solicitudes
  };

  const declineFriendRequest = (friendId: string) => {
    console.log(`Rechazando solicitud de amistad de: ${friendId}`);
    // Llama a tu endpoint o lógica para rechazar solicitudes
  };

  return (
    <FriendshipContext.Provider value={{ sendFriendRequest, acceptFriendRequest, declineFriendRequest }}>
      <FriendRequestNotification />
      {children}
    </FriendshipContext.Provider>
  );
};

// Hook para usar el contexto
export const useFriendship = (): FriendshipContextType => {
  const context = useContext(FriendshipContext);
  if (!context) {
    throw new Error("useFriendship debe ser usado dentro de un FriendshipProvider");
  }
  return context;
};
