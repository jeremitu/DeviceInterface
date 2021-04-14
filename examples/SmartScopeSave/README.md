# SmartScopeSave

A modified copy of SmartScopeConsole, an example program for the [SmartScope](https://www.lab-nation.com), that can save data. For example to be imported in [Sigrok](https://sigrok.org).

Two file formats are supported: Value Change Dump (VCD) and Comma Separated Values (CSV).

Data can be collected in two modes:

  * Analog, collect analog signals from both the analog channels.
  * Digital, collect digital signals from the eight digital inputs.


Command line options:

```
Usage:
  --analog                : perform acquisition of the two analog channels
  --digital               : perform acquisition of the eight digital channels
  --csv                   : save in Comma Separated Values file (CSV) format
  --vcd                   : save in Value Change Dump (VCD) format
   -i
  --interactive           : keep running after acquisition and allow to start another one
   -n
  --non-interactive       : exit after acquisition
  --acq-depth <depth>     : acquisition depth
  --acq-length <length>   : acquisition length
  --dig-trigger <param>   : digital trigger channel [0..7] & value [LHFR], example: D1:L
  --ac-coupling           : AC coupling
  --dc-coupling           : DC coupling
  --probe-10x             : using a 10x probe
  --probe-1x              : using a 1x probe
  --file-name <name>      : file name
  --trigger-edge <param>  : analog acquisition trigger edge: [any|falling|rising]
  --trigger-level <level> : analog acquisition trigger level
  --range-min <range>     : analog acquisition minimum range
  --range-max <range>     : analog acquisition maximum range
  --enable-log            : enable log and print it
   -h
  --help                  : show this message
```

Defaults: vcd(smartscope.vcd), non-interactive, 1M, 0.2s

Analog: trigger is set to 'Falling Edge' on channel 'A' and mode is set to 'Single', trigger level to 1.0. Coupling to DC, range to [-3, 3], probe is 10X.

Digital: trigger is set to 'L' on channel 'D0' and mode is set to 'Single'.

### Interactive mode

In interactive mode the data is saved the program waits for intput and you can trigger some actions:

  * Replace: perform a new acquisition en replace the content of the file.
  * [ : select previous Acquisition Depth
  * ] : select next Acquisition Depth
  * < : select previous Acquisition Length
  * > : select next Acquisition Length
  * Q : quit (X and Escape will do the same)


Selectable Acquisition Depth: 4M 2M 1M 512K 256K 128K

Selectable Acquisition Length (s): 0.000001, 0.000002, 0.000005 ... 0.5, 1, 2

### ToDo
- test command line options
- test analog csv data 
- more command line options: y offset, trigger holdoff, trigger modes (timeout, pulse, external(?))
- allow acquisition without a trigger
- allow mixed mode (1 analog channel + 8 digital channels)

