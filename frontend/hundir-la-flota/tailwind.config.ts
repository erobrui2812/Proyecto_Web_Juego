import type { Config } from "tailwindcss";

export default {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  safelist: [
    "bg-red-500",
    "bg-red-600",
    "bg-red-700",
    "hover:bg-red-500",
    "hover:bg-red-600",
    "hover:bg-red-700",
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
        primary: "#800020",
        secondary: "#1A1A40",
        grayLight: "#EDEDED",
        grayDark: "#222222",
        white: "#FFFFFF",
        gold: "#E5C07B",
        blueLink: "#60a5fa",
        dark: "#111827",
        redError: "#ef4444"
      },
    },
  },
  plugins: [],
} satisfies Config;
