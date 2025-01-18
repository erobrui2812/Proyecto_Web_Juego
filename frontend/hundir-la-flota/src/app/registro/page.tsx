import Link from "next/link";
import RegisterForm from "@/components/RegisterForm";

const RegistroPage = () => {
  return (
    <div className="flex justify-center items-center h-screen bg-background">
      <div className="bg-white shadow-md rounded-lg p-6 w-full sm:w-96">
        <h2 className="text-2xl font-bold text-center mb-4 text-primary">
          Registrarse
        </h2>
        <RegisterForm />
        <div className="mt-4 text-center">
          <p className="text-sm text-gray-600">
            ¿Ya tienes cuenta?{" "}
            <Link
              href="/login"
              className="text-primary hover:text-wine font-semibold"
            >
              Inicia sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default RegistroPage;
