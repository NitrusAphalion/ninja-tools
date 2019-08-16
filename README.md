# ninja-tools
A file viewer for NinjaTrader logs, traces and databases to make it easier to draw out the relevant information. I will maintain and improve this as I have time and I welcome participation from anyone. 

##### Disclaimer
This is a personal project. It is not affiliated with NinjaTrader in any way, shape or form. Neither is it affiliated with any of the 3rd parties mentioned in the Technical Details section below.

[![](https://user-images.githubusercontent.com/12155532/63139325-21d5b300-bf9b-11e9-84f7-dbce6bafa30e.png)](https://user-images.githubusercontent.com/12155532/63139325-21d5b300-bf9b-11e9-84f7-dbce6bafa30e.png)

### Installer
[Download Here](https://github.com/NitrusAphalion/ninja-tools/releases/tag/0.1 "Download Here")
- Only x64 at this time

### Features
###### Interface
- Tabbed interface which partially saves state on exit
- Integration with Windows context menu
- Open one file, multiple files, .zip (logs and traces only) or whole folder
- Multiple themes (**Only Dark theme available currently**)

###### Logs and Traces (.txt)
- Syntax highlighting
- Advanced filtering with ability to export results to a new tab via context menu
- View log and trace side by side with locked scrolling (**Not implemented**)

###### Workspaces, Templates, Config and UI (.xml)
- Syntax highlighting and code collapsing
- Additional tree view to easily navigate
- Basic analysis of workspace contents

###### Database (.sdf)
- Ability to view tables in datagrid
- Ability to run queries (**Not implemented**)
- Ability to edit values from data grid (**Not implemented**)

### How filtering works for logs and traces
- You can click the (+) icon to add more conditions to a given filter and the (-) icon to remove conditions
- Different colors between conditions represents an OR condition (aka either condition A is true or condition B is true)
- Transparent color always represents an OR condition (aka two transparent filters means either must be true)
- All other colors represent AND conditions (aka two red filters means both must be true)
- When using a manual condition you can press Enter to update the filter

### Technical Details
- [Stylet](https://github.com/canton7/Stylet "Stylet") is used as the MVVM framework
- [AvalonEdit](https://github.com/icsharpcode/AvalonEdit "AvalonEdit") is used for the Editor
- [XceedWpfToolkit](https://github.com/xceedsoftware/wpftoolkit "XceedWpfToolkit") is used for the DateTime picker
- [OokiiDialogsWpf](https://github.com/caioproiete/ookii-dialogs-wpf "OokiiDialogsWpf") is used for the folder browser
- [FamFamFam](http://www.famfamfam.com/lab/icons/silk/ "FamFamFam") is used for the icons
