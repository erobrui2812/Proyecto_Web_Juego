"use client";

import { useWebsocket } from "@/contexts/WebsocketContext";
import { useEffect, useState } from "react";

export default function Chat({ gameId }) {
  const { socket } = useWebsocket(); // Obtenemos el WebSocket del contexto
  const [messages, setMessages] = useState([]); // Mensajes del chat
  const [newMessage, setNewMessage] = useState(""); // Mensaje que se está escribiendo

  useEffect(() => {
    if (!socket) {
      console.warn("WebSocket no está inicializado.");
      return;
    }

    // Escuchar mensajes del WebSocket
    const handleMessage = (event) => {
      const data = event.data.split("|");
      const action = data[0];
      const payload = data[1];

      if (action === "ChatMessage") {
        const [userId, message] = payload.split(":");
        setMessages((prev) => [...prev, { userId, message }]);
      }
    };

    socket.addEventListener("message", handleMessage);

    // Cleanup al desmontar
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
    socket.send(`ChatMessage|${payload}`); // Enviar mensaje al servidor
    setNewMessage(""); // Limpiar el campo de entrada
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
