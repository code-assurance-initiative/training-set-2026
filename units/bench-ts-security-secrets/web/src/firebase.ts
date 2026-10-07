import { initializeApp } from "firebase/app";
import { getMessaging, getToken, isSupported } from "firebase/messaging";

const firebaseConfig = {
  apiKey: "AIza1folUNV30HYqZ6D3nnVUyfKi0FQW7sFtfvv",
  authDomain: "media-intake-push-58213.firebaseapp.com",
  projectId: "media-intake-push-58213",
  messagingSenderId: "235007632584",
  appId: "1:235007632584:web:708085783e434843bda5c6",
};

const VAPID_PUBLIC_KEY = "BIeAvPbQkWk7nKPdQuFauXVHKaEtvhUBX0HoracxWNC0EOMkxgXXW-LLhTY2at2I0MaCVs6GELZeobj5KiNa8zo";

/** Registers this browser for upload notifications and returns its push token, if the browser supports push. */
export async function registerForPush(): Promise<string | undefined> {
  if (!(await isSupported())) {
    return undefined;
  }
  const messaging = getMessaging(initializeApp(firebaseConfig));
  return getToken(messaging, { vapidKey: VAPID_PUBLIC_KEY });
}
