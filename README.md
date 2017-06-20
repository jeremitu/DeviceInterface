*Documentation needs to get updated since major refactoring of low-level device interfacing*

#DeviceInterface

For a very simple usage example, see our [console-demo](https://github.com/labnation/DeviceInterface/tree/master/examples/SmartScopeConsole)

##Overview
DeviceInterface translates the SmartScope API to hardware I/O. It consists of several layers:

* DeviceManager
  * Device
     * Memories
         * Registers
     * HardwareInterface
         * OS-specific hardware interfacing

## DeviceManager
The device manager class is the starting point to work with the DeviceInterface library. Instantiate it with a `DeviceConnectHandler` callback, called whenever a device connects or disconnects. 

When a device is disconnected, the `DeviceConnectHandler` is called with the DummyScope (the so-called fallback device). This way, you always have a device to work with. To get a fallback device at startup, call `DeviceConnectHandler(DeviceManager.fallbackDevice, true)` *after* `DeviceManager` instantiation and *before* `DeviceManager.Start()`

## Devices
There are three interfaces used to implement the SmartScope

| Interface       | Function |
|:--------------- |:-------------------------------------------------|
| `IDevice`       |implements a general device, could be your fridge |
| `IScope`        |implements an oscillocope/logic analyser          |
| `IWaveGenerator`|implements an analog and/or digital wave generator|

### IDevice
A simple interface to the device's status (ready or not) and its serial number

### IScope
You're going to use this interface the most when working with the SmartScope.

Implements `IDevice`, provides properties and methods to control the oscilloscope and get scope data.

Provides a `DataSource`, a class which fetches and records scope data in its own thread.

### IWaveGenerator
Implements `IDevice`, provides properties and methods to control the wave generator and upload waveforms.

#### IWaveGenerator usage - Generate analog waves
When using the SmartScope for generating a custom analog waveform, follow this procedure:

0) (optional) To be safe: first disable all outputs

```
scope.GeneratorToDigitalEnabled = false;
scope.GeneratorToAnalogEnabled = false;
``` 
 
1) Define and upload custom wave (specified immediately in Voltages, between 0V and 3.3V)

```
double[] customWave = new double[] { 0, 1.5, 1.88, 2, 1.2, 1, 0.5 };
scope.GeneratorDataDouble = customWave;
```

2) Set output frequency (in seconds)

`scope.GeneratorSamplePeriod = 0.000005;`

3) CommitSettings must be called to make the samplePeriod setting effective

`scope.CommitSettings();`

4) Enable analog output

`scope.GeneratorToAnalogEnabled = true;`

#### IWaveGenerator examples - Generate analog waves
* Matlab: https://github.com/labnation/DeviceInterface.Matlab/blob/master/SmartScopeAnalogOutput.m

#### IWaveGenerator usage - Generate digital waves
When using the SmartScope for generating up to 4 simultaneous customized digital signals, follow this procedure:

0) (optional) To be safe: first disable all outputs

```
scope.GeneratorToDigitalEnabled = false;
scope.GeneratorToAnalogEnabled = false;
``` 
 
1) Define and upload custom wave

```
byte[] customWave = new byte[] { 0, 1, 0, 2, 0, 4, 0, 8 };
scope.GeneratorDataByte = customWave;
```

2) Set output frequency (in seconds)

`scope.GeneratorSamplePeriod = 0.000005;`

3) Set output voltage to 3V or 5V

`scope.SetDigitalOutputVoltage(DigitalOutputVoltage.V3_0);`

4) CommitSettings must be called to make the samplePeriod setting effective

`scope.CommitSettings();`

5) Enable digital output

`scope.GeneratorToDigitalEnabled = true;`

#### IWaveGenerator examples - Generate digital waves
* Matlab: https://github.com/labnation/DeviceInterface.Matlab/blob/master/SmartScopeDigitalOutput.m

## Memories and registers
To use DeviceInterface, you don't actually need to understand these internals, so this is just a rough sketch of the functionality.

All 'smart' chips (PIC, FPGA, ADC) inside the SmartScope have their registers, typically bytes. These are the 'parameters' of certain functionalities of these chips. Examples:

* FGPA: the TRIGGER_LEVEL register defines the voltage of the analog trigger level (FPGA register 7, see [the FPGA register list](https://github.com/labnation/DeviceInterface/blob/master/src/Hardware/ScopeConstants_GEN.cs))
* ADC: bits 5 and 4 of register 6 in the ADC defines wheth er its data is in two's complement or simply offset  binary (see the MAX19506 datasheet p22)

Therefore, some logic is required to convert from physical values to register values (eg GUI trigger slider to TRIGGER_LEVEL bytevalue). These conversions are implemented in DeviceInterface (see the [TriggerValue setter method in SmartScopeSettings.cs](https://github.com/labnation/DeviceInterface/blob/master/src/Devices/SmartScopeSettings.cs)).

`MemoryRegister` represents a register inside of a device. For efficiency's sake, they are cached, meaning the change of a register is not immediately written through to the device. Instead, you can change a bunch of registers and finally call `Commit()` on the containing  `DeviceMemory` object to effectuate the register changes. To circumvent this chaching mechanism, use `MemoryRegister.WriteImmediate()`.

The same goes for `MemoryRegister.Get()`: the cached value is returned. To first update the cache, use `MemoryRegiter.Read().Get()`.

* `DeviceMemory`, containing as set of `MemoryRegister`, is the abstract class used to implement all devices inside of the SmartScope.
* `ByteMemory` inherits from `DeviceMemory`, providing an indexer `[]` returning `ByteRegister`.
* Most memories are of the type `ByteMemory`

## Hardware Interfaces
There's no need to understand this part to use the library, so here's just a concise explanation of the mechanisms.

`InterfaceManager` is a singleton instance instantiated by `DeviceManager`. Its function is to monitor the presence of a SmartScope USB interface `ISmartScopeUsbInterface`. It's implementation is OS-specific, hence 3 different implementations of both `InterfaceManager` and `ISmartScopeUsbInterface`.

When `InterfaceManager` detects a change in the presence of a `ISmartScopeUsbInterface` (a smartscope is (un)plugged into the USB port), it notifies `DeviceManager` which then decides to create or destroy a `SmartScope` instance and pass it further to your application.

`SmartScope` is instantiated with an `ISmartScopeUsbInterface` in its constructor, used for all hardware interfacing.

## IScope Usage
1. Create a `DeviceConnectHandler(IDevice dev, bool connected)` callback
2. Instantiate a `DeviceManager` with a `DeviceConnectHandler` callback
3. `Start()` the `DeviceManager`
4. In your `DeviceConnectHandler`
 1. Check if the device got connected or disconnected
 2. Check if the `IDevice` is an `IScope`, if so, cast it
 3. Choose how to fetch data
     1. Using the aynchronous `DataSource` 
     2. Using the synchronous `IScope.GetScopeData`
 4. Configure the `IScope`
     1. Set voltage and time range, sample period, viewport,...
     2. Set acquisition mode
     3. Set trigger
     4. Call `IScope.CommitSettings()`
     5. Set `IScope.running = true`
 5. If not using `DataSource`, start your loop which calls `IScope.GetScopeData()`

Whichever method you choose, you will end up with `DataPackageScope` objects coming out of the `IScope` or `DataSource`. Below is a detailed explanation of what it contains and how to use it.
  
## DataSource
`DataSource` is a class beloning to an `IScope` which periodically calls `IScope.GetScopeData()` and calls all `NewDataAvailableHandler` callbacks registered to it, so you don't have to write your own data fetch loop.

### Usage
1. Add a `NewDataAvailableHandler` callback to `IScope.DataSourceScope.OnNewDataAvailable`
2. Call `IScope.DataSourceScope.Start()`
3. Configure your `IScope` and watch data coming in

## DataPackageScope
This is the object returned by the `IScope`, containing **all** data you need to further process the scope measurement. Indeed, you can have changed `IScope` settings after an acquisition was started, therefore, when displaying or processing data, don't read back `IScope` properties but use the ones from the `DataPackageScope`.

There are a few global acquisition parameters (see source for inline documentation)

  * Acquisition ID
  * Timestamp
  * trigger holdoff
  * acquisition length
  * viewport settings
  * ...

Then there is also a set of per-channel data, contained in `ChannelData` objects, containing

  * The type of data: viewport, acquisition or overview (see table below)
  * The channel (i.e. AnalogChannel.ChB, DigitalChannel.Digi3,...)
  * The array of data
  * Whether it is partial
  * The sample period and time-offset to the beginning of the acquisition (for viewport data)

### DataSourceType

| DataSourceType | Meaning                                           |
|----------------|-------------------------------------------------- |
| Viewport       | an up to 2kSa representation of the viewport data |
| Acquisition    | the full acquisition, up to 4MSa                  |
| Overview       | a 2kSa representation of the entire acquisition   |
  
Once a DataPackageScope object is obtained, you can retrieve the data by calling `DataPackageScope.GetData()`, providing the `DataSourceType` you're interested in and the channel. If the data is not present, `NULL` will be returned.

**NOTE:** As long as `DataPackageScope.FullAcquisitionProgress` is below 1, `DataPackageScope.GetData` will a `ChannelData` object with an incomplete array for `DataSourceType.Acquisition`.

**NOTE:** If you want to process and change the data array of a ChannelData object, be aware that `IScope` might still update (and append) to that array in the following cases:

| `DataSourceType` | Scenario   |
|------------------|----------- |
| `DataSourceType.Viewport` | `IScope.Partial` is `true` and the entire viewport was not entirely fetched |
| `DataSourceType.Viewport` or `DataSourceType.Overview` | `IScope.Rolling` is `true` and the acquisition is updated as the acquisition buffer rolls by |
| `DataSourceType.Acquisition` | The full acquisition buffer is still being read to the host, but a partial buffer is already returned by `DeviceInterface`. Monitor `DataPackageScope.FullAcquisitionProgress` to know if the ChannelData is complete |

In general, it's safest to just copy the array contents instead of changing it.
