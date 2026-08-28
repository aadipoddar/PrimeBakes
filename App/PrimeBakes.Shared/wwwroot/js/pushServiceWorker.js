self.addEventListener('push', event => {
    if (!event.data)
        return;

    const payload = event.data.json();

    event.waitUntil(self.registration.showNotification(payload.title, {
        body: payload.body,
        icon: '_content/PrimeBakes.Shared/images/icon-192.png',
        badge: '_content/PrimeBakes.Shared/images/icon-192.png',
        vibrate: [100, 50, 100],
        data: { url: payload.url || '/' }
    }));
});

self.addEventListener('notificationclick', event => {
    event.notification.close();

    const url = new URL(event.notification.data?.url || '/', self.location.origin).href;

    event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windowClients => {
        for (const client of windowClients)
            if ('focus' in client)
                return client.focus();

        return clients.openWindow(url);
    }));
});
