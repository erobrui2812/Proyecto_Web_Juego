"use client"
import React from "react";
import { Trash2 } from "lucide-react";
import { useGlobalContext } from "../contexts/GlobalContext";
useGlobalContext

const FriendsPage = () => {
  const { friendship } = useGlobalContext();
  const { friends, removeFriend } = friendship;

  return (
    <div className="max-w-4xl mx-auto mt-8">
      <h1 className="text-2xl font-bold mb-4">Mis Amigos</h1>
      <div className="space-y-4">
        {friends.map((friend: any) => (
          <div key={friend.id} className="flex items-center space-x-4 p-4 border-b">
            <img
              src={friend.urlAvatar}
              alt={`${friend.nickname}'s Avatar`}
              className="w-8 h-8 rounded-full"
            />
            <div className="flex flex-col">
              <span className="font-semibold">{friend.nickname}</span>
              <span className="text-sm text-gray-500">{friend.email}</span>
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
    </div>
  );
};

export default FriendsPage;
