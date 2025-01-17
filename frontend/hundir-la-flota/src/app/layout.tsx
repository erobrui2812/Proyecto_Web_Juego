import { Geist, Geist_Mono } from "next/font/google";
import { Slide, ToastContainer } from "react-toastify";
import { GlobalProvider } from "./contexts/GlobalContext";
import "./globals.css";
import Layout from "./page-layout";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
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
          <Layout>{children}</Layout>
        </GlobalProvider>
      </body>
    </html>
  );
}
