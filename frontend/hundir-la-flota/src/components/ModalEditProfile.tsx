"use client";

import React from "react";
import { useForm } from "react-hook-form";
import Modal from "@/components/Modal";
import { toast } from "react-toastify";
import { useAuth } from "@/contexts/AuthContext";

type EditProfileFormInputs = {
  nickname: string;
  email: string;
  avatar: File | null;
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
};

type ModalEditarPerfilProps = {
  isOpen: boolean;
  onClose: () => void;
  initialData: { nickname: string; email: string; avatarUrl: string };
  onSubmit: (data: {
    nickname: string;
    email: string;
    avatar: File | null;
    currentPassword?: string;
    newPassword?: string;
  }) => Promise<void>;
};

const ModalEditarPerfil: React.FC<ModalEditarPerfilProps> = ({ isOpen, onClose, initialData }) => {
  const { auth } = useAuth();
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<EditProfileFormInputs>({
    defaultValues: {
      nickname: initialData.nickname,
      email: initialData.email,
    },
  });

  const handleAvatarChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const file = e.target.files[0];
      setValue("avatar", file);
    }
  };

  const handleFormSubmit = async (data: EditProfileFormInputs) => {
    if (data.newPassword) {
      if (!data.currentPassword) {
        toast.error("Debes ingresar la contraseña actual para cambiar la contraseña.");
        return;
      }

      if (data.newPassword !== data.confirmPassword) {
        toast.error("Las contraseñas no coinciden.");
        return;
      }
    }

    const formData = new FormData();
    formData.append("nickname", data.nickname);
    formData.append("email", data.email);

    if (data.avatar) {
      formData.append("avatar", data.avatar);
    }

    if (data.currentPassword) {
      formData.append("currentPassword", data.currentPassword);
      formData.append("newPassword", data.newPassword || "");
    }

    try {
      const token = auth.token;

      const response = await fetch("https://localhost:7162/api/Users/update", {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      });


      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || "Error al actualizar el perfil.");
      }

      toast.success("Perfil actualizado exitosamente.");
      onClose();
    } catch (error: any) {
      toast.error(error.message || "Error al actualizar el perfil.");
    }
  };

  return (
    <Modal title="Editar Perfil" isOpen={isOpen} onClose={onClose}>
      <form onSubmit={handleSubmit(handleFormSubmit)}>
        <div className="mb-4">
          <label htmlFor="nickname" className="block text-sm font-medium text-gray-700">
            Nickname
          </label>
          <input
            type="text"
            id="nickname"
            {...register("nickname", { required: "El nickname es obligatorio." })}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
          {errors.nickname && <p className="text-red-500 text-sm">{errors.nickname.message}</p>}
        </div>
        <div className="mb-4">
          <label htmlFor="email" className="block text-sm font-medium text-gray-700">
            Email
          </label>
          <input
            type="email"
            id="email"
            {...register("email", { required: "El email es obligatorio." })}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
          {errors.email && <p className="text-red-500 text-sm">{errors.email.message}</p>}
        </div>
        <div className="mb-4">
          <label htmlFor="avatar" className="block text-sm font-medium text-gray-700">
            Avatar
          </label>
          <input
            type="file"
            id="avatar"
            accept="image/*"
            onChange={handleAvatarChange}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
        </div>
        <div className="mb-4">
          <label htmlFor="currentPassword" className="block text-sm font-medium text-gray-700">
            Contraseña Actual
          </label>
          <input
            type="password"
            id="currentPassword"
            {...register("currentPassword")}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
        </div>
        <div className="mb-4">
          <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700">
            Nueva Contraseña
          </label>
          <input
            type="password"
            id="newPassword"
            {...register("newPassword")}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
        </div>
        <div className="mb-4">
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700">
            Confirmar Nueva Contraseña
          </label>
          <input
            type="password"
            id="confirmPassword"
            {...register("confirmPassword")}
            className="mt-1 block w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
          />
        </div>
        <button
          type="submit"
          className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-opacity-50"
        >
          Guardar cambios
        </button>
      </form>
    </Modal>
  );
};

export default ModalEditarPerfil;
