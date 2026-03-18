namespace VinhKhanhGuide.App;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        mapView.Source = new HtmlWebViewSource
        {
            Html = GetMapHtml()
        };
    }

    private string GetMapHtml()
    {
        return @"
<!DOCTYPE html>
<html>
<head>

<meta name='viewport' content='width=device-width, initial-scale=1.0'>

<link rel='stylesheet'
href='https://unpkg.com/leaflet/dist/leaflet.css'/>

<script
src='https://unpkg.com/leaflet/dist/leaflet.js'></script>

<style>
html, body {
height:100%;
margin:0;
}

#map{
height:100%;
}
</style>

</head>

<body>

<div id='map'></div>

<script>

var map = L.map('map').setView([10.757,106.703],16);

L.tileLayer(
'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
{
attribution:'© OpenStreetMap'
}).addTo(map);

L.marker([10.757,106.703])
.addTo(map)
.bindPopup('Phố ẩm thực Vĩnh Khánh')
.openPopup();

</script>

</body>
</html>
";
    }
}