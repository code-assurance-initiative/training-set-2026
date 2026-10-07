import { GoogleAuth } from "google-auth-library";

import type { ServiceConfig } from "../config.js";

const MESSAGING_SCOPE = "https://www.googleapis.com/auth/firebase.messaging";

export interface PushMessage {
  readonly topic: string;
  readonly title: string;
  readonly body: string;
}

export class PushNotifier {
  private readonly auth: GoogleAuth;

  constructor(config: Pick<ServiceConfig, "firebaseServiceAccountPath">) {
    this.auth = new GoogleAuth({ keyFile: config.firebaseServiceAccountPath, scopes: [MESSAGING_SCOPE] });
  }

  async send(message: PushMessage): Promise<void> {
    const projectId = await this.auth.getProjectId();
    const client = await this.auth.getClient();
    await client.request({
      url: `https://fcm.googleapis.com/v1/projects/${projectId}/messages:send`,
      method: "POST",
      timeout: 10_000,
      data: {
        message: {
          topic: message.topic,
          notification: { title: message.title, body: message.body },
        },
      },
    });
  }
}
