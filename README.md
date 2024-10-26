# FanScript
Programming language that compiles into [Fancade](https://www.fancade.com/) blocks  
Compiler based on [Minsk](https://github.com/terrajobst/minsk)  
Documentation can be found [here](https://github.com/BitcoderCZ/FanScript-Documentation/blob/main/MdDocs/README.md)

## Building
- Make sure you have installed [dotnet 8 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Run `dotnet build -c Release`

## VSCode Extension
To run the extension:
- run `npm install` in the VSCodeExtension folder
- run `npm install` in the VSCodeExtension/client folder
- open vscode
- open folder, select VSCodeExtension
- in Run and Debug, select "Launch Client" and press F5
- a new window will open, create/open a file with .fcs extension