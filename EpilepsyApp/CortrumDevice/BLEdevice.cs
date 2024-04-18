using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpilepsyApp.CortrumDevice
{
   public class BLEdevice : ObservableObject// This class is used to store the UUIDs of the BLE devices that are found during the scan.
   {
      public Guid Id { get; internal set; } // Guid  f.x "123e4567-e89b-12d3-a456-426655440000".
      public string Name { get; internal set; }

   }
}
