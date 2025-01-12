"use client";
import { Link } from "lucide-react";
import React, { useState } from "react";
import { useAuth } from "../contexts/AuthContext";

const LoginForm = () => {
  const { iniciarSesion } = useAuth();
  const [identificador, setIdentificador] = useState("");
  const [password, setPassword] = useState("");
  const [mantenerSesion, setMantenerSesion] = useState(false);
  const [error, setError] = useState("");

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await iniciarSesion(identificador, password, mantenerSesion);
    } catch (error: any) {
      setError(error.message);
    }
  };

  return (
    <div className="flex justify-center items-center h-screen">
      <div className="bg-white shadow-md rounded-lg p-6 w-full sm:w-96">
        <h2 className="text-2xl font-bold text-center mb-4">Iniciar sesión</h2>
        <form onSubmit={handleLogin}>
          <div className="mb-4">
            <label
              htmlFor="identificador"
              className="block text-sm font-medium text-gray-700"
            >
              Nickname/Email
            </label>
            <input
              type="text"
              id="identificador"
              value={identificador}
              onChange={(e) => setIdentificador(e.target.value)}
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
          <div className="flex items-center mb-4">
            <input
              type="checkbox"
              id="mantenerSesion"
              checked={mantenerSesion}
              onChange={(e) => setMantenerSesion(e.target.checked)}
              className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
            />
            <label
              htmlFor="mantenerSesion"
              className="ml-2 block text-sm text-gray-900"
            >
              Mantener sesión activa
            </label>
          </div>
          {error && <p className="text-red-500 text-sm mb-4">{error}</p>}
          <button
            type="submit"
            className="w-full bg-indigo-600 text-white py-2 px-4 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-opacity-50"
          >
            Iniciar sesión
          </button>
        </form>
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            ¿No tienes cuenta?{" "}
            <Link
              href="/register"
              className="text-indigo-600 hover:text-indigo-700 font-semibold"
            >
              Regístrate
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginForm;
