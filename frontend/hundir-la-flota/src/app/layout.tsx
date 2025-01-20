import { Slide, ToastContainer } from "react-toastify";
import { GlobalProvider } from "@/contexts/GlobalContext";
import Header from "@/components/HeaderGlobal";
import "./globals.css";
import "@fontsource/montserrat";



export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body
        className={`antialiased bg-background text-foreground min-h-screen font-montserrat flex flex-col transition-all duration-300`}
      >
        <GlobalProvider>
          <ToastContainer
            position="top-left"
            autoClose={5000}
            limit={5}
            hideProgressBar={false}
            newestOnTop={false}
            closeOnClick
            rtl={false}
            pauseOnFocusLoss={false}
            draggable={false}
            pauseOnHover={false}
            theme="dark"
            transition={Slide}
          />
          <Header />
          <main className="flex-1">{children}</main>

        </GlobalProvider>
      </body>
    </html>
  );
}