**Description:**

_**Sptula** is to navigate web pages using the headless Chromium browser through the PuppeteerSharp library. The **script** extracts email addresses from the visited pages and provides the option to save these addresses to a file. Additionally, it allows the use of **proxies** for anonymized browsing._

[Download Project](https://github.com/4D7220426C7565/Sptula/releases)

**Features**

1. **User Interface:**
    - _Prompts the user if they want to use a proxy for browsing_
    - _Asks the user to enter a starting URL and a limit on the number of pages to visit._
    - _Optionally asks if they want to save the extracted email addresses to a file._

2. **Proxy Management:**
    - _Allows the user to specify a proxy in the format type://host:port (e.g., socks5://127.0.0.1:1080)._
    - _Validates the operational status of the proxy via a test HTTP request._

>_**Note❗** If you're not install dotnet yet._
>
>Debian ==> [Linux-debian](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian)


**_Important❗_**

_If needed: Change the path ```string chromePath = @"./../../../../../usr/bin/chromium";``` to a google executable **.exe** or other_

**Installation**

_**Creates .NET project**_
```Bash
dotnet new console --framework net8.0 --use-program-main -n <Project_Name>
```

_**After**_
```Bash
cd <Project_Name> ; dotnet build
```

_**Finally**_
```Bash
dotnet run Program.cs
```
_**Install package PuppeteerSharp**_
```
dotnet add package PuppeteerSharp --version 18.0.2
```

_At this point, clone the repository ```git clone https://github.com/4D7220426C7565/Sptula.git``` or copy the raw code, but first create the file ```classes.cs```_

_**On ASCIINEMA**_

[asciinema](https://asciinema.org/a/SHs3BAl8br9lsmtrFObitQf6K)


_**Demo:**_

![sptula](https://github.com/4D7220426C7565/Sptula/assets/171493198/32f10109-6239-4af3-8e62-1b0dddcdacee)
