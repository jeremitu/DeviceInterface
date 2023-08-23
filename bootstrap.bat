@rem JJ converted from bootstrap.sh for Windows
git submodule update --init
set SED="C:\Program Files\Git\usr\bin\sed.exe"
@rem change targetted Framework version. 
@rem %SED% -e "s/v4.5/v4.8.1/" Build/Projects/*.definition [**.csproj]
Protobuild.exe --generate Windows
%SED% -i~ -e "s/\(<UseSGen>False<\/UseSGen>\)/\1<Externalconsole>true<\/Externalconsole>/" examples/SmartScopeConsole/SmartScopeConsole.Windows.csproj
.nuget\NuGet.exe restore DeviceInterface.Windows.sln
