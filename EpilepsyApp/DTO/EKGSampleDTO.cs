using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpilepsyApp.DTO
{
   //make this class an observable by inheriting from the INotifyPropertyChanged interface
   public class EKGSampleDTO
   {
      public string PatientId { get; set; }
      public DateTimeOffset Timestamp { get; set; }
      public sbyte[] RawBytes { get; set; }



   }
}
