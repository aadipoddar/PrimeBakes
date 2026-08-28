window.locationMap = {
	show: function (key, points) {
		if (window.google && window.google.maps) {
			window.locationMap.render(points);
			return;
		}

		const script = document.createElement("script");
		script.src = "https://maps.googleapis.com/maps/api/js?key=" + key;
		script.onload = () => window.locationMap.render(points);
		document.head.appendChild(script);
	},

	render: function (points) {
		const element = document.getElementById("locationMap");
		if (!element || !points.length) return;

		const map = new google.maps.Map(element, { zoom: 5, center: { lat: points[0].lat, lng: points[0].lng } });
		const bounds = new google.maps.LatLngBounds();

		points.forEach(point => {
			const position = { lat: point.lat, lng: point.lng };
			new google.maps.Marker({ position: position, map: map, title: point.name });
			bounds.extend(position);
		});

		map.fitBounds(bounds);
	}
};
