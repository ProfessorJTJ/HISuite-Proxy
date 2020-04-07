# HISuite-Proxy
Modifying HiSuite and manipulating it's connection data to install Roms before they officially get released.

![GitHub Logo](http://uupload.ir/files/yafo_bandicam_2020-04-07_13-03-45-612.jpg)

1. Remove TeamMT host address from Windows/System32/Drivers/etc/Hosts (just incase you've edited that file before.
2. Replace HiSuite httpcomponent.dll (just if it's version 10) by clicking on "HTTP Component" button
3. Open hisuite, go to settings and set proxy as 127.0.0.1 and port 7777

Note: some firmwares require "preloaded" firm with them (initial EMUI 10 release for instance)
For those firms, you need to check the box "Has Reloaded Package" and put the url of preloaded firm in the second text box (Reloaded PKG URL)
