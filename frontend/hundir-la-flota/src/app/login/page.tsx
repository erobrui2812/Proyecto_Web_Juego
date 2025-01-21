"use client";

import LoginForm from "@/components/LoginForm";
import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

const LoginPage = () => {
  const router = useRouter();
  const { userDetail } = useAuth();
  useEffect(() => {
    if (userDetail) {
      router.push("/menu");
    }
  }, [userDetail, router]);

  return (
    <div className="flex justify-center items-center h-screen bg-background">
      <div className="bg-white shadow-md rounded-lg p-6 w-full sm:w-96">
        <h2 className="text-2xl font-bold text-center mb-4 text-primary">
          Iniciar sesión
        </h2>
        <LoginForm />
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            ¿No tienes cuenta?{" "}
            <a
              href="/registro"
              className="text-primary hover:text-wine font-semibold"
            >
              Regístrate
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
