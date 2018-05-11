using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace Gps
{
    public partial class MainPage : ContentPage
    {
        Pin pin;
        CustomMap customMap;
        Entry czas = new Entry();
        Label timerlab = new Label();
        Label rekordlab = new Label();
        Button startnav = new Button();
        Button endnav = new Button();
        double suma = 0;
        int j = 0;//do sterowania czy nawgiacja działa        
        int i = 0;
        List<double> lat = new List<double>();
        List<double> lon = new List<double>();
        Timer timer;        
        int min = 0, sec = 0, millisec = 1;
        string rekord;

        public MainPage()
        {
            Load();
            customMap = new CustomMap
            {
                MapType = MapType.Street,
                IsShowingUser = true,
                WidthRequest = App.ScreenWidth,
                HeightRequest = App.ScreenHeight
            };
            Content = customMap;
            startnav = new Button { Text = "Start" };
            startnav.Clicked += Button_start_nav;
            endnav = new Button { Text = "Koniec", IsEnabled=false };
            endnav.Clicked += Button_end_nav;
            Label czaslab = new Label { Text = "Odświeżanie w sekundach:" };            
            czas = new Entry { Text = "5", Keyboard = Keyboard.Numeric };
            czas.TextChanged += OnTextChanged;
            var buttons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = {
                    startnav,endnav,czaslab,czas,rekordlab
                }
            };
            Content = new StackLayout
            {
                Spacing = 0,
                Children = {
                    customMap,
                    buttons
                }
            };

            customMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(50.2445, 18.8318), Xamarin.Forms.Maps.Distance.FromKilometers(50)));
        }

        private async void Button_start_nav(object sender, EventArgs e)
        {
            startnav.IsEnabled = false;
            endnav.IsEnabled = true;
            timer = new Timer();
            timer.Interval = 1; // 1 milliseconds  
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            min = 0;
            sec = 0;
            millisec = 0;
            lat.Clear();
            lon.Clear();
            customMap.RouteCoordinates.Clear();
            customMap.Pins.Clear();
            j = 0;
            i = 0;
            for (; j < 1;)
            {
                await Position();
            }
        }

        private async void Button_end_nav(object sender, EventArgs e)
        {
            startnav.IsEnabled = true;
            var fileService = DependencyService.Get<Saveloadinter>();
            double distance = 0;
            string range = "";
            suma = 0;
            j = 1;            
            timer.Stop();
            string lats = "";
            string lons = "";
            for (int i=0;i<lat.Count;i++)
            {
                lats += lat[i] + " ";
                lons += lon[i] + " ";
            }
            await fileService.SaveTextAsync("lat", lats);
            await fileService.SaveTextAsync("lon", lons);

            for (int i = 0; i < lat.Count - 1; i++)
            {
                distance = Distance(lat[i], lon[i], lat[i + 1], lon[i + 1]);
                distance = Math.Round(distance, 2);

                suma += distance;
                if (distance < 1)
                {
                    range += "Odległość między punktem " + i + " a " + (i + 1) + ": " + distance * 1000 + " m\n";
                }
                else range += "Odległość między punktem " + i + " a " + (i + 1) + ": " + distance + " km\n";
            }            
            if (suma > double.Parse(rekord))
            {
               await fileService.SaveTextAsync("Najdłuższa_trasa", suma.ToString());
               rekord = await fileService.LoadTextAsync("Najdłuższa_trasa");
               rekordlab.Text = "Nadjłuższa trasa: " + rekord + " km";
            }
            if (suma < 1)
            {
                suma *= 1000;
                await DisplayAlert("Podsumowanie trasy", "Przebyto: " + suma.ToString() + " m\n" + "Ogólny czas podróży: " + min + ":" + sec + " min\n" + range + "Najdłuższa przebyta odległość: " + rekord + " km", "Zakończ");
            }
            else await DisplayAlert("Podsumowanie trasy", "Przebyto: " + suma.ToString() + " km\n" + "Ogólny czas podróży: " + min + ":" + sec + " min\n" + range + "Najdłuższa przebyta odległość: " + rekord + " km", "Zakończ");
        }

        private async Task Position()
        {
            try
            {
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 20;
                var position = await locator.GetPositionAsync(TimeSpan.FromSeconds(2), null, true);
                customMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(position.Latitude, position.Longitude), Xamarin.Forms.Maps.Distance.FromKilometers(1)));
                pin = new Pin()
                {
                    Position = new Position(position.Latitude, position.Longitude),
                    Label = "Punkt kontrolny #" + i
                };
                customMap.Pins.Add(pin);
                lat.Add(position.Latitude);
                lon.Add(position.Longitude);
                i++;

                await Task.Delay(int.Parse(czas.Text) * 1000);
            }
            catch(Exception ex)
            {
                await DisplayAlert("Nie można określić położenia", "Włącz lokalizacje gps", "Zamknij");
            }
        }

        private double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(Deg2rad(lat1)) * Math.Sin(Deg2rad(lat2)) + Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) * Math.Cos(Deg2rad(theta));
            dist = Math.Acos(dist);
            dist = Rad2deg(dist);
            dist = dist * 60 * 1.1515;
            dist = dist * 1.609344;
            return (dist);
        }

        private double Deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private double Rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Regex.IsMatch(e.NewTextValue, "^[0-9]+$", RegexOptions.CultureInvariant))
                (sender as Entry).Text = Regex.Replace(e.NewTextValue, "[^0-9]", string.Empty);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            millisec++;
            if(millisec>=1000)
            {
                sec++;
                millisec = 0;
            }
            if(sec==59)
            {
                min++;
                sec = 0;
            }
        }
        
        async void Load()
        {
            var fileService = DependencyService.Get<Saveloadinter>();
            if (fileService.FileExists("Najdłuższa_trasa") == false)
            {
                await fileService.SaveTextAsync("Najdłuższa_trasa", "0");
            }
            if (fileService.FileExists("lat") == false && fileService.FileExists("lon") == false)
            {
                await fileService.SaveTextAsync("lat", "");
                await fileService.SaveTextAsync("lon", "");
            }
            if (fileService.FileExists("lat") == true && fileService.FileExists("lon") == true)
            {
                string lats = await fileService.LoadTextAsync("lat");
                string[] slat = lats.Split(' ');
                double[] lat = new double[slat.Length - 1];
                string lons = await fileService.LoadTextAsync("lon");
                string[] slon = lons.Split(' ');
                double[] lon = new double[slon.Length - 1];
                for (int i = 0; i < slat.Length - 1; i++)
                {
                    lat[i] = double.Parse(slat[i]);
                    lon[i] = double.Parse(slon[i]);
                }
                for (int i = 0; i < lat.Length; i++)
                {
                    customMap.RouteCoordinates.Add(new Position(lat[i], lon[i]));
                }
            }
            rekord = await fileService.LoadTextAsync("Najdłuższa_trasa");
            rekordlab.Text = "Nadjłuższa trasa: " + rekord + " km";
        }
    }
}