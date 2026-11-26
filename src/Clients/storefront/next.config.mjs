/** @type {import('next').NextConfig} */
const mfeShellUrl = process.env.MFE_SHELL_URL || 'http://localhost:3000';
const backofficeUrl = process.env.BACKOFFICE_URL || 'http://localhost:3101';

const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: '/desk',
        destination: `${mfeShellUrl}/`,
      },
      {
        source: '/desk/:path*',
        destination: `${mfeShellUrl}/:path*`,
      },
      {
        source: '/backoffice',
        destination: `${backofficeUrl}/backoffice`,
      },
      {
        source: '/backoffice/:path*',
        destination: `${backofficeUrl}/backoffice/:path*`,
      },
    ];
  },
};

export default nextConfig;
