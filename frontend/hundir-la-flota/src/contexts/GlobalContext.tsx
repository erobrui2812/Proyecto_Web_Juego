"use client"
import React, { createContext } from "react";
import { AuthProvider, useAuth } from "./AuthContext";
import { GameProvider, useGame } from "./GameContext";
import { FriendshipProvider, useFriendship } from "./FriendshipContext";
import { WebsocketProvider } from "./WebsocketContext";
const GlobalContext = createContext(null);

export const GlobalProvider = ({ children }: { children: React.ReactNode }) => {
  return (
    <AuthProvider>
      <GameProvider>
        <FriendshipProvider>
          <WebsocketProvider>
            <GlobalContext.Provider value={null}>{children}</GlobalContext.Provider>
          </WebsocketProvider>
        </FriendshipProvider>
      </GameProvider>
    </AuthProvider>
  );
};

export const useGlobalContext = () => {
  const auth = useAuth();
  const game = useGame();
  const friendship = useFriendship();

  return { auth, game, friendship };
};
