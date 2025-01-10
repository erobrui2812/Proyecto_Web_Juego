import ThemeToggle from "./components/ThemeToggle";
import "./globals.css";

const Layout = ({ children }: { children: React.ReactNode }) => {
  return (
    <div className="bg-background text-foreground min-h-screen flex flex-col">
      <header className="p-4 bg-primary text-white">
        <h1>Mi Aplicación</h1>
        <ThemeToggle />
      </header>
      <main className="flex-1">
        {children}
      </main>
      <footer className="p-4 bg-secondary text-white">
        <p>Pie de página</p>
      </footer>
    </div>
  );
};

export default Layout;