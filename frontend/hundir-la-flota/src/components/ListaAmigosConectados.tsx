"use client";

import { Friend } from "@/types/friendship";
import { useState } from "react";
import ReactPaginate from "react-paginate";

type ListaAmigosConectadosProps = {
  friends: Friend[];

  onSelect: (friend: Friend) => void;
};

const translateStatus = (status: string) => {
  switch (status) {
    case "Connected":
      return "Conectado";
    case "Disconnected":
      return "Desconectado";
    case "Playing":
      return "Jugando";
    default:
      return "Desconocido";
  }
};

const ListaAmigosConectados: React.FC<ListaAmigosConectadosProps> = ({
  friends,
  onSelect,
}) => {
  const [pageNumber, setPageNumber] = useState(0);
  const [friendsPerPage, setFriendsPerPage] = useState(3);

  const connectedFriends = friends.filter(
    (friend) => friend.status === "Connected"
  );

  const pagesVisited = pageNumber * friendsPerPage;
  const currentFriends = connectedFriends.slice(
    pagesVisited,
    pagesVisited + friendsPerPage
  );
  const pageCount = Math.ceil(connectedFriends.length / friendsPerPage);

  const changePage = (selectedItem: { selected: number }) => {
    setPageNumber(selectedItem.selected);
  };

  return (
    <div>
      <div className="flex items-center gap-2 mb-4">
        <label htmlFor="friendsPerPage" className="font-semibold">
          Amigos conectados por página:
        </label>
        <select
          id="friendsPerPage"
          value={friendsPerPage}
          onChange={(e) => {
            setFriendsPerPage(parseInt(e.target.value));
            setPageNumber(0);
          }}
          className="p-1 border rounded"
        >
          <option value={2}>2</option>
          <option value={3}>3</option>
          <option value={4}>4</option>
          <option value={8}>8</option>
        </select>
      </div>

      {connectedFriends.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {currentFriends.map((friend) => (
            <div
              key={`${friend.id}`}
              className="flex flex-col p-4 bg-gray-800 rounded-md shadow-md cursor-pointer"
              onClick={() => onSelect(friend)}
            >
              <div className="flex items-center mb-2">
                <img
                  src={friend.urlAvatar}
                  alt={`${friend.nickname}'s Avatar`}
                  className="w-10 h-10 rounded-full border-2 border-secondary mr-3"
                />
                <div>
                  <span className="font-semibold text-gold block">
                    {friend.nickname}
                  </span>
                  <span className="text-sm text-green-400">
                    {translateStatus(friend.status)}
                  </span>
                </div>
              </div>
              <p className="text-sm text-gray-200 mb-2">
                {friend.email || "Correo no disponible"}
              </p>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-gray-500">
          No hay amigos conectados disponibles para invitar.
        </p>
      )}

      <div className="mt-4">
        <ReactPaginate
          previousLabel={"< Anterior"}
          nextLabel={"Siguiente >"}
          pageCount={pageCount}
          onPageChange={changePage}
          forcePage={pageNumber}
          containerClassName={"flex justify-center items-center gap-2"}
          pageLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          previousLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          nextLinkClassName={
            "px-2 py-1 border rounded hover:bg-gray-300 transition"
          }
          disabledClassName={"opacity-50 cursor-not-allowed"}
          activeClassName={"bg-blueLink text-white"}
        />
      </div>
    </div>
  );
};

export default ListaAmigosConectados;
