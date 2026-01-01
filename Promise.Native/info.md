PUBLISHING WIN PACKAGES FOR .NET 10
--------------------------------


- FIRST! DONT FORGET TO SELECT CORRECT FOLDER BEFORE PUBLISHING! It is Promise.Native folder!

- SECOND! comment out Android target in Native .csproj file! 
<!--<TargetFrameworks>net10.0-android;</TargetFrameworks>-->
for some reason otherwise for x86 and arm64 it fails.



THIRD! To publish Win packages for .NET 10, use the following command:

# x64
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true

# x86
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win-x86 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true

# ARM64
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win-arm64 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true



FORTH! To create bundle use the following command:

& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe" bundle /d . /p YouCent_2.0.11.0.msixbundle

(all 3 files from publish should be in the current folder in order to create the correct bundle for submition to legacy YouCent App)



---- end of publish instructions ----





when switching to new app from legacy one dont forget to change these in the Package.appxmanifest file (native/platforms/windows folder):

package identity name: ArkadyFenev.YouCentApp instead of  51862ArkFen.YouCent)
package publisher name: CN=4F5A9A46-AD65-453B-84E4-455CCAEA8E4E instead of CN=0C67DA6B-051F-46FD-83CF-694E2017EAC0)

and most likely packaging siplay name will be YouCent App instead of YouCent (if we will not be able to set YouCent free from legacy app)

but maybe it will no need to be done if for new app we wikll use a new code base created from scratch or partically copied from legacy one.