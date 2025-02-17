
# BBuild

A lightweight build tool for C++ projects made in .NET Core

## Why did i make this ?
Building a project should never be an obstacle when coding, and i got tired of dealing with the cursed CMake syntax everytime i wanted to do something slightly different than building a "Hello world" app , so decided to make my own build tool with blackjack and hookers, something sane, lightweight and easily editable, this is my attempt at making that.


## Who can use it ?
For now this is usable on Windows only, it uses the MSVC complier and linker ``cl.exe``, ``lib.exe`` and ``link.exe``, I have no plans to make this support multiple compliers for now, the focus for me is to make it work better as need be and add more options.
## Why this is different from CMake?

The project is a wrapper over ``cl.exe``, ``link.exe`` and ``lib.exe``along with a couple of JSON files describing each project.

Your first thought might be 
>isn't that CMake with extra steps?

No, here's why :
- This doesn't use a godforsaken language like CMake , is uses C#
- This isn't a build system generator , it's a build system ***and even calling that is a stretch*** , it's 3 processes on top of eachother wearing a trenchcoat
- It's easily exetendable, if you want to perform a custom action related to the build, all you have to do is provide a DLL that have a method that does what you want and it's up to you to call it prebuild or post build


## How to use it? Let's start simple
Let's start simple, let's say you have a typical ``main.cpp`` file that prints hello world to the console , let's say we're in this kind of folder structure

```md
CppTestProject
└── Console
    ├── Build
    │   └── Objects
    │   └── Build
    │       └── Exe
    └── Source
        └── main.cpp
```
To make this a project visible by ``BBuild``, all we have to do is drop in 1 extra files : ``Build.json``

```md
CppTestProject
└── Console
    ├── Build
    │   └── Objects
    │   └── Build
    │       └── Exe
    └── Source
    │   └── main.cpp
    └── Build.json
```

<details>
<summary><code>Build.json</code> should look something like this (Click to expand) </summary>

```json
{
    "Name": "HelloWorldConsole",
    "Description": "Building a simple Console program with BBuild",
    "CompilerResources":
    {
        "CompilerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/cl.exe",
        "LibPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/lib.exe",
        "LinkerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/link.exe"
    },
    "LibrariesFolderPaths": 
    [
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x64",

        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x86"
    ],
    "HeaderIncludeFolders": 
    [
        "C:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/ucrt",
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/include"
    ],
    "SourceFiles": 
    [
        "Source/main.cpp"
    ],
    "CompilerFlags": 
    [
        "DEBUG"
    ],
    "ObjectFilesPath": "Build/Objects",
    "PBDFilename": "main.pdb",
    "BuildOutputs": 
    [
        {
            "OutputType": "Executable",
            "Filename": "main",
            "FolderPath": "Build/Outputs/Exe"
        }
    ]
}
```
</details>

That's it !
To launch a build, first, make sure that BBuild.exe is added to the "PATH" environment variable , then go to the folder that contains ``Build.json`` and run the command 
```batch
::: You can it directly in the folder containing Build.json
> BBuild
```
or
```batch
::: You can specify the path to the folder containing Build.json by using the "/Path" argument
> BBuild /Path "BasePath/CppTestProject/Console" 
```
Your build should be done in an instant and you'll find your ``main.exe`` at `CppTestProject/Console/Build/Exe/main.exe`as indicated in the ``"BuildOutputs"`` value in the ``Build.json``.

## Let's show a more involved example

This is where Things get interresting, let's see how we would setup the project in the following situation

```md
CppTestProject
└── Callback
│   └── BBuildCallback.dll
│
└── Library
│   ├── Objects
│   ├── Build
│   │   └── Dll
│   └── Source
│   │   └── main.cpp
│   └── Build.json
│
└── Console
    ├── Build
    │   └── Objects
    │   └── Build
    │       └── Exe
    └── Source
    │   └── maths.cpp
    └── Build.json
```

In this case, out project ``Console`` is almost the same executable as the previous example, except this time it depends on a Dll produced by ``Library`` project.

<details>
<summary><code>main.cpp</code> should look something like this (Click to expand) </summary>

```cpp
#include <stdio.h>

__declspec(dllimport)
int Add(int a , int b);

int main(int argc , char** argv)
{
    int result = Add(5 , 3);
    printf("Hello world from BBuild !! The result is %d" , result);
    return 0;
}
```
</details>

<details>
<summary><code>maths.cpp</code> should look something like this (Click to expand) </summary>

```cpp
__declspec(dllexport)
int Add(int a , int b)
{
    return a + b;
}
```
</details>

Let's start by checking ``Build.json`` for the library project

</details>

<details>
<summary><code>Library/Build.json</code> should look something like this (Click to expand) </summary>

```json
{
    "Name" : "MyMathDll",
    "Description" : "A Dll exporting an Add method",
    "CompilerResources":
    {
        "CompilerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/cl.exe",
        "LibPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/lib.exe",
        "LinkerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/link.exe"
    },
    "CompilationSettings" : 
    {
        "Platform" : "x64",
        "ExceptionHandling" : ["EHs" , "EHc"],
        "WarningLevel" : "W4",
        "WarningsAsError" : true,
        "DebugInformation" : "Zi",
        "EnabledSanitizers" : ["AddressSanitizer"],
        "LanguageStandard" : "Cpp17",
        "OptimizationLevel" : "Ot",
        "UseJumpTableRData" : true,
        "ProcessCount" : 8
    },
    "LibrariesFolderPaths" : 
    [
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x64",

        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x86"
    ],
    "HeaderIncludeFolders" :
    [
        "C:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/ucrt",
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/include"
    ],    
    "SourceFiles" : 
    [
        "Source/maths.cpp"
    ],
    "CompilerFlags" : 
    [
        "DEBUG"
    ],
    "ObjectFilesPath" : "Build/Objects",
    "PBDFilename" : "maths.pdb",
    "BuildOutputs" :
    [
        {
            "OutputType" : "Dll",
            "Filename" : "maths",
            "FolderPath" : "Build/Outputs/Dll" 
        }
    ]
}
```
</details>

Seems simple enough, pretty much the same as the simeple ``Console`` example except the ``"BuildOutput"`` being ``Dll`` since that's what we need for the library.

***Now here's where things get interresting***, let's check out the new build file for ``Console``

<details>
<summary><code>Console/Build.json</code> should look something like this (Click to expand) </summary>

```json
{
    "Name": "MyFirstBuild",
    "Description": "Something to test the build program",
    "DependencyPaths": 
    [
        {
            "Name": "MathsLibrary",
            "Path": "../Library",
            "Outputs": "Dll"
        }
    ],
    "CustomVariables": 
    {
        "MyLibsPath": "../Library/Build/Outputs/Dll"
    },
    "PrebuildAction": 
    [
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.PrebuildAction"
        },
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CleanupObjectFilesFolder"
        },
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CleanupOutputFolder",
            "Params" : [ "Executable" ]
        }
    ],
    "PostbuildAction":
    [ 
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CopyOutputDllNextToExe",
            "Params": [ "MathsLibrary" ]
        },
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.PostbuildAction"
        }
    ],
    "CompilerResources":
    {
        "CompilerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/cl.exe",
        "LibPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/lib.exe",
        "LinkerPath": "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/bin/Hostx64/x64/link.exe"
    },
    "CompilationSettings" : 
    {
        "Platform" : "x64",
        "ExceptionHandling" : ["EHs" , "EHc"],
        "WarningLevel" : "W4",
        "WarningsAsError" : true,
        "DebugInformation" : "Zi",
        "EnabledSanitizers" : ["AddressSanitizer"],
        "LanguageStandard" : "Cpp17",
        "OptimizationLevel" : "Ot",
        "UseJumpTableRData" : true,
        "ProcessCount" : 8
    },
    "SourceFiles": 
    [
        "Source/main.cpp"
    ],
    "LibraryFiles": 
    [
        "[MyLibsPath]/maths.lib"
    ],
    "LibrariesFolderPaths": 
    [
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x64",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x64",

        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/lib/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/ucrt/x86",
        "C:/Program Files (x86)/Windows Kits/10/Lib/10.0.22621.0/um/x86"
    ],
    "HeaderIncludeFolders": 
    [
        "C:/Program Files (x86)/Windows Kits/10/Include/10.0.22621.0/ucrt",
        "C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.41.34120/include"
    ],
    "CompilerFlags": 
    [
        "DEBUG"
    ],
    "ObjectFilesPath": "Build/Objects",
    "PBDFilename": "main.pdb",
    "BuildOutputs": 
    [
        {
            "OutputType": "Executable",
            "Filename": "main",
            "FolderPath": "Build/Outputs/Exe"
        }
    ]
}
```
</details>

Okay that 's a big wall of text , let's remove the parts we saw already and focus on the new ones

<details>
<summary><code>Console/Build.json</code> but only including the new changes (Click to expand) </summary>

```json
{
    "DependencyPaths": 
    [
        {
            "Name": "MathsLibrary",
            "Path": "../Library",
            "Outputs": "Dll"
        }
    ],
    "CustomVariables": 
    {
        "MyLibsPath": "../Library/Build/Outputs/Dll"
    },
    "LibraryFiles": 
    [
        "[MyLibsPath]/maths.lib"
    ],
    "PrebuildAction": 
    [
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CleanupObjectFilesFolder"
        },
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CleanupOutputFolder",
            "Params" : [ "Executable" ]
        }
    ],
    "PostbuildAction":
    [ 
        {
            "DllPath": "../Callback/BBuildCallback.dll",
            "MethodAssemblyName": "BBuildCallback.CopyOutputDllNextToExe",
            "Params": [ "MathsLibrary" ]
        }
    ],

}
```
</details>

So let's unpack how this works and explain what each value does:
- DependencyPaths : an array containing the list of projects the current one depends on , in our case , we need the Dll from the math project so we point to it using a relative path and we specify that we need the Dll (the name here doesn't have to match the actual project name , it's purely a label to give more information ).
> Note : "DependencyPaths" works recursively , for example :
>
> if A depends on B, and B depends on C , then starting a build for A will result in : Building C , Then building B then building A

- CustomVariables : a dictionay containing user-defined key-value pairs that can used in the project to avoid redundency and copy-pasting that same thing over and over, here we created a variable called ``"MyLibsPath"`` that containing the path to the Dll

- LibraryFiles : an array containing paths to the ``.lib`` files needed for the linking stage

- PrebuildAction : an array containing actions to perform before starting the compilation

- PostbuildAction : an array containing actions to perform after the end of the compilation

- For both ``PrebuildAction``and ``PostbuildAction`` Each action is expressed as follows
    - DllPath : A path to a C# Dll containing the a public static method that implements the action we need to perform
    - MethodAssemblyName : The full name of method that we need find inside the Dll , the naming goes as flllows ``[Namespace].[Method name]``
    - Params : a list of parameters that we can pass to the method
The signature of the method is the following
```csharp
    public static void PostbuildAction(BuildSettings settings, BuildContext context, JsonElement[] parameters)
    {
        Console.WriteLine($"> Postbuild called from project : {settings.Name} with {parameters.Length} params passed");
    }
```
In the example mentioned i used some Postbuild and Prebuild actions that are already provided and implemented in the ``BBuildCallback`` project