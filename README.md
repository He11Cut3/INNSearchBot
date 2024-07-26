# INNSearchBot

<a href="https://ibb.co/RpH82Z0"><img src="https://i.ibb.co/hR80shW/2024-07-26-203143.png" alt="2024-07-26-203143" border="0"></a>


![Static Badge](https://img.shields.io/badge/Framework-WPF_(Net_FrameWork_4.8)-purple?logo=dotnet) ![Static Badge](https://img.shields.io/badge/Language-C%23-purple?logo=csharp) ![Static Badge](https://img.shields.io/badge/DataBase-MSSQL-purple?logo=microsoftsqlserver)

## About

The bot executes the following commands:

```/start``` - Start working with the bot, displaying a welcome message.  
```/help``` - List of available commands.  
```/hello``` - Developer contact information.  
```/inn <INN>``` - Obtaining information about the company by INN.  
```/okved <TIN>``` - Obtaining information about the company’s OKVED code using the TIN.  
```/last``` - Repeats the last command executed by the user.  

## Installation  

1. Clone the repositories:  

```
git clone https://github.com/yourusername/INNSearchBot.git
```
2. Go to the project directory:  
```
INNSearchBot CD
```
3. Set depending:  
- Make sure you have the .NET SDK and required libraries installed.
- Use the NuGet package manager to install the required packages if they are not installed:
```
dotnet add package Telegram.Bot
dotnet add package Newtonsoft.Json
```
4. Set up an environment variable or appsettings.json configuration file to store the Telegram Bot API token and Dadata API switch.

## Developers

- [He11Cut3](https://github.com/He11Cut3)

