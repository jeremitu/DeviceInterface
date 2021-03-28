# SmartScopeSave

A copy of SmartScopeConsole and modified so it can save data to be imported in Sigrok.

It collects digital signals using the B-channel and saves them into a VCD file (called "smartscope.vcd").
Trigger is set to 'L' on channel 'D0' and mode is set to 'Single'.

Default configured Acquisition Depth is 1Mb and Acquisition Length is 0.2s. The way the SmartScope decides what the actual values will be are a mystery to me.

When the data is saved the program waits for intput and you can trigger some actions:

  * Replace: perform a new acquisition en replace the content of the file.
  * [ : select previous Acquisition Depth
  * ] : select next Acquisition Depth
  * < : select previous Acquisition Length
  * > : select next Acquisition Length
  * Q : quit (X and Escape will do the same)


Selectable Acquisition Depth: 4M 2M 1M 512K 256K 128K

Selectable Acquisition Length (s): 0.000001, 0.000002, 0.000005 ... 0.5, 1, 2

There is also code to save into a CSV file but that has been disabled.

