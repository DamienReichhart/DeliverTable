let stripe = null;
let elements = null;

export function initialize(publishableKey) {
    stripe = window.Stripe(publishableKey);
}

export function mountPaymentElement(clientSecret, domElementId) {
    elements = stripe.elements({ clientSecret, locale: 'fr' });
    const el = elements.create('payment', { layout: 'tabs' });
    el.mount(`#${domElementId}`);
}

export async function confirmPayment(returnUrl) {
    const result = await stripe.confirmPayment({
        elements,
        confirmParams: { return_url: returnUrl },
        redirect: 'if_required'
    });
    if (result.error) {
        return { succeeded: false, errorMessage: result.error.message, paymentIntentId: null };
    }
    return { succeeded: true, errorMessage: null, paymentIntentId: result.paymentIntent?.id ?? null };
}

export function unmount() {
    if (elements) elements = null;
}
