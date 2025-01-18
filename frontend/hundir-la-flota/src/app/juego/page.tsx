import GameGrid from "@/components/GameGrid"; // Asegúrate de ajustar la ruta si es diferente.

const Juego = () => {
  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col items-center justify-center space-y-8">
      <h1 className="text-4xl font-bold text-primary mb-6">Hundir la Flota</h1>
      <p className="text-lg text-secondary">
        Arrastra tus barcos al tablero para empezar el juego.
      </p>
      <GameGrid />
    </div>
  );
};

export default Juego;
