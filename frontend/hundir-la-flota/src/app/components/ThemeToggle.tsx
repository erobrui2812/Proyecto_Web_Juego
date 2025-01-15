"use client";

import { Eclipse, Sun } from "lucide-react";
import { useEffect, useState } from "react";

const ThemeToggle = () => {
  const [theme, setTheme] = useState<"light" | "dark">("light");

  useEffect(() => {
    const preferredTheme = window.matchMedia("(prefers-color-scheme: light)")
      .matches
      ? "dark"
      : "light";
    setTheme(preferredTheme);
    document.documentElement.classList.toggle(
      "dark",
      preferredTheme === "light"
    );
  }, []);

  const toggleTheme = () => {
    const newTheme = theme === "dark" ? "light" : "dark";
    setTheme(newTheme);
    document.documentElement.classList.toggle("dark", newTheme === "dark");
  };

  return (
    <button
      onClick={toggleTheme}
      className="bg-primary border text-lg px-4 py-2 rounded shadow hover:scale-105 hover:shadow-lg transition-all duration-200"
    >
      {theme === "dark" ? <Sun /> : <Eclipse />}
    </button>
  );
};

export default ThemeToggle;
