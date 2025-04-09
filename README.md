# RDPManager
This application was created to address a short-come of Windows 11.  In Windows 10, I could add any folder as a Toolbar to the Taskbar and Windows would build the menu from the folder structure; as shown below.

|<img title="Windows10" alt="Windows10" src="/doc/Windows10-Toolbar.png">|<img title="Windows10-Menu" alt="Windows10-Menu" src="/doc/Windows10-Toolbar-Menu.png">|

I searched and it seems that other people ran into the same limitation and the only option is to use a third-party taskbar replacement.

I thought, why not create a System Tray application to replace the Windows 10 feature.

####About me
```
    1. I am an engineer but I have never programmed in CSharp before, so this program may not be optimized.
    2. I got help from AI, I wouldn't even know where to begin without AI.
    3. AI generated code in Allman style, but I prefer K&R style, so some files are modified to to the K&R style.
    4. This is my first Open Source projecct, so please be kind.
```

#### Features
    1. Monitor one or more folders
    2. Sort on recently used option
    3. Mark a connection as favorite using Right-Click

# Installation
Extract the zip file and run the <b>RdpManager.exe</b>

# Settings

### Directory structure for C:\RDWeb
<img title="RDPFileStructure" alt="RDPFileStructure" src="/doc/RDPFileStructure.png">

## Monitor One folder
When monitoring a single folder, the monitored folder is omitted from the menu; any .rdp files under the monitored folder are added as top-level menu item.
| Monitored Folder(s) | Menu Structure |
| -------- | ------- |
|<img title="MonitorRDWebFolder" alt="MonitorRDWebFolder" src="/doc/Monitored-RDWeb-Folder.png">|<img title="RDWebFolder-Menu" alt="RDWebFolder-Menu" src="/doc/RDWeb-Menu-Structure.png">|
|<img title="MonitoredNoneProd" alt="MonitoredTwoFolders" src="/doc/Monitored-Non-Prod.png">|<img title="Non-Prod-Menu" alt="Non-Prod-Menu" src="/doc/Non-Prod-Structure.png">|

### Monitored Two folders
When monitoring two or more folders, connections are groupped under the monitored folders name
| Monitored Folder(s) | Menu Structure |
| -------- | ------- |
|<img title="MonitoredTwoFolders" alt="MonitoredTwoFolders" src="/doc/Monitored-Two-Folders.png">| <img title="TwoFolders-Menu" alt="TwoFolders-Menu" src="/doc/Two-Folders-Structure.png">|

<b>Build</b>
~~~
dotnet clean && dotnet build
dotnet publish -c Release -r win-x64 --self-contained true
~~~
