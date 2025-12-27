to publish Win packages for .NET 10, use the following command:

# x64
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true

# x86
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win-x86 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true

# ARM64
dotnet publish -f net10.0-windows10.0.19041.0 -c Release -r win-arm64 -p:WindowsPackageType=MSIX -p:WindowsAppSDKSelfContained=true