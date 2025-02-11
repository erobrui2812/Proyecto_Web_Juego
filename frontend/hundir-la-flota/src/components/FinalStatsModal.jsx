"use client";
import React from "react";
import GameSummary from "./GameSummary";

const FinalStatsModal = ({ isOpen, onClose, summary, onRematch }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="relative bg-gray-800 p-6 rounded shadow-lg max-w-lg w-full">
        <button
          onClick={onClose}
          className="absolute top-2 right-2 text-white hover:text-gray-400"
        >
          X
        </button>

        <GameSummary summary={summary} onRematch={onRematch} />
      </div>
    </div>
  );
};

export default FinalStatsModal;
