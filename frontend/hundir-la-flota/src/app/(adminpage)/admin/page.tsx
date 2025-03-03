"use client";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import { toast } from "react-toastify";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

interface User {
  id: number;
  nickname: string;
  email: string;
  role: string;
  isBlocked: boolean;
}

export default function AdminPage() {
  const router = useRouter();
  const { auth,userDetail } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const currentUserId = userDetail?.id;

  useEffect(() => {
    if (!auth.token) {
      router.push("/");
      return;
    }

    async function fetchUsers() {
      try {
        const response = await fetch(`${API_URL}api/Users/list`, {
          method: "GET",
          headers: { Authorization: `Bearer ${auth.token}` },
        });

        if (!response.ok) throw new Error("Error al obtener usuarios");

        const result = await response.json();

        if (result.success && Array.isArray(result.data)) {
          setUsers(result.data);
        } else {
          throw new Error("Formato de respuesta incorrecto");
        }
      } catch (error) {
        console.error(error);
      } finally {
        setLoading(false);
      }
    }

    fetchUsers();
  }, [auth.token, router]);

  const handleBlockToggle = async (userId: number) => {
    if (String(userId) === String(currentUserId)) {
      toast.error("No puedes editarte a ti mismo");
      return;
    }
  
    try {
      const response = await fetch(`${API_URL}api/Admin/block/${userId}`, {
        method: "PUT",
        headers: { Authorization: `Bearer ${auth.token}` },
      });
  
      if (!response.ok) throw new Error("Error en la solicitud");
      
      setUsers((prevUsers) =>
        prevUsers.map(user =>
          user.id === userId ? { ...user, isBlocked: !user.isBlocked } : user
        )
      );
    } catch (error) {
      console.error(error);
      toast.error("Error al cambiar el estado de bloqueo");
    }
  };
  
  const handleRoleChange = async (userId: number, newRole: string) => {
    if (String(userId) === String(currentUserId)) {
      toast.error("No puedes editarte a ti mismo");
      return;
    }
    
    try {
      const response = await fetch(`${API_URL}api/Admin/change-role/${userId}`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${auth.token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(newRole),
      });
  
      if (!response.ok) throw new Error("Error en la solicitud");
  
      setUsers((prevUsers) =>
        prevUsers.map(user =>
          user.id === userId ? { ...user, role: newRole } : user
        )
      );
    } catch (error) {
      console.error(error);
      toast.error("Error al cambiar el rol");
    }
  };

  if (loading) return <p className="text-center text-lg">Cargando usuarios...</p>;

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">Panel de Administración</h1>
      <table className="w-full border-collapse border border-gray-300">
        <thead>
          <tr className="bg-gray-200">
            <th className="border p-2">ID</th>
            <th className="border p-2">Nickname</th>
            <th className="border p-2">Email</th>
            <th className="border p-2">Rol</th>
            <th className="border p-2">Bloqueado</th>
            <th className="border p-2">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.id} className="text-center">
              <td className="border p-2">{user.id}</td>
              <td className="border p-2">{user.nickname}</td>
              <td className="border p-2">{user.email}</td>
              <td className="border p-2">
                <select 
                  value={user.role} 
                  onChange={(e) => handleRoleChange(user.id, e.target.value)}
                  className="border rounded p-1"
                >
                  <option value="user">User</option>
                  <option value="admin">Admin</option>
                </select>
              </td>
              <td className="border p-2">
                <input 
                  type="checkbox" 
                  checked={user.isBlocked} 
                  onChange={() => handleBlockToggle(user.id)} 
                />
              </td>
              <td className="border p-2">
                <button 
                  className={`p-1 rounded ${user.isBlocked ? "bg-red-500 text-white" : "bg-green-500 text-white"}`}
                  onClick={() => handleBlockToggle(user.id)}
                >
                  {user.isBlocked ? "Desbloquear" : "Bloquear"}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
