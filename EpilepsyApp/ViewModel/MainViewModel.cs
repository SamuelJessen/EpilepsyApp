using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EpilepsyApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mime;

namespace EpilepsyApp.ViewModel
{
   public partial class MainViewModel: ObservableObject
   {
        [ObservableProperty]
        string username;

        [ObservableProperty] 
        string password;

        public BLEservice BLEservice { get; set; }
        private readonly IDecoder decoder;

        public MainViewModel(BLEservice ble, IDecoder decoder)
        {
            BLEservice = ble;
            this.decoder = decoder;
            
        }

        [ICommand]
        async Task Login()
        {
            await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?Username={username}");
        }
        
   }
}
