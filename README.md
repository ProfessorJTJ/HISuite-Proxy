# [HISuite-Proxy](https://github.com/ProfessorJTJ/HISuite-Proxy/releases/latest)
Modifying HiSuite and manipulating it's connection data to install Roms before they officially get released.

![GitHub Logo](http://uupload.ir/files/mdq8_capture.png)

0. Download and install HISuite V10.0.0.510 or V10.0.1.100
1. Remove TeamMT host address from Windows/System32/Drivers/etc/Hosts (just incase you've edited that file before.
2. Click on HTTP Component, choose the official httpcomponent.dll, pick a folder to save the patched one, replace the patched one with the official httpcomponent.dll.
3. Open hisuite, go to settings and set proxy as 127.0.0.1 and port 7777

Note: some firmwares require "preloaded" firm with them (initial EMUI 10 release for instance)
For those firms, you need to check the box "Has Preloaded Package" and put the url of preloaded firm in the third text box (Preloaded PKG)
Also include CUST package ( that's related to phone region and career I guess, you might want to add it if you wish, and don't forget to check the box "CUST PKG" )

Firms example: ( Mate 10 Pro, Japan's EMUI 10 beta )

Base: http://update.dbankcdn.com/TDS/data/files/p3/s15/G5398/g1755/v341266/f1/full/changelog_base.xml
Cust: http://update.dbankcdn.com/TDS/data/files/p3/s15/G5398/g1755/v341265/f1/full/changelog_cust_hw_jp.xml
Preload: http://update.dbankcdn.com/TDS/data/files/p3/s15/G5398/g1755/v341267/f1/full/changelog_preload_hw_jp_R1.xml


"Debug Logging" Logs the incoming requests inside logs.txt file, near the application, can be checked for further infomration about the installation process.

[System Recovery Rom Instruction](https://github.com/ProfessorJTJ/HISuite-Proxy/wiki/System-Recovery-Rom-Instruction)
