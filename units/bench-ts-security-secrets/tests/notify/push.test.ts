import { beforeEach, describe, expect, it, vi } from "vitest";

const request = vi.fn();
const googleAuth = vi.fn();

vi.mock("google-auth-library", () => ({
  GoogleAuth: class {
    constructor(options: unknown) {
      googleAuth(options);
    }
    getProjectId(): Promise<string> {
      return Promise.resolve("media-push-test");
    }
    getClient(): Promise<{ request: typeof request }> {
      return Promise.resolve({ request });
    }
  },
}));

const { PushNotifier } = await import("../../src/notify/push.js");

describe("PushNotifier", () => {
  beforeEach(() => {
    request.mockReset().mockResolvedValue({ data: { name: "projects/media-push-test/messages/1" } });
  });

  it("sends a topic message through the FCM v1 endpoint with the configured key file", async () => {
    const notifier = new PushNotifier({ firebaseServiceAccountPath: "/run/secrets/fcm.json" });

    await notifier.send({ topic: "owner-studio-north", title: "Upload received", body: "image/png" });

    expect(googleAuth).toHaveBeenCalledWith(expect.objectContaining({ keyFile: "/run/secrets/fcm.json" }));
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: "https://fcm.googleapis.com/v1/projects/media-push-test/messages:send",
        method: "POST",
        timeout: 10_000,
        data: { message: { topic: "owner-studio-north", notification: { title: "Upload received", body: "image/png" } } },
      }),
    );
  });
});
