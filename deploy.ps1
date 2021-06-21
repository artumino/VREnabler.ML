dotnet restore
dotnet build --configuration Release

$BuildDir = './bin/Release/net35'
$DeployDir = './Deploy'
$PluginsDir = $DeployDir + '/Plugins'

Remove-Item $DeployDir -Force
mkdir $DeployDir
mkdir $PluginsDir
mkdir ($PluginsDir + '/VREnabler')
mkdir ($DeployDir + '/UserData')
Copy-Item -Path ($BuildDir + '/VREnablerPlugin.dll') -Destination ($PluginsDir + '/VREnablerPlugin.dll')
Copy-Item -Path ($BuildDir + '/AssetsTools.NET.dll') -Destination ($PluginsDir + '/AssetsTools.NET.dll')

#Copy dependencies
Copy-Item ('./External/classdata.tpk') -Destination ($PluginsDir + '/VREnabler/classdata.tpk')
Copy-Item ('./External/MelonPreferences.cfg') -Destination ($DeployDir + '/UserData/MelonPreferences.cfg')