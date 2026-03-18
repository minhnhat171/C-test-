// 1. Tạo map
const map = L.map('map').setView([10.762622, 106.660172], 15);

// 2. Map nền
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  attribution: '© OpenStreetMap'
}).addTo(map);

// 3. Icon đẹp hơn (optional)
const customIcon = L.icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
  iconSize: [30, 30]
});

// 4. Gọi API C#
fetch("http://localhost:5287/api/food")
  .then(res => res.json())
  .then(data => {

    data.forEach(item => {

      const marker = L.marker([item.lat, item.lng], { icon: customIcon })
        .addTo(map);

      marker.bindPopup(`
        <div style="width:150px">
          <h4>${item.name}</h4>
          <p>${item.description}</p>
        </div>
      `);

    });

  })
  .catch(err => console.error("Lỗi API:", err));