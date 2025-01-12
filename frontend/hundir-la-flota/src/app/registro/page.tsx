"use client";
import { Link } from "lucide-react";
import React, { useState } from "react";
import { useAuth } from "../contexts/AuthContext";

const RegisterPage = () => {
  const { registrarUsuario } = useAuth();
  const [nickname, setNickname] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [avatarUrl, setAvatarUrl] = useState("");
  const [error, setError] = useState<string>("");

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (password !== confirmPassword) {
        throw new Error("Las contraseñas no coinciden");
      }

      await registrarUsuario(
        nickname,
        email,
        password,
        confirmPassword,
        avatarUrl
      );
    } catch (error: any) {
      setError(error.message || "Error desconocido");
    }
  };

  return (
    <div className="flex justify-center items-center h-screen">
      <div className="bg-white shadow-md rounded-lg p-6 w-full sm:w-96">
        <h2 className="text-2xl font-bold text-center mb-4">Registrarse</h2>
        <form onSubmit={handleRegister}>
          <div className="mb-4">
            <label
              htmlFor="nickname"
              className="block text-sm font-medium text-gray-700"
            >
              Nickname
            </label>
            <input
              type="text"
              id="nickname"
              value={nickname}
              onChange={(e) => setNickname(e.target.value)}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              required
            />
          </div>
          <div className="mb-4">
            <label
              htmlFor="email"
              className="block text-sm font-medium text-gray-700"
            >
              Email
            </label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              required
            />
          </div>
          <div className="mb-4">
            <label
              htmlFor="password"
              className="block text-sm font-medium text-gray-700"
            >
              Contraseña
            </label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              required
            />
          </div>
          <div className="mb-4">
            <label
              htmlFor="confirmPassword"
              className="block text-sm font-medium text-gray-700"
            >
              Confirmar Contraseña
            </label>
            <input
              type="password"
              id="confirmPassword"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              required
            />
          </div>
          <div className="mb-4">
            <label
              htmlFor="avatarUrl"
              className="block text-sm font-medium text-gray-700"
            >
              URL del Avatar
            </label>
            <input
              type="text"
              id="avatarUrl"
              value={avatarUrl}
              onChange={(e) => setAvatarUrl(e.target.value)}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
            />
          </div>
          {error && <p className="text-red-500 text-sm mb-4">{error}</p>}
          <button
            type="submit"
            className="w-full bg-indigo-600 text-white py-2 px-4 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-opacity-50"
          >
            Registrarse
          </button>
        </form>
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            ¿Ya tienes cuenta?{" "}
            <Link
              href="/login"
              className="text-indigo-600 hover:text-indigo-700 font-semibold"
            >
              Inicia sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
