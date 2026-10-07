/** Sends one RFC 5322 message. */
export interface Mailer {
  send(to: string, message: string): Promise<void>;
}
