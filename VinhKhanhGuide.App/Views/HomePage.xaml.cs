using Microsoft.Maui.Controls;

namespace VinhKhanhGuide.App.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <link rel='stylesheet' href='https://unpkg.com/leaflet/dist/leaflet.css' />
            <script src='https://unpkg.com/leaflet/dist/leaflet.js'></script>
            <style>
                html, body { margin:0; padding:0; height:100%; }
                #map { height:100%; }
            </style>
        </head>
        <body>
            <div id='map'></div>
            <script>
                var map = L.map('map').setView([10.755, 106.660], 15);

                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: 'Leaflet'
                }).addTo(map);

                L.marker([10.755, 106.660])
                    .addTo(map)
                    .bindPopup('Phố Vĩnh Khánh')
                    .openPopup();
            </script>
        </body>
        </html>";

        MyWebView.Source = new HtmlWebViewSource
        {
            Html = html
        };
    }
}