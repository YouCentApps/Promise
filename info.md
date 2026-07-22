to setup iOS development environment, you need to follow these steps:

https://luke.digital/net-maui-without-my-mac-part-1/

https://luke.digital/net-maui-without-my-mac-part-2/

there I learned bunch of stuff to build the ios_deploy_promisenative.yml

and this is how I used openssl on my machine along the way in GIT folder

1. opened powershell in admin mode
2. went to Progream Files\Git\usr\bin
3. created ark dir (if not yet)
4. .\openssl.exe genrsa -out ark\distribution.key 2048 - run this and other commands this way... 
dont forget .\ before openssl.exe and also  ark\ before distribution.key and other files like this...



P.S. as of 12 of April 2026 after long test on GitHub Actions I must tell 
it took 6 hours and I ran out of minutes and it was not successful

SO... I guess we need to get MAC and then remove it all here )))








===================


ANDROID release creation command

dotnet publish -f net10.0-android -c Release -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=YouCentAppAndroidKey.keystore -p:AndroidSigningKeyAlias=YouCentAppAndroidKey -p:AndroidSigningKeyPass='!f' -p:AndroidSigningStorePass='!f'