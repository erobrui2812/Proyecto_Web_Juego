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
        primary: "#800020", // rojo vino
        secondary: "#1A1A40", // azul marino oscuro
        gold: "#E5C07B", // dorado suave
        silver: "#B0B0B0", // gris metálico
      },
    },
  },
  plugins: [],
} satisfies Config;
