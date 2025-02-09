"use client";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";


interface GameSummaryData {
  winner: string;
  totalTurns: number;
  shipsRemaining: string;
}

interface PlayerStats {
  userId: number;
  nickname: string;
  gamesPlayed: number;
  gamesWon: number;
}

interface LeaderboardEntry {
  userId: number;
  nickname: string;
  gamesWon: number;
  totalGames: number;
}

interface GameSummaryProps {
  summary: GameSummaryData;
}

const GameSummary = ({ summary }: GameSummaryProps) => {
  const { auth, userDetail } = useAuth();
  const router = useRouter();
  const [playerStats, setPlayerStats] = useState<PlayerStats | null>(null);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);

  useEffect(() => {
    if (!auth?.token || !userDetail?.id) return;

    const fetchPlayerStats = async () => {
      try {
        const res = await fetch(`/api/stats/player/${userDetail.id}`, {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        });
        if (res.ok) {
          const data = await res.json();
          setPlayerStats(data);
        }
      } catch (error) {
        console.error("Error fetching player stats:", error);
      }
    };

    const fetchLeaderboard = async () => {
      try {
        const res = await fetch(`/api/stats/leaderboard`, {
          headers: {
            Authorization: `Bearer ${auth.token}`,
          },
        });
        if (res.ok) {
          const data = await res.json();
          setLeaderboard(data);
        }
      } catch (error) {
        console.error("Error fetching leaderboard:", error);
      }
    };

    fetchPlayerStats();
    fetchLeaderboard();
  }, [auth, userDetail]);

  const handleRematch = () => {
    router.push("/game/rematch");
  };

  return (
    <div className="p-6">
      <div className="bg-gray-800 text-white rounded p-4 mb-4">
        <h2 className="text-xl font-bold">Resumen de la partida</h2>
        <p>
          <strong>Ganador:</strong> {summary.winner}
        </p>
        <p>
          <strong>Total de turnos:</strong> {summary.totalTurns}
        </p>
        <p>
          <strong>Barcos restantes:</strong> {summary.shipsRemaining}
        </p>
      </div>

      {playerStats && (
        <div className="bg-gray-800 text-white rounded p-4 mb-4">
          <h3 className="text-lg font-bold">Tus estadísticas</h3>
          <p>
            <strong>Partidas jugadas:</strong> {playerStats.gamesPlayed}
          </p>
          <p>
            <strong>Partidas ganadas:</strong> {playerStats.gamesWon}
          </p>
        </div>
      )}

      {leaderboard.length > 0 && (
        <div className="bg-gray-800 text-white rounded p-4 mb-4">
          <h3 className="text-lg font-bold">Leaderboard</h3>
          <ul>
            {leaderboard.map((entry, index) => (
              <li key={entry.userId}>
                {index + 1}. {entry.nickname} - Ganadas: {entry.gamesWon} /
                Total: {entry.totalGames}
              </li>
            ))}
          </ul>
        </div>
      )}

      <button
        onClick={handleRematch}
        className="mt-4 bg-green-500 px-4 py-2 rounded hover:bg-green-600 transition"
      >
        Revancha
      </button>
    </div>
  );
};

export default GameSummary;
