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

  // useEffect(() => {
  //   console.log(messages);
  // }, [messages]);

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

    setMessages((prev) => [...prev, { userId: idUsuario, message: newMessage }]);
    socket.send(`ChatMessage|${payload}`);
    setNewMessage("");
  };

  return (
    <div className="p-4 border rounded-lg">
      <h2 className="text-xl font-bold mb-2">Chat de la partida</h2>
      <div className="h-64 overflow-y-auto border mb-2 p-2">
        {messages.map((msg, index) => (
          <div key={index} className="mb-1">
            <strong>Usuario {msg.userId}:</strong> {msg.message}
          </div>
        ))}
      </div>
      <div className="flex items-center space-x-2">
        <input
          type="text"
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Escribe un mensaje"
          className="flex-grow border p-2 rounded"
        />
        <button
          onClick={sendMessage}
          className="bg-blue-500 text-white p-2 rounded hover:bg-blue-600"
        >
          Enviar
        </button>
      </div>
    </div>
  );
}