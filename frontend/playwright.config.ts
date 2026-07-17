import { defineConfig, devices } from "@playwright/test";
import type { PlaywrightTestConfig } from "@playwright/test";

const port = Number(process.env.E2E_PORT || 3000);
const baseURL = process.env.E2E_BASE_URL || `http://127.0.0.1:${port}`;
const browserChannel = process.env.E2E_BROWSER_CHANNEL || (process.platform === "win32" ? "msedge" : undefined);

const use: NonNullable<PlaywrightTestConfig["use"]> = {
  baseURL,
  trace: "retain-on-failure"
};

if (browserChannel) {
  use.channel = browserChannel;
}

const config: PlaywrightTestConfig = {
  testDir: "./e2e",
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"]],
  use,
  projects: [
    {
      name: "role-guard-smoke",
      use: { ...devices["Desktop Chrome"] }
    }
  ],
  webServer: {
    command: `npm run dev -- --hostname 127.0.0.1 --port ${port}`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056"
    }
  }
};

export default defineConfig(config);
