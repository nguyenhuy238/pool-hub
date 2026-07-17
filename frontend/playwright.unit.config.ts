import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./unit",
  timeout: 10_000,
  reporter: [["list"]]
});
