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
				var l = String.Format(nfi, "{0:N9},", runningTimeOffset);
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
			protected double timeOffset;

			string ISampleSerializer.getFileName() { return SAVE_FILENAME; }

			void ISampleSerializer.initialize() {
				if(sw != null)
					sw.Close();
				sw = new StreamWriter(SAVE_FILENAME);
				/*
				$date Sat Mar 27 10:35:40 2021 $end
				$version libsigrok 0.5.2 $end
				$comment
					Acquisition with 5 / 8 channels at 1.56 MHz
				$end
				$timescale 1 ns $end
				$scope module libsigrok $end
				$var wire 1! D0 $end
				$var wire 1 " D1 $end
				$var wire 1 # D2 $end
				$var wire 1 $ D3 $end
				$var wire 1 % D4 $end
				$upscope $end
				$enddefinitions $end
				*/
				sw.WriteLine("D0,D1,D2,D3,D4,D5,D6,D7");
			}

			void ISampleSerializer.prepareForSamples(double samplePeriod, double timeOffset) {
				this.samplePeriod = samplePeriod;
				this.timeOffset = timeOffset;
			}

			void ISampleSerializer.reopen() {
				sw = new StreamWriter(SAVE_FILENAME, true); // append
			}

			static public bool[] getBits(byte sample) {
				bool[] bits = new bool[8];
				for(byte i = 0; i < 8; i++) {
					byte mask = (byte)(0x01 << i);
					bits[i] = (mask & sample) > 0;
				}
				return bits;
			}

			void ISampleSerializer.handleSample(byte sample) {
				var l = "";
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

	}
}
