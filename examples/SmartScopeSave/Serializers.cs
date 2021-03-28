using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartScopeSave {
  public class Serializers {

		public interface ISampleSerializer {
			void initialize();
			void prepareForSamples(double samplePeriod, double timeOffset);
			void handleSample(byte sample);
			void reopen();
			void finalize();
			string getFileName();
		}

		public class CSVSerializer : ISampleSerializer {
			static public string SAVE_FILENAME = "smartscope.csv";
			protected StreamWriter sw = null;
			protected bool hasTimeColumn = true;
			protected double samplePeriod;
			protected double timeOffset;
			protected double runningTimeOffset;
			NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

			string ISampleSerializer.getFileName() { return SAVE_FILENAME; }

			void ISampleSerializer.initialize() {
				if(sw != null)
					sw.Close();
				sw = new StreamWriter(SAVE_FILENAME);
				sw.WriteLine("Time,D0,D1,D2,D3,D4,D5,D6,D7");
			}

			void ISampleSerializer.prepareForSamples(double samplePeriod, double timeOffset) {
				this.samplePeriod = samplePeriod;
				this.timeOffset = timeOffset;
				runningTimeOffset = timeOffset;
			}

			void ISampleSerializer.reopen() {
				sw = new StreamWriter(SAVE_FILENAME, true); // append
			}

			void ISampleSerializer.handleSample(byte sample) {
				var l = hasTimeColumn ? String.Format(nfi, "{0:N9},", runningTimeOffset) : "";
				runningTimeOffset += samplePeriod;
				for(byte i = 0; i < 8; i++) {
					byte mask = (byte)(0x01 << i);
					l += (mask & sample) > 0 ? '1' : '0';
					if(i < 7) l += ',';
				}
				sw.WriteLine(l);
			}

			void ISampleSerializer.finalize() {
				sw.Close();
			}
		}

		public class VCDSerializer : ISampleSerializer {
			static public string SAVE_FILENAME = "smartscope.vcd";
			protected StreamWriter sw = null;
			protected double samplePeriod;
			protected UInt64 sampleInc;
			protected double timeOffset;
			protected UInt64 runningTimeOffset;
			protected byte[] channelValues = new byte[8] {
				0, 0, 0, 0, 0, 0, 0, 0
			};
			public static string[] channelChars = new string[8] {
				"!", "@", "#", "$", "%", "^", "&", "*"
			};

			string ISampleSerializer.getFileName() { return SAVE_FILENAME; }

			void ISampleSerializer.initialize() {
				if(sw != null)
					sw.Close();
				sw = new StreamWriter(SAVE_FILENAME);
			}

			void ISampleSerializer.prepareForSamples(double samplePeriod, double timeOffset) {
				this.samplePeriod = samplePeriod;
				sampleInc = (UInt64)(samplePeriod * 1e8); // scale to 10 ns
				this.timeOffset = timeOffset;
				runningTimeOffset = 0; // timeOffset;

				// header
				sw.WriteLine(String.Format("$date {0} $end", DateTime.Now.ToUniversalTime()));
				sw.WriteLine("$comment");
				sw.WriteLine("  SmartScope samples. Period: {0}$", samplePeriod);
				sw.WriteLine("$end");
				sw.WriteLine(String.Format("$timescale 10ns $end"));
				sw.WriteLine("$scope module SmartScope $end");
				for(int i = 0; i < channelChars.Length; i++)
					sw.WriteLine(String.Format("$var wire 1 {0} D{1} $end", channelChars[i], i));
				sw.WriteLine("$upscope $end");
				sw.WriteLine("$enddefinitions $end");

				// first data line
				string firstLine = "#0 0"+ channelChars[0];
				for(int i = 1; i < channelChars.Length; i++)
					firstLine += " 0" + channelChars[i];
				sw.WriteLine(firstLine);

			}

			void ISampleSerializer.reopen() {
				sw = new StreamWriter(SAVE_FILENAME, true); // append
			}

			static public byte[] getBits(byte sample) {
				byte[] bits = new byte[8];
				for(byte i = 0; i < 8; i++) {
					byte mask = (byte)(0x01 << i);
					bits[i] = (byte)((mask & sample) > 0 ? 1 : 0);
				}
				return bits;
			}

			void ISampleSerializer.handleSample(byte sample) {
				runningTimeOffset += sampleInc;
				byte[] sampleValues = getBits(sample);
				var l = "";
				for(byte i = 0; i < 8; i++) {
					if(sampleValues[i] != channelValues[i]) {
						if(l.Length == 0)
							l = "#" + runningTimeOffset;
						l += " " + sampleValues[i] + channelChars[i];
						channelValues[i] = sampleValues[i]; // save new value
					}
				}
				if(l.Length > 0)
				  sw.WriteLine(l);
			}

			void ISampleSerializer.finalize() {
				sw.Close();
			}
		}

	}
}
