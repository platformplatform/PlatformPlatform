/** @type {import('next').NextConfig} */
const nextConfig = {
    reactStrictMode: true,
    output: 'standalone',
    experimental: {
      swcPlugins: [
        ['@lingui/swc-plugin', {}],
      ],
    },
};

export default nextConfig;
