using System.Diagnostics;
using EpilepsyApp.DTO;
using EpilepsyApp.Models;

namespace EpilepsyApp.Services
{
	public interface IRawDataService
	{
		public ECGBatchSeriesData ProcessData(EKGSampleDTO bytes, bool started);
	}

	public class RawDataService : IRawDataService
	{

		private readonly IMQTTService mqttService;

		// Buffering
		private int nBufferSamples = 252 * 60 * 3; //45360 samples
		private ECGBatchSeriesData bufferedECG = new ECGBatchSeriesData()
		{
			EcgRawBytes = new List<sbyte[]>(),
			TimeStamp = new DateTime(),
		};
		private bool newBufferReady = false;
		private int check = 0;

		public RawDataService(IMQTTService MQTTManager)
		//Todo: Db interaktion
		//public RawDataService(IPythonMQTTService MQTTManager, UserList userList)
		{
			mqttService = MQTTManager;
		}

		public ECGBatchSeriesData ProcessData(EKGSampleDTO eKGSample, bool started)
		{
			//Save raw data in database
			//Todo: Db interaktion POST RAW
			//await rawService.postRaw(eKGSample);

			if (started == true)
			{ // Decode bytes

				try
				{
					var t1 = DateTime.Now.ToUniversalTime();

					//Save decoded data in database
					//Todo: Db interaktion postDecoded
					//await decodedService.postDecoded(ecgdata);

					// Buffer data for 3 minutes

					BufferData(eKGSample, bufferedECG);

					if (bufferedECG.Samples >= nBufferSamples && check == 0)
					{
						ECGBatchSeriesData dataDTO = new ECGBatchSeriesData()
						{
							EcgRawBytes = bufferedECG.EcgRawBytes,
							TimeStamp = bufferedECG.TimeStamp,
							PatientID = bufferedECG.PatientID,
						};

						var t2 = DateTime.Now.ToUniversalTime();
						var timedifferent = t2 - t1;
						Debug.WriteLine(timedifferent);
						check = 1;
						return dataDTO;
					}

					if (newBufferReady == true && check == 1)
					{
						ECGBatchSeriesData dataDTO = new ECGBatchSeriesData()
						{
							EcgRawBytes = bufferedECG.EcgRawBytes,
							TimeStamp = bufferedECG.TimeStamp,
							PatientID = bufferedECG.PatientID,
						};

						var t2 = DateTime.Now.ToUniversalTime();
						var timedifferent = t2 - t1;
						Debug.WriteLine(timedifferent);
						return dataDTO;
					}
				}
				catch (Exception)
				{
					Debug.WriteLine("ProcessData(EKGSampleDTO eKGSample)");
					return null;
				}
			}

			return null;
		}

		private void BufferData(EKGSampleDTO ecgData, ECGBatchSeriesData buffer)
		{
			int nCurrentSamples = bufferedECG.Samples;
			//TODO: dynamic length of data
			// Add new batch to buffer
			buffer.EcgRawBytes.Add(ecgData.RawBytes);
			var time = ecgData.TimeStamp.Millisecond;
			//buffer.TimeStamp.Add(time);
			buffer.Samples += 12;
			buffer.PatientID = ecgData.PatientId;
			newBufferReady = false;

			//When buffer is full, remove first batch every 5 seconds
			if (nCurrentSamples >= nBufferSamples + 252 * 5)
			{
				// Remove first batch from buffer (FIFO)
				//buffer.ECGChannel1.RemoveRange(0, 21);
				//buffer.ECGChannel2.RemoveRange(0, 21);
				//buffer.ECGChannel3.RemoveRange(0, 21);
				//buffer.TimeStamp.RemoveRange(0, 21);
				buffer.EcgRawBytes.RemoveRange(0, 21 * 5);
				buffer.Samples -= 12 * 21 * 5;
				newBufferReady = true;
			}
		}
	}
}
