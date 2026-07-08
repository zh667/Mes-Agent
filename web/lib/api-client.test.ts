import { afterEach, describe, expect, it } from "vitest";

import { getApiBaseUrl, workOrdersApi } from "./api-client";

const originalApiUrl = process.env.NEXT_PUBLIC_API_URL;

afterEach(() => {
  if (originalApiUrl === undefined) {
    delete process.env.NEXT_PUBLIC_API_URL;
  } else {
    process.env.NEXT_PUBLIC_API_URL = originalApiUrl;
  }
});

describe("getApiBaseUrl", () => {
  it("uses the local API default when no environment override is set", () => {
    delete process.env.NEXT_PUBLIC_API_URL;

    expect(getApiBaseUrl()).toBe("http://localhost:5000/api");
  });

  it("uses NEXT_PUBLIC_API_URL when configured", () => {
    process.env.NEXT_PUBLIC_API_URL = "https://mes.example.test/api";

    expect(getApiBaseUrl()).toBe("https://mes.example.test/api");
  });
});

describe("workOrdersApi", () => {
  it("targets the generated WorkOrders routes", () => {
    expect(workOrdersApi.routes.getAll).toBe("/WorkOrders");
    expect(workOrdersApi.routes.getById(42)).toBe("/WorkOrders/42");
  });
});
