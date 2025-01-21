"use client";

import { motion } from "framer-motion";
import { FaLock, FaUser } from "react-icons/fa";

const LoginPage = () => {
  return (
    <div className="relative flex justify-center items-center h-screen bg-white text-black">
      <div className="absolute top-10 left-10"></div>

      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        className="bg-white text-black rounded-lg shadow-lg p-8 w-96 border-2 border-primary"
      >
        <h2 className="text-2xl font-bold text-center text-wine mb-4">
          Iniciar sesión
        </h2>
        <form>
          <div className="mb-4">
            <label
              htmlFor="email"
              className="block text-sm font-semibold text-primary"
            >
              Nickname/Email
            </label>
            <div className="flex items-center border rounded-md px-3 mt-1 border-secondary">
              <FaUser className="text-gray-400 mr-2" />
              <input
                type="text"
                id="email"
                className="w-full py-2 focus:outline-none text-secondary"
                placeholder="Introduce tu Nickname o Email"
              />
            </div>
          </div>
          <div className="mb-4">
            <label
              htmlFor="password"
              className="block text-sm font-semibold text-primary"
            >
              Contraseña
            </label>
            <div className="flex items-center border rounded-md px-3 mt-1 border-secondary">
              <FaLock className="text-gray-400 mr-2" />
              <input
                type="password"
                id="password"
                className="w-full py-2 focus:outline-none text-secondary"
                placeholder="Introduce tu contraseña"
              />
            </div>
          </div>
          <div className="flex items-center mb-4">
            <input
              type="checkbox"
              id="mantenerSesion"
              className="mr-2 accent-wine"
            />
            <label htmlFor="mantenerSesion" className="text-sm text-primary">
              Mantener sesión activa
            </label>
          </div>
          <button
            type="submit"
            className="w-full bg-wine text-white py-2 rounded-md hover:bg-navy transition focus:ring-2 focus:ring-primary focus:ring-opacity-50"
          >
            Iniciar sesión
          </button>
        </form>
        <p className="text-sm text-center mt-4 text-primary">
          ¿No tienes cuenta?{" "}
          <a href="/registro" className="text-wine font-bold underline">
            Regístrate
          </a>
        </p>
      </motion.div>
    </div>
  );
};

export default LoginPage;
