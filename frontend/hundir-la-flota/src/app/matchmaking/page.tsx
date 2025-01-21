"use client";

import Button from "@/components/Button";
import ListaAmigos from "@/components/ListaAmigos";
import Modal from "@/components/Modal";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

const MatchmakingPage = () => {
  const router = useRouter();
  const [amigosConectados, setAmigosConectados] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedFriend, setSelectedFriend] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);

  const getToken = () => {
    return localStorage.getItem("token") || sessionStorage.getItem("token");
  };

  useEffect(() => {
    const fetchAmigos = async () => {
      try {
        const token = getToken();
        if (!token)
          throw new Error("No se encontró el token de autenticación.");

        const response = await fetch(
          "https://localhost:7162/api/Friendship/connected",
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );
        if (!response.ok) throw new Error("Error al obtener amigos conectados");
        const data = await response.json();
        console.log("Amigos conectados:", data);
        setAmigosConectados(data);
      } catch (error) {
        toast.error("Error al obtener amigos conectados.");
        console.error("Error fetching connected friends:", error);
      }
    };

    fetchAmigos();
  }, []);

  const jugarContraBot = async () => {
    setLoading(true);
    try {
      const token = getToken();
      if (!token) throw new Error("No se encontró el token de autenticación.");

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
    setLoading(true);
    try {
      const token = getToken();
      if (!token) throw new Error("No se encontró el token de autenticación.");

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

  const invitarAmigo = async (amigoId) => {
    setLoading(true);
    try {
      const token = getToken();
      if (!token) throw new Error("No se encontró el token de autenticación.");

      const response = await fetch("https://localhost:7162/api/game/invite", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
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

  const handleSelectFriend = (friend) => {
    setSelectedFriend(friend);
    setModalOpen(true);
  };

  return (
    <div className="flex flex-col items-center justify-center py-10">
      <h1 className="text-2xl font-bold mb-6">Emparejamiento</h1>
      <div className="grid gap-4 md:grid-cols-3">
        <Button onClick={jugarContraBot} loading={loading}>
          Jugar contra un bot
        </Button>
        <Button onClick={unirsePartidaAleatoria} loading={loading}>
          Jugar contra un oponente aleatorio
        </Button>
        <Button onClick={() => setModalOpen(true)}>Invitar a un amigo</Button>
      </div>
      <ListaAmigos amigos={amigosConectados} />
      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)}>
        <h2 className="text-xl font-semibold mb-4">Invitar a un amigo</h2>
        <ListaAmigos amigos={amigosConectados} onSelect={handleSelectFriend} />
        {selectedFriend && (
          <div className="mt-4">
            <p>
              ¿Enviar invitación a{" "}
              <strong>{selectedFriend.FriendNickname}</strong>?
            </p>
            <div className="flex gap-4 mt-4">
              <Button
                onClick={() => invitarAmigo(selectedFriend.FriendId)}
                loading={loading}
              >
                Sí
              </Button>
              <Button onClick={() => setModalOpen(false)}>Cancelar</Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default MatchmakingPage;
