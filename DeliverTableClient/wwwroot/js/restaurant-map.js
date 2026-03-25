window.restaurantMap = {
    _map: null,
    _markers: [],
    _circle: null,
    _centerMarker: null,
    _dotNetRef: null,

    init: function (elementId, dotNetRef) {
        this._dotNetRef = dotNetRef;

        this._map = L.map(elementId, { zoomControl: true }).setView([46.6, 2.2], 6);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(this._map);

        this._map.on('click', function (e) {
            dotNetRef.invokeMethodAsync('OnMapClicked', e.latlng.lat, e.latlng.lng);
        });
    },

    updateCenter: function (lat, lng, radiusKm) {
        if (!this._map) return;

        if (this._centerMarker) {
            this._map.removeLayer(this._centerMarker);
        }

        var centerIcon = L.divIcon({
            className: 'map-center-icon',
            html: '<div style="width:12px;height:12px;background:#e74c3c;border:2px solid #fff;border-radius:50%;box-shadow:0 0 4px rgba(0,0,0,0.3);"></div>',
            iconSize: [12, 12],
            iconAnchor: [6, 6]
        });
        this._centerMarker = L.marker([lat, lng], { icon: centerIcon, interactive: false }).addTo(this._map);

        if (this._circle) {
            this._map.removeLayer(this._circle);
        }
        this._circle = L.circle([lat, lng], {
            radius: radiusKm * 1000,
            color: '#3388ff',
            fillColor: '#3388ff',
            fillOpacity: 0.1,
            weight: 2
        }).addTo(this._map);

        this._map.fitBounds(this._circle.getBounds(), { padding: [20, 20] });
    },

    updateRadius: function (lat, lng, radiusKm) {
        if (!this._map || !this._circle) return;

        this._circle.setRadius(radiusKm * 1000);
        this._circle.setLatLng([lat, lng]);
        this._map.fitBounds(this._circle.getBounds(), { padding: [20, 20] });
    },

    updateMarkers: function (restaurants) {
        if (!this._map) return;

        for (var i = 0; i < this._markers.length; i++) {
            this._map.removeLayer(this._markers[i]);
        }
        this._markers = [];

        for (var j = 0; j < restaurants.length; j++) {
            var r = restaurants[j];
            var marker = L.marker([r.latitude, r.longitude]).addTo(this._map);
            marker.bindPopup(
                '<div class="map-popup">' +
                '<strong>' + this._escapeHtml(r.name) + '</strong><br/>' +
                '<span class="map-popup-type">' + this._escapeHtml(r.type) + '</span><br/>' +
                '<a href="/restaurant/' + r.id + '">Voir le restaurant</a>' +
                '</div>'
            );
            this._markers.push(marker);
        }
    },

    dispose: function () {
        if (this._map) {
            this._map.remove();
            this._map = null;
        }
        this._markers = [];
        this._circle = null;
        this._centerMarker = null;
        this._dotNetRef = null;
    },

    invalidateSize: function () {
        if (this._map) {
            this._map.invalidateSize();
        }
    },

    _escapeHtml: function (text) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }
};
