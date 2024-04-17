using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EpilepsyApp.ViewModel
{
   [QueryProperty("Username", "Username")]
   public partial class MonitoringViewModel: ObservableObject
   {
      [ObservableProperty]
      string username;

      [ICommand]
      async Task Logout()
      {
         await Shell.Current.GoToAsync("..");
      }
   }
}
