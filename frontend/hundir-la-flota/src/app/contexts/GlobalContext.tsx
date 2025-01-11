import React, { createContext, useContext } from "react";
import { AuthProvider, useAuth } from "./AuthContext";
import { GameProvider, useGame } from "./GameContext";
import { FriendshipProvider, useFriendship } from "./FriendshipContext";

// Crear un contexto global
const GlobalContext = createContext(null);

export const GlobalProvider = ({ children }: { children: React.ReactNode }) => {
  return (
    <AuthProvider>
      <GameProvider>
        <FriendshipProvider>
          <GlobalContext.Provider value={null}>{children}</GlobalContext.Provider>
        </FriendshipProvider>
      </GameProvider>
    </AuthProvider>
  );
};

// Hook para acceder a todos los contextos desde uno
export const useGlobalContext = () => {
  const auth = useAuth();
  const game = useGame();
  const friendship = useFriendship();

  return { auth, game, friendship };
};
