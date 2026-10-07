import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import svgr from "vite-plugin-svgr";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const isProduction = mode === "production";
  return {
    plugins: [react(), svgr()],
    define: {
      __APP_VERSION__: JSON.stringify("dev"),
      __BUILD_TIME__: JSON.stringify(new Date().toISOString()),
    },
    server: {
      port: 3000,
      open: true,
      host: true,
    },
    build: {
      sourcemap: !isProduction,
      outDir: "dist",
      emptyOutDir: true,
      rollupOptions: {
        output: {
          manualChunks: {
            playcanvas: ["playcanvas"],
            react: ["react", "react-dom"],
            router: ["react-router-dom"],
            state: ["zustand"],
            ui: ["react", "react-dom"],
          },
        },
      },
    },
    test: {
      globals: true,
      environment: "jsdom",
      setupFiles: ["./tests/setup.ts"],
      include: ["tests/**/*.{test,spec}.{ts,tsx}"],
      coverage: {
        reporter: ["text", "json", "html"],
        exclude: ["node_modules/", "dist/", "scripts/"],
      },
    },
  };
});
