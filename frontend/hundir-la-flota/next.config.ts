import type { NextConfig } from "next";

const nextConfig: NextConfig = {

  images: {
    domains: ['s3.amazonaws.com'],
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
};


export default nextConfig;
