import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{js,ts,jsx,tsx,mdx}', './components/**/*.{js,ts,jsx,tsx,mdx}', './lib/**/*.{js,ts,jsx,tsx,mdx}'],
  theme: {
    extend: {
      colors: {
        studio: {
          bg: '#07090f',
          panel: '#0d111c',
          card: '#121827',
          line: '#273247',
          gold: '#d6b56d',
          champagne: '#f2dfad'
        }
      },
      boxShadow: {
        glow: '0 0 40px rgba(214,181,109,0.16)'
      }
    },
  },
  plugins: [],
};
export default config;
