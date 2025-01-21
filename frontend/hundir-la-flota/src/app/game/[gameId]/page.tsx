interface PartidaPageProps {
  params: { gameId: string };
}

export default async function PartidaPage ({  params }: PartidaPageProps) {
  const { gameId } = await params;

  return <p>El ID del juego es: {gameId}</p>;
}
