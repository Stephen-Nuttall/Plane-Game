namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class Constants
    {
        public const string k_PackageName = "com.unity.multiplayer.playmode";
        public const string k_PackageRoot = "Packages/" + k_PackageName + "/";
        public const string k_VirtualProjectsFolder = "Library/VP/";
        public const string k_TempRootPath = "Temp/com.unity.multiplayer.playmode/";

        public const string RootPath = "Packages/" + k_PackageName + "/Scenarios/Editor/";
        public const string StylesRootPath = RootPath + "/Assets/Styles/";
        public const string UxmlRootPath = RootPath + "/Assets/Uxmls/";

        // Script define, that is used to enable internal development features. Can be activated via ManagePlayModeConfigs Window context Menu.
        public const string k_InternalDevelopmentDefine = "DEDICATED_SERVER_INTERNAL_DEVELOPMENT";
    }
}
