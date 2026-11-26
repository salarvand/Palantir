/** @type {import('next').NextConfig} */
const mfeShellUrl = process.env.MFE_SHELL_URL || 'http://localhost:3000';

const nextConfig = {
  basePath: '/backoffice',
  reactStrictMode: true,
  async redirects() {
    return [
      {
        source: '/',
        destination: '/backoffice',
        basePath: false,
        permanent: false,
      },
    ];
  },
  async rewrites() {
    return [
      {
        source: '/desk',
        destination: `${mfeShellUrl}/`,
        basePath: false,
      },
      {
        source: '/desk/:path*',
        destination: `${mfeShellUrl}/:path*`,
        basePath: false,
      },
    ];
  },
};

export default nextConfig;
