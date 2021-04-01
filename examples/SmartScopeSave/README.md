# SmartScopeSave

A copy of SmartScopeConsole and modified so it can save data to be imported in Sigrok.

Two file formats are supported: Value Change Dump (VCD) and Comma Seperated Values (CSV).

It collects digital signals using the B-channel and saves them into a VCD file (called "smartscope.vcd").
Trigger is set to 'L' on channel 'D0' and mode is set to 'Single'.


Command line options:

```
Usage:
  --csv                 : save in Comma Seperated Values file (CSV) format
  --vcd                 : save in Value Change Dump (VCD) format
   -i
  --interactive         : keep running after acquisition and allow to start another one
   -n
  --non-interactive     : exit after acquisition
  --acq-depth <depth>   : acquisition depth
  --acq-length <length> : acquisition length
  --file-name <name>    : file name
  --enable-log          : enable log and print it
   -h
  --help                : show this message
```

Defaults: vcd(smartscope.vcd), non-interactive, 1M, 0.2s

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


