/** @type {import('next').NextConfig} */
const nextConfig = {
  images: {
    // Permitir protocolo HTTP y HTTPS desde cualquier hostname ( ** )
    remotePatterns: [
      {
        protocol: "http",
        hostname: "**",
      },
      {
        protocol: "https",
        hostname: "**",
      },
    ],
  },
  eslint: {
    ignoreDuringBuilds: true,
  },
};

module.exports = nextConfig;
