// src/types/friendship.d.ts
export type PendingRequest = {
  id: string;
  senderId: string;
  senderNickname: string;
  createdAt: string;
};

export type Friend = {
  id: string;
  nickname: string;
  email: string;
  urlAvatar?: string;
  status: "Connected" | "Disconnected" | "Playing";
}

export type GameResponse = {
  gameId: string;
  OpponentId?: string;
}

export interface ListaAmigosProps {
  amigos: Friend[];
  onSelect: (friend: Friend) => void;
}