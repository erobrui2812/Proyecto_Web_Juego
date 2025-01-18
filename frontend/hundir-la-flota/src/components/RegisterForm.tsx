"use client";

import { useForm } from "react-hook-form";
import { useAuth } from "@/contexts/AuthContext";

type RegisterFormInputs = {
  nickname: string;
  email: string;
  password: string;
  confirmPassword: string;
  avatarUrl: string;
};

const RegisterForm = () => {
  const { registrarUsuario } = useAuth();
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegisterFormInputs>();

  const onSubmit = async (data: RegisterFormInputs) => {
    try {
      await registrarUsuario(
        data.nickname,
        data.email,
        data.password,
        data.confirmPassword,
        data.avatarUrl
      );
    } catch (error: any) {
      console.error("Error al registrar usuario:", error.message);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
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
          {...register("nickname", {
            required: "Este campo es obligatorio",
          })}
          className={`mt-1 block w-full px-3 py-2 border ${errors.nickname ? "border-red-500" : "border-gray-300"
            } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
        />
        {errors.nickname && (
          <p className="text-red-500 text-sm">{errors.nickname.message}</p>
        )}
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
          {...register("email", {
            required: "Este campo es obligatorio",
            pattern: {
              value: /^[a-zA-Z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,4}$/,
              message: "Email no válido",
            },
          })}
          className={`mt-1 block w-full px-3 py-2 border ${errors.email ? "border-red-500" : "border-gray-300"
            } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
        />
        {errors.email && (
          <p className="text-red-500 text-sm">{errors.email.message}</p>
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
          className={`mt-1 block w-full px-3 py-2 border ${errors.password ? "border-red-500" : "border-gray-300"
            } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
        />
        {errors.password && (
          <p className="text-red-500 text-sm">{errors.password.message}</p>
        )}
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
          {...register("confirmPassword", {
            required: "Este campo es obligatorio",
            validate: (value) =>
              value === watch("password") || "Las contraseñas no coinciden",
          })}
          className={`mt-1 block w-full px-3 py-2 border ${errors.confirmPassword ? "border-red-500" : "border-gray-300"
            } rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm`}
        />
        {errors.confirmPassword && (
          <p className="text-red-500 text-sm">
            {errors.confirmPassword.message}
          </p>
        )}
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
          {...register("avatarUrl")}
          className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-primary focus:border-primary sm:text-sm"
        />
      </div>
      <button
        type="submit"
        className="w-full bg-primary text-white py-2 px-4 rounded-md hover:bg-wine focus:outline-none focus:ring-2 focus:ring-primary focus:ring-opacity-50"
      >
        Registrarse
      </button>
    </form>
  );
};

export default RegisterForm;
