window.getCurrentLocation = function () {
    return new Promise((resolve) => {
        if (!navigator.geolocation) {
            resolve(null);
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            }),
            () => resolve(null),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 1800000 });
    });
};
