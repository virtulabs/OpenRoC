# OpenRoC
Open-source Restart on Crash alternative with Sensu support.

## End User Documentation
  - [Main Window](#main-window)
    - [Top Menu Bar](#top-menu-bar)
    - [Process List](#process-list)
      - [Process Column](#process-column)
      - [Status Column](#status-column)
    - [Performance Graph](#performance-graph)
    - [Bottom Status Bar](#bottom-status-bar)
    - [Right-click Menu](#right-click-menu)
    - [Taskbar Menu](#taskbar-menu)
  - [Process Window](#process-window)
    - [Process Path](#process-path)
    - [Crash Settings](#crash-settings)
    - [Pre-start Event](#pre-start-event)
    - [Post-stop Event](#post-stop-event)
    - [MISC. Settings](#misc-settings)
  - [Settings Window](#settings-window)
    - [Start Settings](#start-settings)
    - [Sensu Settings](#sensu-settings)


## Main Window
![Main Window screenshot](OpenRoC/Docs/main.png?raw=true)

### Top Menu Bar

 - **Add:** Adds a new process to monitor. Clicking this button will open up a [Process Window](#process-window).
 - **Delete:** Removes selected processes in the [Process list](#process-list) section. You may multi-select different Processes or use the CTRL key to select them individually.
 - **Settings:** Opens up OpenRoC's [Settings Window](#settings-window).
 - **Logs:** Opens up OpenRoC's Logs Window. Logs are also stored and rotated (every 10MB) into a file named `OpenRoC.log` next to where you have `OpenRoC.exe` located.
 - **About:** Opens up OpenRoC's About Window. You may find third-party licenses and credits here.

### Process List

#### Process Column
Lists all currently monitored Processes. You may use the check mark next to a Process to disable its monitoring. In order to add a Process to this list, you can either use the [Add](#add) button or you can do so by accessing the [Right-click menu](#right-click-menu). Double-clicking on a Process opens up its [Process Window](process-window) to edit.

#### Status Column
Reports the current state of the Process as well as the time it has spent since the last time it entered this state. States are:

 - **Running:** Process is running correctly and being monitored for crashes.
 - **Stopped:** Process is stopped (not running) and being monitored.
 - **Disabled:** Process is stopped and is not being monitored.

### Performance Graph
It shows a line-graph of the cumulative % usage of three main resources on the computer OpenRoC is running on. CPU usage (red), RAM usage (blue), and GPU usage (green) are data gathered for the past few seconds.

### Bottom Status Bar
Shows useful tips and explanations about some highlighted UI items.

### Right-click Menu
You can access this menu by right-clicking anywhere in the [Process column](#process-column) section. Following buttons are available in this menu:

 - **Add:** same as [Add](#add).
 - **Edit:** same as double clicking a Process in [Process column](#process-column).
 - **Delete:** same as [Delete](#delete)
 - **Disable:** same as using check boxes of a Process in [Process column](#process-column).
 - **Start Process(es):** sets state of a Process to *running*.
 - **Stop Process(es):** sets state of a Process to *stopped*.
 - **Show Window(s):** sets state of a Process to *disabled*.

### Taskbar Menu
You can access this menu by right-clicking on OpenRoC's icon in your task-bar while it's running.

 - **Show/Hide:** Toggles UI visibility of OpenRoC.
 - **Exit:** Quits OpenRoC.

## Process Window
![Process Window screenshot](OpenRoC/Docs/process.png?raw=true)

### Process Path
 - **Executable Path:** path to the Process `.exe`. Only `.exe` is allowed (e.g `.bat` files are not allowed).
 - **Working Directory:** working directory of the Process. This is usually the same directory where the Process `.exe` is located.

### Crash Settings
 - **Crashed if not running:** Assume a Process is crashed if it's not running. If you set the initial state to *running* and OpenRoC sees Process is *stopped*, it will go into the *running* state.
 - **Crashed if unresponsive:** Some Processes may have unresponsive windows, in that case OpenRoC assumes it is crashed and restarts it.
 - **Unresponsiveness time period:** The number of seconds passed until OpenRoC assumes an unresponsive Process is crashed.
 - **Grace period:** The number of seconds to wait before attempting a relaunch of a crashed Process.

### Pre-start Event
You can specify a script to be executed before OpenRoC attempts to start the Process again. This can be an `.exe`, a `.py` or any other shell executable path. OpenRoC will wait for the script to be executed before starting the Process.

### Post-stop Event
You can specify a script to be executed after OpenRoC stops the Process (or Process encounteres a crash). This can be an `.exe`, a `.py` or any other shell executable path. OpenRoC will wait for the script to be executed before starting the Process. You can also opt-in for an aggressive method of stopping processes (*recommended*).

### MISC. Settings
Most options in this part are self-explanatory. Most notably you can set the initial state of the Process after you hit *Save* and return to the [Main Window](#main-window).

## Settings Window
![Settings Window screenshot](OpenRoC/Docs/settings.png?raw=true)

### Start Settings

 - **Start Minimized:** Enables OpenRoC to start minimized in task-bar next time it launches. UI visibility can be toggled via the [Taskbar Menu](#taskbar-menu).
 - **Single Instance Mode:** Forces only one instance of OpenRoC being able to launch on this machine. You *may* need administrator privileges for this feature to work.

### Sensu Settings
Enables [Sensu Client Socket](https://sensuapp.org/docs/0.25/reference/clients.html#client-socket-input) support via UDP. You need to install and configure Sensu client separately. Sensu clients can be downloaded and installed from [Sensu Downloads](https://sensuapp.org/downloads) page.
