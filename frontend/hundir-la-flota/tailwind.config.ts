import type { Config } from "tailwindcss";

export default {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      fontFamily: {
        bebasneue: ["Bebas Neue", "cursive"],
        montserrat: ["Montserrat", "sans-serif"],
      },
      backgroundImage: {
        "fondo-mar": "url('/fondo-mar.jpg')",
      },
      colors: {
        background: "var(--background)",
        foreground: "var(--foreground)",
        primary: "#800020", // Burdeos
        secondary: "#1A1A40", // Azul oscuro
        grayLight: "#EDEDED", // Gris claro
        grayDark: "#222222", // Gris oscuro
        white: "#FFFFFF", // Blanco
        gold: "#E5C07B", // Dorado
      },
    },
  },
  plugins: [],
} satisfies Config;
