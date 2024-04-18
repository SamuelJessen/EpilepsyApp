using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpilepsyApp.Models;

namespace EpilepsyApp.Events
{
   public class ECGDataReceivedEventArgs : EventArgs
   {
      public ECGBatchData ECGBatch { get; set; }
   }
}
