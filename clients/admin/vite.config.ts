import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), "");
    const apiBase = env.VITE_API_BASE_URL ?? "http://localhost:5030";

    return {
        plugins: [react(), tailwindcss()],

        resolve: {
            alias: {
                "@": path.resolve(__dirname, "./src"),
            },
        },

        build: {
            rollupOptions: {
                onwarn(warning, warn) {
                    if (
                        warning.code === "INVALID_ANNOTATION" &&
                        warning.id?.includes("@microsoft/signalr")
                    ) {
                        return;
                    }

                    warn(warning);
                },

                output: {
                    manualChunks(id) {
                        const moduleId = id.replaceAll("\\", "/");

                        if (
                            moduleId.includes("/node_modules/react/") ||
                            moduleId.includes("/node_modules/react-dom/") ||
                            moduleId.includes("/node_modules/scheduler/")
                        ) {
                            return "react-vendor";
                        }

                        if (
                            moduleId.includes("/node_modules/react-router/") ||
                            moduleId.includes("/node_modules/react-router-dom/")
                        ) {
                            return "router-vendor";
                        }

                        if (
                            moduleId.includes("/node_modules/@tanstack/react-query/")
                        ) {
                            return "query-vendor";
                        }

                        if (
                            moduleId.includes("/node_modules/@microsoft/signalr/")
                        ) {
                            return "realtime-vendor";
                        }

                        return undefined;
                    },
                },
            },
        },

        server: {
            port: 5173,
            strictPort: true,
            proxy: {
                "/api": {
                    target: apiBase,
                    changeOrigin: true,
                    secure: false,
                },
                "/health": {
                    target: apiBase,
                    changeOrigin: true,
                    secure: false,
                },
                "/openapi": {
                    target: apiBase,
                    changeOrigin: true,
                    secure: false,
                },
                "/scalar": {
                    target: apiBase,
                    changeOrigin: true,
                    secure: false,
                },
            },
        },
    };
});