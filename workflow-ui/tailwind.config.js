/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#2E75D1',
          secondary: '#8CB3E6',
        },
        alert: {
          fatal: '#B20000',
          critical: '#E22A11',
          major: '#FB8C00',
          minor: '#FDD835',
          degraded: '#00ACC1',
          info: '#417ABB',
          normal: '#43A047',
        },
        icon: {
          primary: '#535E6D',
          secondary: '#798799',
          disabled: '#82878C',
          error: '#A21515',
          light: '#F5F5F5',
        }
      },
      fontFamily: {
        sans: ['Noto Sans', 'sans-serif'],
      }
    }
  },
  plugins: [],
}
