window.appUpdate = {
	forceReload: async function () {
		if (window.caches)
			for (const key of await caches.keys())
				await caches.delete(key);

		if (navigator.serviceWorker)
			for (const registration of await navigator.serviceWorker.getRegistrations())
				await registration.unregister();

		location.reload();
	}
};
