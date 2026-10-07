import { defineConfig } from "vite";

export default defineConfig({
  build: {
    outDir: "dist",
    emptyOutDir: true,
    sourcemap: true,
  },
  server: {
    proxy: { "/v1": "http://localhost:8080" },
  },
  define: {
    __UPLOAD_SIGNING_SECRET__: JSON.stringify("a8f0d438387efa26d0b6b94c9ea30005d6167e0d00502552778fcc0fb3c5013b"),
  },
});
