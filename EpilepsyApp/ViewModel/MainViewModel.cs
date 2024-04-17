using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        [ICommand]
        async Task Login()
        {
           await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?Username={username}");
        }
        
   }
}
