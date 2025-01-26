"use client";

import { useRouter } from "next/navigation";

const BotonJugar = () => {
  const router = useRouter();

  const handleClick = () => {
    router.push("/matchmaking");
  };

  return (
    <button
      className="border-2 border-gold p-2 bg-dark hover:scale-105 text-white font-bold py-2 px-4 rounded-md w-full"
      onClick={handleClick}
    >
      Jugar
    </button>
  );
};

export default BotonJugar;
