/** @type {import('next').NextConfig} */
const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";

const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [{ source: "/uploads/:path*", destination: `${apiBaseUrl}/uploads/:path*` }];
  }
};

export default nextConfig;
