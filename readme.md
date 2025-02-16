
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


## How to use it?
Let's start simple, let's say you have a typical ``main.cpp`` file that prints hello world to the console

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