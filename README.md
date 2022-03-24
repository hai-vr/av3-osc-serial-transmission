# av3-osc-serial-transmission
WIP 🚧 POC Arbitrarily large data transmission over 1 Bool with synchronous serial communication for Avatars 3.0 OSC. Designed for transmission of low update rate data (weather, heart rate, local time, etc.).

TODO:
- Solve permanent desync
  - When data is updated continuously, there's a chance the receiver side won't be able to figure out the beginning of a message. This needs to be solved
- Allow multiple data streams
  - Extend python script to handle an arbitrary amount of bit streams
  - Split CLK and animator
- Clean up python script
  - Use idiomatic python (I'll need help from a friend)
  - Use proper coroutines
- Propose using it as an OSC relay
  - Expose simple OSC server for queuing messages
- Allow transmission of data across more than 1 data line
  - Extend animator to allow transmission of data across more than 1 data line
  - Extend python script to allow transmission of data across more than 1 data line
- Detect culling
  - Extend animator to ignore incoming packets when the animator has recently been culled
