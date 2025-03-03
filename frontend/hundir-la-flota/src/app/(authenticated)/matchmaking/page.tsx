"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

import Button from "@/components/Button";
import ListaAmigosConectados from "@/components/ListaAmigosConectados";
import Modal from "@/components/Modal";

import { useAuth } from "@/contexts/AuthContext";
import { useWebsocket } from "@/contexts/WebsocketContext";

import { Friend } from "@/types/friendship";

const API_URL = apiUrl;

const MatchmakingPage = () => {
  const { auth } = useAuth();
  const token = auth?.token || null;
  const router = useRouter();
  const { socket } = useWebsocket();

  const [amigosConectados, setAmigosConectados] = useState<Friend[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedFriend, setSelectedFriend] = useState<Friend | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    const fetchAmigos = async () => {
      if (!token) return;

      try {
        const response = await fetch(
          `${API_URL}api/Friendship/connected`,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );
        if (!response.ok) throw new Error("Error al obtener amigos conectados");

        const data = await response.json();

        const friendsMapped: Friend[] = data.map((friend: unknown) => ({
          id: friend.friendId,
          nickname: friend.friendNickname,
          email: friend.friendMail,
          urlAvatar: friend.avatarUrl,
          status: friend.status,
        }));

        setAmigosConectados(friendsMapped);
      } catch (error) {
        toast.error("Error al obtener amigos conectados.");
        console.error("Error fetching connected friends:", error);
      }
    };

    fetchAmigos();
  }, [token]);


  const jugarContraBot = async () => {
    if (!token) {
      toast.error("Debes iniciar sesión para jugar.");
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(
        `${API_URL}api/game/play-with-bot`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (!response.ok) throw new Error("Error al jugar contra un bot");

      const data = await response.json();
      toast.success("¡Partida contra el bot creada!");
      router.push(`/game/${data.gameId}`);
    } catch (error) {
      toast.error("Error al crear partida contra el bot.");
      console.error("Error playing with bot:", error);
    } finally {
      setLoading(false);
    }
  };

  const unirsePartidaAleatoriaWS = () => {
    if (!token) {
      toast.error("Debes iniciar sesión para unirte a una partida.");
      return;
    }
    if (!socket || socket.readyState !== WebSocket.OPEN) {
      toast.error("No hay WebSocket activo o está cerrado.");
      return;
    }

    setLoading(true);
    socket.send("Matchmaking|random");
    toast.info("Buscando oponente...");
  };
  const cancelarMatchmaking = () => {
    if (!token) {
      toast.error("Debes iniciar sesión para unirte a una partida.");
      return;
    }
    if (!socket || socket.readyState !== WebSocket.OPEN) {
      toast.error("No hay WebSocket activo o está cerrado.");
      return;
    }

    setLoading(false);
    socket.send("Matchmaking|cancel");
    toast.info("Cancelando el emparejamiento...");
  };

  const invitarAmigo = async (amigoId: string) => {
    if (!token) {
      toast.error("Debes iniciar sesión para invitar a un amigo.");
      return;
    }

    setLoading(true);
    try {
      const friendIdNumber = parseInt(amigoId, 10);

      const response = await fetch(`${API_URL}api/game/invite`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(friendIdNumber),
      });

      if (!response.ok) throw new Error("Error al invitar a un amigo");

      toast.success("Invitación enviada con éxito.");
    } catch (error) {
      toast.error("Error al invitar a un amigo.");
      console.error("Error inviting friend:", error);
    } finally {
      setLoading(false);
      setModalOpen(false);
    }
  };

  const handleSelectFriend = (friend: Friend) => {
    setSelectedFriend(friend);
    setModalOpen(true);
  };

  return (
    <div className="flex flex-col items-center justify-center py-10">
      <h1 className="text-3xl font-bold text-gold mb-6">Emparejamiento</h1>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-1">
          <Button
            onClick={cancelarMatchmaking}
            className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md"
          >
            Cancelar emparejamiento
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          <Button
            onClick={jugarContraBot}
            loading={loading}
            className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md"
          >
            Jugar contra un bot
          </Button>
          <Button
            onClick={unirsePartidaAleatoriaWS}
            loading={loading}
            className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md"
          >
            Jugar contra un oponente aleatorio
          </Button>
        </div>
      )}     

      <div className="mt-6 w-full max-w-4xl">
        <h2 className="text-2xl font-semibold text-gold mb-4">
          Amigos Conectados
        </h2>
        <ListaAmigosConectados
          friends={amigosConectados}
          onSelect={handleSelectFriend}
        />
      </div>

      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)}>
        <h2 className="text-xl font-semibold text-gold mb-4">
          Invitar a un amigo
        </h2>
        {selectedFriend && (
          <div className="mt-4">
            <p className="text-lg text-black">
              ¿Enviar invitación a <strong>{selectedFriend.nickname}</strong>?
            </p>
            <div className="flex gap-4 mt-4">
              <Button
                onClick={() => invitarAmigo(selectedFriend.id)}
                loading={loading}
                className="bg-primary p-4 rounded-md hover:bg-dark text-white border-gold"
              >
                Sí
              </Button>
              <Button
                onClick={() => setModalOpen(false)}
                className="bg-gray-600 p-4 rounded-md hover:bg-gray-800 text-white border-gold"
              >
                Cancelar
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default MatchmakingPage;
