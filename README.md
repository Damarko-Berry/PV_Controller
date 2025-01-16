# PV_Controller

This is a controller MAUI for my PV_Client. It connects via SSDP, so it should find any clients and connect to them as long as you're connected to the same network. Very minimalistic and comprehensive design. 


This project is apart of the PV family:
## Related PV projects:
- [PseudoVision]( https://github.com/Damarko-Berry/PseudoVision )
- [PV_Client]( https://github.com/Damarko-Berry/PV_Client )

_I may or may not have added the just enough try-catch blocks for it not to crash. If there is no release availible, you'll have to clone, add proper exception handling when it comes to trying to send commands to a client that is no longer availible. If there is a release, then I did it, and forgot to remove this message. Should be fairly simple. I'm just lazy😅_ 


This project will evolve the more I learn about VLC's rc module, so stay tuned.
