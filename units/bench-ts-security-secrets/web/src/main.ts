import { confirmPlanUpgrade, mountPlanUpgrade } from "./checkout.js";
import { registerForPush } from "./firebase.js";

const form = document.querySelector<HTMLFormElement>("#upload-form");
const status = document.querySelector<HTMLParagraphElement>("#status");
const upgrade = document.querySelector<HTMLButtonElement>("#upgrade");
const paymentElement = document.querySelector<HTMLDivElement>("#payment-element");

function hex(buffer: ArrayBuffer): string {
  return [...new Uint8Array(buffer)].map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

function field(data: FormData, name: string): string {
  const value = data.get(name);
  return typeof value === "string" ? value : "";
}

/** Signs `<owner>.<timestamp>.<sha256 of the payload>` and returns the three request headers that carry it. */
async function signedHeaders(ownerId: string, payload: BufferSource): Promise<Record<string, string>> {
  const encoder = new TextEncoder();
  const timestamp = Math.floor(Date.now() / 1000);
  const bodyDigest = hex(await crypto.subtle.digest("SHA-256", payload));
  const key = await crypto.subtle.importKey(
    "raw",
    encoder.encode(__UPLOAD_SIGNING_SECRET__),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = hex(await crypto.subtle.sign("HMAC", key, encoder.encode(`${ownerId}.${timestamp}.${bodyDigest}`)));
  return { "x-owner-id": ownerId, "x-upload-timestamp": String(timestamp), "x-upload-signature": signature };
}

function setStatus(message: string): void {
  if (status !== null) {
    status.textContent = message;
  }
}

form?.addEventListener("submit", (event) => {
  event.preventDefault();
  const data = new FormData(form);
  const ownerId = field(data, "owner");
  const file = data.get("file");
  const email = field(data, "email");
  if (!(file instanceof File)) {
    return;
  }
  void (async () => {
    setStatus("Uploading…");
    const body = new FormData();
    body.append("file", file, file.name);
    const headers = await signedHeaders(ownerId, await file.arrayBuffer());
    if (email !== "") {
      headers["x-notify-email"] = email;
    }
    const response = await fetch("/v1/media", { method: "POST", headers, body });
    setStatus(response.ok ? "Uploaded." : `Upload failed (${response.status}).`);
    await registerForPush();
  })();
});

upgrade?.addEventListener("click", () => {
  const ownerId = document.querySelector<HTMLInputElement>("#owner")?.value ?? "";
  if (paymentElement === null || ownerId === "") {
    setStatus("Enter your workspace first.");
    return;
  }
  void (async () => {
    const headers = await signedHeaders(ownerId, new TextEncoder().encode("plan-upgrade"));
    const response = await fetch("/v1/plans/upgrade-intent", { method: "POST", headers });
    const { clientSecret } = (await response.json()) as { clientSecret: string };
    const elements = await mountPlanUpgrade(paymentElement, clientSecret);
    upgrade.onclick = async () => {
      const error = await confirmPlanUpgrade(elements, `${location.origin}/plan-upgraded`);
      setStatus(error ?? "Plan upgraded.");
    };
  })();
});
