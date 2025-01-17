"use client";

import Link from "next/link";
import { useForm } from "react-hook-form";
import { useAuth } from "../contexts/AuthContext";

type LoginFormInputs = {
  identificador: string;
  password: string;
  mantenerSesion: boolean;
};

const LoginForm = () => {
  const { iniciarSesion } = useAuth();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormInputs>();

  const onSubmit = async (data: LoginFormInputs) => {
    try {
      await iniciarSesion(
        data.identificador,
        data.password,
        data.mantenerSesion
      );
    } catch (error: any) {
      console.error("Error al iniciar sesión:", error.message);
    }
  };

  return (
    <div className="flex justify-center items-center h-screen bg-background">
      <div className="bg-white shadow-md rounded-lg p-6 w-full sm:w-96">
        <h2 className="text-2xl font-bold text-center mb-4 text-primary">
          Iniciar sesión
        </h2>
        <form onSubmit={handleSubmit(onSubmit)}>
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
              {...register("identificador", {
                required: "Este campo es obligatorio",
              })}
              className={`mt-1 block w-full px-3 py-2 border ${
                errors.identificador ? "border-red-500" : "border-gray-300"
              } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
            />
            {errors.identificador && (
              <p className="text-red-500 text-sm">
                {errors.identificador.message}
              </p>
            )}
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
              {...register("password", {
                required: "Este campo es obligatorio",
              })}
              className={`mt-1 block w-full px-3 py-2 border ${
                errors.password ? "border-red-500" : "border-gray-300"
              } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
            />
            {errors.password && (
              <p className="text-red-500 text-sm">{errors.password.message}</p>
            )}
          </div>
          <div className="flex items-center mb-4">
            <input
              type="checkbox"
              id="mantenerSesion"
              {...register("mantenerSesion")}
              className="h-4 w-4 text-primary focus:ring-primary border-gray-300 rounded"
            />
            <label
              htmlFor="mantenerSesion"
              className="ml-2 block text-sm text-gray-900"
            >
              Mantener sesión activa
            </label>
          </div>
          <button
            type="submit"
            className="w-full bg-primary text-white py-2 px-4 rounded-md hover:bg-wine focus:outline-none focus:ring-2 focus:ring-primary focus:ring-opacity-50"
          >
            Iniciar sesión
          </button>
        </form>
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            ¿No tienes cuenta?{" "}
            <Link
              href="/registro"
              className="text-primary hover:text-wine font-semibold"
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
