window.restaurantDetailMap = {
    _map: null,

    init: function (elementId, lat, lng, name) {
        if (this._map) {
            this._map.remove();
            this._map = null;
        }

        this._map = L.map(elementId, {
            zoomControl: true,
            scrollWheelZoom: false
        }).setView([lat, lng], 15);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(this._map);

        var marker = L.marker([lat, lng]).addTo(this._map);
        marker.bindPopup('<strong>' + this._escapeHtml(name) + '</strong>').openPopup();
    },

    dispose: function () {
        if (this._map) {
            this._map.remove();
            this._map = null;
        }
    },

    _escapeHtml: function (text) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }
};
