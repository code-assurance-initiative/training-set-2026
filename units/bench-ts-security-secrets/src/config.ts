export interface ServiceConfig {
  readonly port: number;
  readonly publicBaseUrl: string;
  readonly databaseUrl: string | undefined;
  readonly auditMongoUri: string | undefined;
  readonly uploadSigningSecret: string | undefined;
  readonly stripeRestrictedKey: string | undefined;
  readonly sendgridApiKey: string | undefined;
  readonly slackBotToken: string | undefined;
  readonly slackDigestChannel: string;
  readonly transcoderBaseUrl: string;
  readonly webhookSigningKeyPath: string;
  readonly webhookSubscriberUrl: string | undefined;
  readonly firebaseServiceAccountPath: string;
  readonly maxUploadBytes: number;
}

export function loadConfig(env: NodeJS.ProcessEnv = process.env): ServiceConfig {
  return {
    port: Number(env.PORT ?? "8080"),
    publicBaseUrl: env.PUBLIC_BASE_URL ?? "http://localhost:8080",
    databaseUrl: env.DATABASE_URL,
    auditMongoUri: env.AUDIT_MONGO_URI,
    uploadSigningSecret: env.UPLOAD_SIGNING_SECRET,
    stripeRestrictedKey: env.STRIPE_RESTRICTED_KEY,
    sendgridApiKey: env.SENDGRID_API_KEY,
    slackBotToken: env.SLACK_BOT_TOKEN,
    slackDigestChannel: env.SLACK_DIGEST_CHANNEL ?? "#media-uploads",
    transcoderBaseUrl: env.TRANSCODER_BASE_URL ?? "https://api.transcoder.invalid/v2",
    webhookSigningKeyPath: env.WEBHOOK_SIGNING_KEY_PATH ?? "keys/webhook-signing.pem",
    webhookSubscriberUrl: env.WEBHOOK_SUBSCRIBER_URL,
    firebaseServiceAccountPath: env.FIREBASE_SERVICE_ACCOUNT_PATH ?? "config/firebase-service-account.json",
    maxUploadBytes: Number(env.MAX_UPLOAD_BYTES ?? String(50 * 1024 * 1024)),
  };
}
