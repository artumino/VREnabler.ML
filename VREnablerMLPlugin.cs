using MelonLoader;
using AssetsTools.NET;
using System.IO;
using AssetsTools.NET.Extra;
using System.Collections.Generic;

[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.UNIVERSAL)]
[assembly: MelonInfo(typeof(VREnabler.ML.Plugin), "VREnabler", "0.0.1", "artum")]
[assembly: MelonGame(null, null)]
namespace VREnabler.ML
{
    public class Plugin : MelonPlugin
    {
        public const string k_VREnablerSettings = "VREnabler";
        
        public bool VRSupportEnabled {get; set;} = false;
        public int VROverrideStereoMode {get; set;} = -1;

        public string BackupFile(string fileName)
        {
            var backupName = fileName + ".bak";
            if(!File.Exists(backupName))
                File.Copy(fileName, backupName);
            return backupName;
        }

        public void ReadPreferences()
        {
            MelonPreferences.CreateCategory(k_VREnablerSettings, "VREnabler Settings");
            MelonPreferences.CreateEntry(k_VREnablerSettings, nameof(VRSupportEnabled), VRSupportEnabled, "Enable VR mode");
            MelonPreferences.CreateEntry(k_VREnablerSettings, nameof(VROverrideStereoMode), VROverrideStereoMode, "Override Stereo Mode: -1 don't override");
            VRSupportEnabled = MelonPreferences.GetEntryValue<bool>(k_VREnablerSettings, nameof(VRSupportEnabled));
            VROverrideStereoMode = MelonPreferences.GetEntryValue<int>(k_VREnablerSettings, nameof(VROverrideStereoMode));
        }

        public override void OnPreInitialization()
        {
            var dataDir = MelonLoader.MelonUtils.GetGameDataDirectory();
            var pluginPath = Path.Combine("Plugins", "VREnabler");
            var gameManagersPath = Path.Combine(dataDir, "globalgamemanagers");
            var backupPath = BackupFile(gameManagersPath);

            ReadPreferences();

            if(!VRSupportEnabled)
            {
                File.Copy(backupPath, gameManagersPath, true);
                return;
            }

            AssetsManager assetsManager = new AssetsManager();
            assetsManager.LoadClassPackage(Path.Combine(pluginPath, "classdata.tpk"));
            AssetsFileInstance assetsFileInstance = assetsManager.LoadAssetsFile(backupPath, false);
            AssetsFile assetsFile = assetsFileInstance.file;
            AssetsFileTable assetsFileTable = assetsFileInstance.table;
            assetsManager.LoadClassDatabaseFromPackage(assetsFile.typeTree.unityVersion);

            List<AssetsReplacer> replacers = new List<AssetsReplacer>();

            AssetFileInfoEx buildSettings = assetsFileTable.GetAssetInfo(11);
            AssetTypeValueField buildSettingsBase = assetsManager.GetTypeInstance(assetsFile, buildSettings).GetBaseField();
            AssetTypeValueField enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
            AssetTypeTemplateField stringTemplate = enabledVRDevices.templateField.children[1];
            AssetTypeValueField[] vrDevicesList = new AssetTypeValueField[] { StringField("OpenVR", stringTemplate) };
            enabledVRDevices.SetChildrenList(vrDevicesList);
            replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int)buildSettings.curFileType, 0xffff, buildSettingsBase.WriteToByteArray()));

            if(VROverrideStereoMode >= 0)
            {
                AssetFileInfoEx playerSettings = assetsFileTable.GetAssetInfo(1);
                AssetTypeValueField playerSettingsBase = assetsManager.GetTypeInstance(assetsFile, playerSettings).GetBaseField();
                AssetTypeValueField steroRenderingPath = playerSettingsBase.Get("m_StereoRenderingPath");
                steroRenderingPath.value = new AssetTypeValue(EnumValueTypes.ValueType_Int32, VROverrideStereoMode);
                replacers.Add(new AssetsReplacerFromMemory(0, playerSettings.index, (int)playerSettings.curFileType, 0xffff, playerSettingsBase.WriteToByteArray()));
            }


            using (AssetsFileWriter writer = new AssetsFileWriter(File.OpenWrite(gameManagersPath)))
            {
                assetsFile.Write(writer, 0, replacers, 0);
            }
        }

        static AssetTypeValueField StringField(string str, AssetTypeTemplateField template)
        {
            return new AssetTypeValueField()
            {
                children = null,
                childrenCount = 0,
                templateField = template,
                value = new AssetTypeValue(EnumValueTypes.ValueType_String, str)
            };
        }
    }
}