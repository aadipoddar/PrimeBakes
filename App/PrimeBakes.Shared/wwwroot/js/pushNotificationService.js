function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(base64);
    const output = new Uint8Array(raw.length);

    for (let i = 0; i < raw.length; i++)
        output[i] = raw.charCodeAt(i);

    return output;
}

function toBase64(buffer) {
    return window.btoa(String.fromCharCode.apply(null, new Uint8Array(buffer)));
}

window.pushNotificationsSupported = function () {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
};

window.requestPushSubscription = async function (publicKey) {
    if (!window.pushNotificationsSupported() || !publicKey)
        return null;

    try {
        const registration = await navigator.serviceWorker.ready;
        let subscription = await registration.pushManager.getSubscription();

        if (!subscription) {
            if (Notification.permission === 'denied')
                return null;

            if (Notification.permission !== 'granted' && await Notification.requestPermission() !== 'granted')
                return null;

            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicKey)
            });
        }

        return {
            endpoint: subscription.endpoint,
            p256dh: toBase64(subscription.getKey('p256dh')),
            auth: toBase64(subscription.getKey('auth'))
        };
    }
    catch {
        return null;
    }
};

window.removePushSubscription = async function () {
    if (!window.pushNotificationsSupported())
        return null;

    try {
        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();

        if (!subscription)
            return null;

        const endpoint = subscription.endpoint;
        await subscription.unsubscribe();

        return endpoint;
    }
    catch {
        return null;
    }
};

window.showLocalNotification = async function (title, body) {
    if (!('Notification' in window))
        return;

    try {
        if (Notification.permission !== 'granted' && await Notification.requestPermission() !== 'granted')
            return;

        const registration = await navigator.serviceWorker.ready;

        await registration.showNotification(title, {
            body: body,
            icon: '_content/PrimeBakes.Shared/images/icon-192.png',
            badge: '_content/PrimeBakes.Shared/images/icon-192.png',
            vibrate: [100, 50, 100]
        });
    }
    catch { }
};
