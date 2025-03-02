"use client";

import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

export default function AuthenticatedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated,userDetail } = useAuth();
  const router = useRouter();
  const [isCheckingAdmin, setIsCheckingAdmin] = useState(true);

  useEffect(() => {
    if (!userDetail) {
      return;
    }
  
    if (userDetail.rol !== "admin") {
      toast.info("Debes autenticarte como admin para entrar aquí.");
      router.replace("/login");
    }
  
    setIsCheckingAdmin(false);
  
  }, [router, userDetail]);
  

  if (isCheckingAdmin) {
    return <div className="flex items-center justify-center h-screen">Cargando...</div>;
  }

  if (!isAuthenticated) return null;

  return <>{children}</>;
}
