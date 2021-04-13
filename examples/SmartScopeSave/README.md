# SmartScopeSave

A copy of SmartScopeConsole and modified so it can save data. For example to be imported in Sigrok.

Two file formats are supported: Value Change Dump (VCD) and Comma Seperated Values (CSV).

Data can be collected in two modes:

  * Analog, collect analog signals from both the analog channels.
  * Digital, collect digital signals from the eight digital inputs.


Command line options:

```
Usage:
  --analog              : perform acquisition of the two analog channels
  --digital             : perform acquisition of the eight digital channels
  --csv                 : save in Comma Seperated Values file (CSV) format
  --vcd                 : save in Value Change Dump (VCD) format
   -i
  --interactive         : keep running after acquisition and allow to start another one
   -n
  --non-interactive     : exit after acquisition
  --acq-depth <depth>   : acquisition depth
  --acq-length <length> : acquisition length
  --trigger <param>     : trigger channel [0..7] & value [LHFR], example: D1:L
  --file-name <name>    : file name
  --enable-log          : enable log and print it
   -h
  --help                : show this message
```

Defaults: vcd(smartscope.vcd), non-interactive, 1M, 0.2s

Analog trigger is set to 'Falling Edge' on channel 'A' and mode is set to 'Single'.

Digital trigger is set to 'L' on channel 'D0' and mode is set to 'Single'.

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


