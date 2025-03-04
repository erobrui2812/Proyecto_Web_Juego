"use client";

import { useEffect, useState } from "react";
import Modal from "@/components/Modal";
import { useFriendship } from "@/contexts/FriendshipContext";
import { PendingRequest } from "@/types/friendship";
const ModalSolicitudes = ({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) => {
  const { fetchPendingRequests, respondToFriendRequest } = useFriendship();
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);

  useEffect(() => {
    if (isOpen) {
      fetchPendingRequests().then((data) => setPendingRequests(data));
    }
  }, [isOpen]);

  return (
    <Modal title="Solicitudes de Amistad" isOpen={isOpen} onClose={onClose}>
      <div className="flex flex-col gap-4">
        {pendingRequests.length > 0 ? (
          pendingRequests.map((request) => (
            <div
              key={request.id}
              className="flex items-center justify-between p-4 bg-gray-700 rounded-md"
            >
              <div>
                <p className="font-semibold text-gold">{request.senderNickname}</p>
                <p className="text-sm text-silver">
                  Enviado el: {new Date(request.createdAt).toLocaleDateString()}
                </p>
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={() => respondToFriendRequest(request.senderId, true)}
                  className="p-2 bg-green-500 text-white rounded-md hover:bg-green-600"
                >
                  Aceptar
                </button>
                <button
                  onClick={() => respondToFriendRequest(request.senderId, false)}
                  className="p-2 bg-redError text-white rounded-md hover:bg-red-600"
                >
                  Rechazar
                </button>
              </div>
            </div>
          ))
        ) : (
          <p className="text-center text-silver">No hay solicitudes pendientes.</p>
        )}
      </div>
    </Modal>
  );
};

export default ModalSolicitudes;
