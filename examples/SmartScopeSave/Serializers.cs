using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MatlabFileIO;

namespace SmartScopeSave {
  public class Serializers {

    public interface ISampleSerializer {
      void initialize();
      void prepareForLogicalSamples(double samplePeriod, double timeOffset, string[] meta);
      void handleLogicalSample(byte sample);
      void prepareForAnalogSamples(double samplePeriod, double timeOffset, string[] meta);
      void handleAnalogSamples(float[] samples);
      void reopen();
      void finalize();
      string getName();
      string getFileName();
      void setFileName(string fileName);
      ulong getNumberOfSavedRecords();
    }

    public class CSVSerializer : ISampleSerializer {
      public string saveFileName = "smartscope.csv";
      protected StreamWriter sw = null;
      protected bool hasTimeColumn = true;
      protected double samplePeriod;
      protected double timeOffset;
      protected double runningTimeOffset;
      protected ulong numberOfSavedRecords;
      NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

      string ISampleSerializer.getName() { return "CSV"; }
      string ISampleSerializer.getFileName() { return saveFileName; }
      void ISampleSerializer.setFileName(string fileName) { saveFileName = fileName; }

      void ISampleSerializer.initialize() {
        if(sw != null)
          sw.Close();
        sw = new StreamWriter(saveFileName);
      }

      void ISampleSerializer.prepareForLogicalSamples(double samplePeriod, double timeOffset, string[] meta) {
        this.samplePeriod = samplePeriod;
        this.timeOffset = timeOffset;
        runningTimeOffset = timeOffset;
        numberOfSavedRecords = 0;

        sw.WriteLine(";  SmartScopeSave:");
        foreach(string ms in meta)
          sw.WriteLine(";    " + ms);
        sw.WriteLine("Time,D0,D1,D2,D3,D4,D5,D6,D7");
      }

      void ISampleSerializer.prepareForAnalogSamples(double samplePeriod, double timeOffset, string[] meta) {
        this.samplePeriod = samplePeriod;
        this.timeOffset = timeOffset;
        runningTimeOffset = timeOffset;
        numberOfSavedRecords = 0;

        sw.WriteLine(";  SmartScopeSave:");
        foreach(string ms in meta)
          sw.WriteLine(";    " + ms);
        sw.WriteLine("Time,A0,A1");
      }

      void ISampleSerializer.handleAnalogSamples(float[] samples) {
        var l = hasTimeColumn ? String.Format(nfi, "{0:N9},", runningTimeOffset) : "";				
        for(byte i = 0; i < samples.Length; i++) {
          if(i > 0) l += ',';
          l += samples[i];
        }
        sw.WriteLine(l);
        numberOfSavedRecords++;
        runningTimeOffset += samplePeriod;
      }

      void ISampleSerializer.reopen() {
        sw = new StreamWriter(saveFileName, true); // append
      }

      void ISampleSerializer.handleLogicalSample(byte sample) {
        var l = hasTimeColumn ? String.Format(nfi, "{0:N9},", runningTimeOffset) : "";				
        for(byte i = 0; i < 8; i++) {
          byte mask = (byte)(0x01 << i);
          l += (mask & sample) > 0 ? '1' : '0';
          if(i < 7) l += ',';
        }
        sw.WriteLine(l);
        numberOfSavedRecords++;
        runningTimeOffset += samplePeriod;
      }

      void ISampleSerializer.finalize() {
        sw.Close();
      }

      ulong ISampleSerializer.getNumberOfSavedRecords() {
        return numberOfSavedRecords;
      }
    }

    public class MATSerializer : ISampleSerializer
    {
      public string saveFileName = "smartscope.mat";
      MatlabFileWriter matFileWriter = null;
      MatLabFileArrayWriter arrayWriter = null;
      protected float samplePeriod;
      protected float timeOffset;
      protected float  runningTimeOffset;
      protected ulong numberOfSavedRecords;

      void ISampleSerializer.initialize()
      {
        if (matFileWriter != null)
          matFileWriter.Close();
        matFileWriter = new MatlabFileWriter(saveFileName);
      }

      string ISampleSerializer.getName() { return "MAT"; }
      string ISampleSerializer.getFileName() { return saveFileName; }
      void ISampleSerializer.setFileName(string fileName) { saveFileName = fileName; }
      void ISampleSerializer.prepareForAnalogSamples(double samplePeriod, double timeOffset, string[] meta)
      {
        this.samplePeriod = (float)samplePeriod;
        this.timeOffset = (float)timeOffset;
        runningTimeOffset = (float)timeOffset;
        numberOfSavedRecords = 0;
        Type t = typeof(float);				
        string name = "analog";
        bool compress = true;
        arrayWriter = matFileWriter.OpenArray(t, name, compress);
      }
      void ISampleSerializer.handleAnalogSamples(float[] samples)
      {
        arrayWriter.AddFloat(runningTimeOffset);
        arrayWriter.AddRow(samples);
        runningTimeOffset += samplePeriod;
        numberOfSavedRecords++;
      }
      void ISampleSerializer.prepareForLogicalSamples(double samplePeriod, double timeOffset, string[] meta) {
        this.samplePeriod = (float)samplePeriod;
        this.timeOffset = (float)timeOffset;
        runningTimeOffset = (float)timeOffset;
        numberOfSavedRecords = 0;
      }
      void ISampleSerializer.handleLogicalSample(byte sample) {
        numberOfSavedRecords++;
        throw new NotImplementedException();
        
      }
      
      void ISampleSerializer.reopen() { 
        // append - new array "analog1"?
      }
      void ISampleSerializer.finalize() {
        arrayWriter.FinishArray();
        matFileWriter.Close();
        matFileWriter = null;
      }
      ulong ISampleSerializer.getNumberOfSavedRecords()
      {
        return numberOfSavedRecords;
      }
    }

    public class VCDSerializer : ISampleSerializer {
      public string saveFileName = "smartscope.vcd";
      protected StreamWriter sw = null;
      protected double samplePeriod;
      protected UInt64 sampleInc;
      protected double timeOffset;
      protected UInt64 runningTimeOffset;
      protected bool firstSampleSeen = false;
      protected ulong numberOfSavedRecords;
      protected byte[] channelValues = new byte[8] {
        0, 0, 0, 0, 0, 0, 0, 0
      };
      public static string[] logicalChannelChars = new string[8] {
        "!", "@", "#", "$", "%", "^", "&", "*"
      };
      protected float[] analogChannelValues = new float[2] {
        0, 0
      };
      public static string[] analogChannelChars = new string[2] {
        "(", ")"
      };
      NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
      string ISampleSerializer.getName() { return "VCD"; }
      string ISampleSerializer.getFileName() { return saveFileName; }
      void ISampleSerializer.setFileName(string fileName) { saveFileName = fileName; }

      void ISampleSerializer.initialize() {
        if(sw != null)
          sw.Close();
        sw = new StreamWriter(saveFileName);
      }

      void ISampleSerializer.prepareForLogicalSamples(double samplePeriod, double timeOffset, string[] meta) {
        this.samplePeriod = samplePeriod;
        sampleInc = (UInt64)(samplePeriod * 1e8); // scale to 10 ns
        this.timeOffset = timeOffset;
        runningTimeOffset = 0; // timeOffset;

        // header
        sw.WriteLine(String.Format("$date {0} $end", DateTime.Now.ToUniversalTime()));
        sw.WriteLine("$comment");
        sw.WriteLine("  SmartScopeSave:");
        foreach(string ms in meta)
          sw.WriteLine("    " + ms);
        sw.WriteLine("$end");
        sw.WriteLine(String.Format("$timescale 10ns $end"));
        sw.WriteLine("$scope module SmartScope $end");
        for(int i = 0; i < logicalChannelChars.Length; i++)
          sw.WriteLine(String.Format("$var wire 1 {0} D{1} $end", logicalChannelChars[i], i));
        sw.WriteLine("$upscope $end");
        sw.WriteLine("$enddefinitions $end");

        numberOfSavedRecords = 0;
      }

      void ISampleSerializer.reopen() {
        sw = new StreamWriter(saveFileName, true); // append
      }

      static public byte[] getBits(byte sample) {
        byte[] bits = new byte[8];
        for(byte i = 0; i < 8; i++) {
          byte mask = (byte)(0x01 << i);
          bits[i] = (byte)((mask & sample) > 0 ? 1 : 0);
        }
        return bits;
      }

      void ISampleSerializer.handleLogicalSample(byte sample) {
        var l = "";
        byte[] sampleValues = getBits(sample);

        if(!firstSampleSeen) {

          // first data line
          l = "#0";
          for(byte i = 0; i < 8; i++) {
            l += " " + sampleValues[i] + logicalChannelChars[i];
            channelValues[i] = sampleValues[i]; // save new value
          }
          runningTimeOffset = 0;
          firstSampleSeen = true;

        } else {

          runningTimeOffset += sampleInc;
          for(byte i = 0; i < 8; i++) {
            if(sampleValues[i] != channelValues[i]) {
              if(l.Length == 0)
                l = "#" + runningTimeOffset;
              l += " " + sampleValues[i] + logicalChannelChars[i];
              channelValues[i] = sampleValues[i]; // save new value
            }
          }

        }

        if(l.Length > 0) {
          sw.WriteLine(l);
          numberOfSavedRecords++;
        }
      }

      void ISampleSerializer.prepareForAnalogSamples(double samplePeriod, double timeOffset, string[] meta) {
        this.samplePeriod = samplePeriod;
        sampleInc = (UInt64)(samplePeriod * 1e8); // scale to 10 ns
        this.timeOffset = timeOffset;
        runningTimeOffset = 0; // timeOffset;

        // header
        sw.WriteLine(String.Format("$date {0} $end", DateTime.Now.ToUniversalTime()));
        sw.WriteLine("$comment");
        sw.WriteLine("  SmartScopeSave:");
        foreach(string ms in meta)
          sw.WriteLine("    " + ms);
        sw.WriteLine("$end");
        sw.WriteLine(String.Format("$timescale 10ns $end"));
        sw.WriteLine("$scope module SmartScope $end");
        for(int i = 0; i < analogChannelChars.Length; i++)
          sw.WriteLine(String.Format("$var real 64 {0} A{1} $end", analogChannelChars[i], i));
        sw.WriteLine("$upscope $end");
        sw.WriteLine("$enddefinitions $end");

        numberOfSavedRecords = 0;
      }

      void ISampleSerializer.handleAnalogSamples(float[] samples) {
        var l = "";

        if(!firstSampleSeen) {

          // first data line
          l = "#0";
          for(byte i = 0; i < samples.Length; i++) {
            l += " r" + String.Format(nfi, "{0:N9}", samples[i]) + " " + analogChannelChars[i];
            analogChannelValues[i] = samples[i]; // save new value
          }
          runningTimeOffset = 0;
          firstSampleSeen = true;

        } else {

          runningTimeOffset += sampleInc;
          for(byte i = 0; i < samples.Length; i++) {
            if(samples[i] != analogChannelValues[i]) {
              if(l.Length == 0)
                l = "#" + runningTimeOffset;
              l += " r" + String.Format(nfi, "{0:N9}", samples[i]) + " " + analogChannelChars[i];
              analogChannelValues[i] = samples[i]; // save new value
            }
          }

        }

        if(l.Length > 0) {
          sw.WriteLine(l);
          numberOfSavedRecords++;
        }
      }

      void ISampleSerializer.finalize() {
        sw.Close();
      }

      ulong ISampleSerializer.getNumberOfSavedRecords() {
        return numberOfSavedRecords;
      }

    }

  }
}
