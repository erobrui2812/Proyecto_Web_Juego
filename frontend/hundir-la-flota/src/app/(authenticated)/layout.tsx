"use client";

import { useAuth } from "@/contexts/AuthContext";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function AuthenticatedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated } = useAuth();
  const router = useRouter();
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);

  useEffect(() => {
    const timeout = setTimeout(() => {
      if (!isAuthenticated) {
        router.replace("/login");
      }
      setIsCheckingAuth(false);
    }, 1000);

    return () => clearTimeout(timeout);
  }, [isAuthenticated, router]);

  if (isCheckingAuth) {
    return <div className="flex items-center justify-center h-screen">Cargando...</div>;
  }

  if (!isAuthenticated) return null;

  return <>{children}</>;
}
