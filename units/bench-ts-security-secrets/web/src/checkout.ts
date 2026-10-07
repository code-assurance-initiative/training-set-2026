import { loadStripe, type Stripe, type StripeElements } from "@stripe/stripe-js";

const stripePromise: Promise<Stripe | null> = loadStripe(
  "pk_test_zns3yXR4CH9o4SlqXFFa1pZBSWJyP8DXHJYtyPJeC4thT26eYo7WShZiC18efjJgoTa0RyZSGppD3KKmcD6MC1btwJt3nwWAJpm",
);

export async function mountPlanUpgrade(container: HTMLElement, clientSecret: string): Promise<StripeElements> {
  const stripe = await stripePromise;
  if (stripe === null) {
    throw new Error("Stripe.js failed to load");
  }
  const elements = stripe.elements({ clientSecret });
  elements.create("payment").mount(container);
  return elements;
}

export async function confirmPlanUpgrade(elements: StripeElements, returnUrl: string): Promise<string | undefined> {
  const stripe = await stripePromise;
  if (stripe === null) {
    return "Stripe.js failed to load";
  }
  const { error } = await stripe.confirmPayment({ elements, confirmParams: { return_url: returnUrl } });
  return error?.message;
}
