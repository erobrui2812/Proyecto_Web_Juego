"use client";

import Button from "@/components/Button";
import ListaAmigosConectados from "@/components/ListaAmigosConectados";
import Modal from "@/components/Modal";
import { useAuth } from "@/contexts/AuthContext";
import { Friend } from "@/types/friendship";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

const MatchmakingPage = () => {
  const { auth } = useAuth();
  const token = auth?.token || null;
  const router = useRouter();

  const [amigosConectados, setAmigosConectados] = useState<Friend[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedFriend, setSelectedFriend] = useState<Friend | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    const fetchAmigos = async () => {
      if (!token) {
        console.warn("No se encontró el token de autenticación.");
        return;
      }

      try {
        const response = await fetch(
          "https://localhost:7162/api/Friendship/connected",
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (!response.ok) throw new Error("Error al obtener amigos conectados");

        const data: Friend[] = await response.json();
        console.log("Amigos conectados:", data);
        setAmigosConectados(data);
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
        "https://localhost:7162/api/game/play-with-bot",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
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

  const unirsePartidaAleatoria = async () => {
    if (!token) {
      toast.error("Debes iniciar sesión para unirte a una partida.");
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(
        "https://localhost:7162/api/game/join-random-match",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      if (!response.ok) throw new Error("Error al unirse a partida aleatoria");

      const data = await response.json();
      if (data.OpponentId) {
        toast.success("¡Emparejado con un oponente!");
        router.push(`/game/${data.OpponentId}`);
      } else {
        toast.info("Esperando a un oponente...");
      }
    } catch (error) {
      toast.error("Error al unirse a partida aleatoria.");
      console.error("Error joining random match:", error);
    } finally {
      setLoading(false);
    }
  };

  const invitarAmigo = async (amigoId: string) => {
    if (!token) {
      toast.error("Debes iniciar sesión para invitar a un amigo.");
      return;
    }

    setLoading(true);
    try {
      const response = await fetch("https://localhost:7162/api/game/invite", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ friendId: amigoId }),
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
      <div className="grid gap-4 md:grid-cols-3">
        <Button
          onClick={jugarContraBot}
          loading={loading}
          className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md bg-primary hover:bg-dark text-white border-gold"
        >
          Jugar contra un bot
        </Button>
        <Button
          onClick={unirsePartidaAleatoria}
          loading={loading}
          className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md bg-primary hover:bg-dark text-white border-gold"
        >
          Jugar contra un oponente aleatorio
        </Button>
        <Button
          onClick={() => setModalOpen(true)}
          className="px-4 py-2 bg-primary hover:bg-dark text-white border-gold rounded-lg shadow-md bg-primary hover:bg-dark text-white border-gold"
        >
          Invitar a un amigo
        </Button>
      </div>

      <div className="mt-6 w-full max-w-4xl">
        <h2 className="text-2xl font-semibold text-gold mb-4">Amigos Conectados</h2>
        <ListaAmigosConectados
          friends={amigosConectados}
          onSelect={handleSelectFriend}
        />
      </div>

      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)}>
        <h2 className="text-xl font-semibold text-gold mb-4">Invitar a un amigo</h2>
        {selectedFriend && (
          <div className="mt-4">
            <p className="text-lg text-white">
              ¿Enviar invitación a <strong>{selectedFriend.nickname}</strong>?
            </p>
            <div className="flex gap-4 mt-4">
              <Button
                onClick={() => invitarAmigo(selectedFriend.id)}
                loading={loading}
                className="bg-primary hover:bg-dark text-white border-gold"
              >
                Sí
              </Button>
              <Button
                onClick={() => setModalOpen(false)}
                className="bg-gray-600 hover:bg-gray-800 text-white border-gold"
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
