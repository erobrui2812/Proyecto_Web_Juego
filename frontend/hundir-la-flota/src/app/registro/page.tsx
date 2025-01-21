"use client";

import RegisterForm from "@/components/RegisterForm";
import { useAuth } from "@/contexts/AuthContext";
import { motion } from "framer-motion";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

const RegistroPage = () => {
  const router = useRouter();
  const { userDetail } = useAuth();

  useEffect(() => {
    if (userDetail) {
      router.push("/menu");
    }
  }, [userDetail, router]);

  return (
    <div className="relative flex justify-center items-center h-screen bg-white text-black">
      <div className="absolute top-10 left-10"></div>

      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        className="bg-white text-black rounded-lg shadow-lg p-8 w-96 border-2 border-primary"
      >
        <h2 className="text-2xl font-bold text-center text-wine mb-4">
          Registrarse
        </h2>
        <RegisterForm />
        <p className="text-sm text-center mt-4 text-primary">
          ¿Ya tienes cuenta?{" "}
          <a href="/login" className="text-wine font-bold underline">
            Inicia sesión
          </a>
        </p>
      </motion.div>
    </div>
  );
};

export default RegistroPage;
