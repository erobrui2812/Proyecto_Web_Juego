"use client";

import { useAuth } from "@/contexts/AuthContext";
import { useWebsocket } from "@/contexts/WebsocketContext";
import { useEffect, useState } from "react";

interface Message {
  userId: string | number;
  message: string;
}

export default function Chat({ gameId }: { gameId: string }) {
  const { socket } = useWebsocket();
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState<string>("");
  const { userDetail } = useAuth();

  useEffect(() => {
    if (!socket) {
      console.warn("WebSocket no está inicializado.");
      return;
    }

    const handleMessage = (event: MessageEvent) => {
      const data = event.data.split("|");
      const action = data[0];
      const payload = data[1];

      if (action === "ChatMessage") {
        const [userId, message] = payload.split(":");
        setMessages((prev) => [...prev, { userId, message }]);
      }
    };

    socket.addEventListener("message", handleMessage);

    return () => {
      socket.removeEventListener("message", handleMessage);
    };
  }, [socket]);

  const sendMessage = () => {
    if (!newMessage.trim()) {
      console.warn("El mensaje está vacío.");
      return;
    }

    if (!socket || socket.readyState !== WebSocket.OPEN) {
      console.error("El WebSocket no está conectado.");
      return;
    }

    const payload = `${gameId}:${newMessage}`;
    const idUsuario = userDetail?.id || "1";

    setMessages((prev) => [
      ...prev,
      { userId: idUsuario, message: newMessage },
    ]);
    socket.send(`ChatMessage|${payload}`);
    setNewMessage("");
  };

  return (
    <div className="chat-container">
      <h2 className="chat-header">Chat de la partida</h2>
      <div className="chat-messages">
        {messages.map((msg, index) => (
          <div key={index} className="mb-1">
            <strong>Usuario {msg.userId}:</strong> {msg.message}
          </div>
        ))}
      </div>
      <div className="flex flex-col sm:flex-row items-stretch gap-2">
        <input
          type="text"
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Escribe un mensaje"
          className="chat-input flex-grow"
        />
        <button onClick={sendMessage} className="chat-send">
          Enviar
        </button>
      </div>
    </div>
  );
}
