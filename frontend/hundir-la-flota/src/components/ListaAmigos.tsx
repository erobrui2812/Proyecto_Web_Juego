"use client";

import { Trash2 } from "lucide-react";
import { useFriendship } from "@/contexts/FriendshipContext";

const ListaAmigos = () => {
  const { friends, removeFriend } = useFriendship();

  return (
    <div className="space-y-4">
      {friends.map((friend) => (
        <div
          key={friend.id}
          className="flex items-center space-x-4 p-4 bg-gray-800 rounded-md shadow-md"
        >
          <img
            src={friend.urlAvatar}
            alt={`${friend.nickname}'s Avatar`}
            className="w-10 h-10 rounded-full border-2 border-secondary"
          />
          <div className="flex flex-col">
            <span className="font-semibold text-gold">{friend.nickname}</span>
            <span
              className={`text-sm ${
                friend.status === "Conectado"
                  ? "text-green-500"
                  : "text-gray-500"
              }`}
            >
              {friend.status}
            </span>
          </div>
          <button
            onClick={() => removeFriend(friend.id)}
            className="ml-auto text-red-500 hover:text-red-700"
          >
            <Trash2 size={20} />
          </button>
        </div>
      ))}
    </div>
  );
};

export default ListaAmigos;
