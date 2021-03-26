﻿using System;
using LabNation.DeviceInterface.Devices;
using LabNation.Common;
using LabNation.DeviceInterface.DataSources;
using System.Threading;
using System.IO;
using System.Linq;
#if WINDOWS
using System.Windows.Forms;
#endif

//
// Copied SmartScopeConsole and modified it so it can save data to be imported in Sigrok.
// For now it collects Digital signals using the B-channel and saves them into a CSV file.
// The file is called "smartscope.csv".
// Trigger is set to L on D0 mode Single.
//

namespace SmartScopeSave
{
	class MainClass {
		/// <summary>
		/// The DeviceManager detects device connections
		/// </summary>
		static DeviceManager deviceManager;
		/// <summary>
		/// The scope used in here
		/// </summary>
		static IScope scope;
		static bool running = true;

		// 4M 2M 1M 512K 256K 128K
		static uint[] AcqDepthData = new uint[] {
			4 * 1024 * 1024, 2 * 1024 * 1024, 1 * 1024 * 1024,
			512 * 1024, 256 * 1024, 128 * 1024 };
		static string[] AcqDepthDataStr = new string[] {
			"4M", "2M", "1M",	"512K", "256K", "128K" };
		static int acqDepthIndex = 2;
		//enum ACQ_DEPTH { AD_4M, AD_2M, AD_1M, AD_512K, AD_256K, AD_128K };
		//static ACQ_DEPTH acqDepth = ACQ_DEPTH.AD_1M;

		static void updateAcqDepthValue() {
			scope.AcquisitionDepth = AcqDepthData[acqDepthIndex];
			scope.CommitSettings();
			Console.Write(String.Format("new Acquisition Depth: {0}({1})\n", AcqDepthDataStr[acqDepthIndex], scope.AcquisitionDepth));
		}

		/*static void updateAcqDepthValue(ACQ_DEPTH acqDepth) {
			uint ad = 1024;
			switch(acqDepth) {
				case ACQ_DEPTH.AD_4M:
					ad *= 4 * 1024; 
					break;
				case ACQ_DEPTH.AD_2M:
					ad *= 2 * 1024;
					break;
				case ACQ_DEPTH.AD_1M:
					ad *= 1 * 1024;
					break;
				case ACQ_DEPTH.AD_512K:
					ad *= 512;
					break;
				case ACQ_DEPTH.AD_256K:
					ad *= 256;
					break;
				case ACQ_DEPTH.AD_128K:
					ad *= 128;
					break;
			}
			scope.AcquisitionDepth = ad;
			scope.CommitSettings();
			Console.Write(String.Format("new Acquisition Depth: {0}\n", scope.AcquisitionDepth));
		}

		static ACQ_DEPTH prevAcqDepth(ACQ_DEPTH acqDepth) {
			switch(acqDepth) {
				case ACQ_DEPTH.AD_4M: return ACQ_DEPTH.AD_2M;
				case ACQ_DEPTH.AD_2M: return ACQ_DEPTH.AD_1M;
				case ACQ_DEPTH.AD_1M: return ACQ_DEPTH.AD_512K;
				case ACQ_DEPTH.AD_512K: return ACQ_DEPTH.AD_256K;
				case ACQ_DEPTH.AD_256K: return ACQ_DEPTH.AD_128K;
				default: return ACQ_DEPTH.AD_4M;
			}
		}

		static ACQ_DEPTH nextAcqDepth(ACQ_DEPTH acqDepth) {
			switch(acqDepth) {
				case ACQ_DEPTH.AD_4M: return ACQ_DEPTH.AD_128K;
				case ACQ_DEPTH.AD_2M: return ACQ_DEPTH.AD_4M;
				case ACQ_DEPTH.AD_1M: return ACQ_DEPTH.AD_2M;
				case ACQ_DEPTH.AD_512K: return ACQ_DEPTH.AD_1M;
				case ACQ_DEPTH.AD_256K: return ACQ_DEPTH.AD_512K;
				default: return ACQ_DEPTH.AD_256K;
			}
		}*/

		[STAThread]
		static void Main (string[] args)
		{
			//Open logger on console window
			FileLogger consoleLog = new FileLogger (new StreamWriter (Console.OpenStandardOutput ()), LogLevel.INFO);

			Logger.Info ("LabNation SmartScope Save");
			Logger.Info ("---------------------------------");

			//Set up device manager with a device connection handler (see below)
			deviceManager = new DeviceManager (connectHandler);
			deviceManager.Start ();

			ConsoleKeyInfo cki = new ConsoleKeyInfo();
			while (running) {
#if WINDOWS
                Application.DoEvents();
#endif
				Thread.Sleep (100);

				if (Console.KeyAvailable) {
					cki = Console.ReadKey (true);
					HandleKey (cki);
				}
			}
            Logger.Info("Stopping device manager");
			deviceManager.Stop ();
            Logger.Info("Stopping Logger");
			consoleLog.Stop ();
		}

		static void connectHandler (IDevice dev, bool connected)
		{
			//Only accept devices of the IScope type (i.e. not IWaveGenerator)
			//and block out the fallback device (dummy scope)
			if (connected && dev is IScope && !(dev is DummyScope)) {
				Logger.Info ("Device connected of type " + dev.GetType ().Name + " with serial " + dev.Serial);
				scope = (IScope)dev;
				ConfigureScope ();
			} else {
				scope = null;
			}
		}

		static void HandleKey(ConsoleKeyInfo k)
		{
			switch (k.Key) {
				case ConsoleKey.Q:
				case ConsoleKey.X:
        case ConsoleKey.Escape:
					// quit
					running = false;
					break;
				case ConsoleKey.R:
					// replace sample
					initFile();
					scope.Running = true;
					scope.CommitSettings();
					break;
				case ConsoleKey.A:
					// add sample
					scope.Running = true;
					scope.CommitSettings();
					break;
				default:
					switch(k.KeyChar) {
						case '[':
							acqDepthIndex--;
							if(acqDepthIndex < 0)
								acqDepthIndex = AcqDepthData.Length - 1;
							updateAcqDepthValue();
							//acqDepth = prevAcqDepth(acqDepth);
							//updateAcqDepthValue(acqDepth);
							break;
						case ']':
							acqDepthIndex++;
							if(acqDepthIndex >= AcqDepthData.Length)
								acqDepthIndex =  0;
							updateAcqDepthValue();
							//acqDepth = nextAcqDepth(acqDepth);
							//updateAcqDepthValue(acqDepth);
							break;
					}
					break;
				//case ConsoleKey.:
				//	break;
			}
		}

		static void initFile() {
			using(var sw = new StreamWriter(SAVE_FILENAME)) {
				sw.WriteLine("D0,D1,D2,D3,D4,D5,D6,D7");
			}
		}

		static void ConfigureScope ()
		{
			Logger.Info ("Configuring scope");

			//Stop the scope acquisition (commit setting to device)
			scope.Running = false;
			scope.CommitSettings ();

			//Set handler for new incoming data
			scope.DataSourceScope.OnNewDataAvailable += CollectData;
			//Start datasource
			scope.DataSourceScope.Start ();

			//Configure acquisition

			/******************************/
			/* Horizontal / time settings */
			/******************************/
			//Disable logic analyser
			scope.ChannelSacrificedForLogicAnalyser = AnalogChannel.ChB;
			//Don't use rolling mode
			scope.Rolling = false;
			//Don't fetch overview buffer for faster transfers
			scope.SendOverviewBuffer = false;
			//trigger holdoff in seconds
			scope.TriggerHoldOff = 0; 
			//Acquisition mode to automatic so we get data even when there's no trigger
			scope.AcquisitionMode = AcquisitionMode.SINGLE; 
			//Don't accept partial packages
			scope.PreferPartial = false;
			//Set viewport to match acquisition
			scope.SetViewPort (0, scope.AcquisitionLength);

			//Set sample depth to the minimum for a max datarate
			//scope.AcquisitionLength = scope.AcquisitionLengthMin; 
			scope.AcquisitionDepthUserMaximum = 4 * 1024 * 1024;
			scope.AcquisitionDepth = AcqDepthData[acqDepthIndex];
			scope.AcquisitionLength = 0.2;

			/*******************************/
			/* Vertical / voltage settings */
			/*******************************/
			/*foreach (AnalogChannel ch in AnalogChannel.List) {
				//FIRST set vertical range
				scope.SetVerticalRange (ch, -3, 3);
				//THEN set vertical offset (dicated by range)
				scope.SetYOffset (ch, 0);
				//use DC coupling
				scope.SetCoupling (ch, Coupling.DC);
				//and x10 probes
				ch.SetProbe(Probe.DefaultX10Probe);
			}*/

			// Set trigger to channel A
			/*scope.TriggerValue = new TriggerValue () {
				channel = AnalogChannel.ChA,
				edge = TriggerEdge.RISING,
				level = 1.0f
			};*/

			// Digital trigger 
			System.Collections.Generic.Dictionary<DigitalChannel, DigitalTriggerValue> digitalTriggers = scope.TriggerValue.Digital;
			digitalTriggers[DigitalChannel.Digi1] = DigitalTriggerValue.L;
			scope.TriggerValue = new TriggerValue() {
				mode = TriggerMode.Digital,
				source = TriggerSource.Channel,
				channel = null,
				Digital = digitalTriggers
			};

			//Update the scope with the current settings
			scope.CommitSettings ();

			//Show user what he did
			PrintScopeConfiguration ();

			initFile();

			//Set scope runnign;
			scope.Running = true;
			scope.CommitSettings ();
		}

		static string SAVE_FILENAME = "smartscope.csv";

		/// <summary>
		/// Print 
		/// </summary>
		static void CollectData(DataPackageScope p, DataSource s) {
			int triesLeft = 20;
			while(triesLeft >= 0) {
				DataPackageScope dps = scope.GetScopeData();

				if(dps == null) {
					triesLeft--;
					continue;
				}

				if(dps.FullAcquisitionFetchProgress < 1f) {
					continue;
				}

				if(dps.GetData(ChannelDataSourceScope.Acquisition, AnalogChannel.ChB.Raw()) != null) {
					ChannelData cd = dps.GetData(ChannelDataSourceScope.Acquisition, AnalogChannelRaw.List[1]); // [1] for channel B
					byte[] ba = (byte[])cd.array;
					using(StreamWriter sw = File.AppendText(SAVE_FILENAME)) {
						foreach(byte b in ba) {
							var l = "";
							for(byte i = 0; i < 8; i++) {
								byte mask = (byte)(0x01 << i);
								l += (mask & b) > 0 ? '1' : '0';
								if(i < 7) l += ',';
							}
							sw.WriteLine(l);
						}
					}
					Console.Write(String.Format("Saved {0} records into: \"{1}\"\n", ba.Length, SAVE_FILENAME));
					Console.Write(String.Format("  Acq. Depth: {0}, Acq. Length\n", scope.AcquisitionDepth, scope.AcquisitionLength));
					Console.Write("'R':replace, 'A':add, '[]':prev/next AcqDepth, 'Q|X|Esc' to Quit\n");
					return;
				}
			}
			Console.Write("Timeout while waiting for scope data.\n");
			running = false;
		}

		static void PrintScopeConfiguration ()
		{
			string c = "";
			string f = "{0,-20}: {1:s}\n";
			string fCh = "Channel {0:s} - {1,-15:s}: {2:s}\n";
			c += "---------------------------------------------\n";
			c += "-              SCOPE SETTINGS               -\n";
			c += "---------------------------------------------\n";
			c += String.Format (f, "Scope serial", scope.Serial);
			c += String.Format (f, "Acquisition depth", Utils.siPrint (scope.AcquisitionDepth, 1, 3, "Sa", 1024));
			c += String.Format (f, "Acquisition length", Utils.siPrint (scope.AcquisitionLength, 1e-9, 3, "s"));
			c += String.Format (f, "Sample rate", Utils.siPrint (1.0 / scope.SamplePeriod, 1, 3, "Hz"));
			c += String.Format (f, "Viewport offset", Utils.siPrint (scope.ViewPortOffset, 1e-9, 3, "s"));
			c += String.Format (f, "Viewport timespan", Utils.siPrint (scope.ViewPortTimeSpan, 1e-9, 3, "s"));	
			c += String.Format (f, "Trigger holdoff", Utils.siPrint (scope.TriggerHoldOff, 1e-9, 3, "s"));
			c += String.Format (f, "Acquisition mode", scope.AcquisitionMode.ToString ("G"));
			c += String.Format (f, "Rolling", scope.Rolling.YesNo ());
			c += String.Format (f, "Partial", scope.PreferPartial.YesNo ());
			c += String.Format (f, "Logic Analyser", scope.LogicAnalyserEnabled.YesNo ());


			/*foreach (AnalogChannel ch in AnalogChannel.List) {
				string chName = ch.Name;
				c += String.Format ("======= Channel {0:s} =======\n", chName);
				c += String.Format (fCh, chName, "Vertical offset", printVolt (scope.GetYOffset (ch)));
				c += String.Format (fCh, chName, "Coupling", scope.GetCoupling (ch).ToString ("G"));
				c += String.Format (fCh, chName, "Probe division", ch.Probe.ToString ());
			}*/
			c += "---------------------------------------------\n";
			Console.Write (c);				
		}
	}
}