using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    struct StateRepositoryDelegates
    {
        // This simply represents the static methods of SessionState
        internal delegate string GetStringOrDefault(string key);
        internal delegate void Save(string key, string value);
        internal delegate void Clear(string key);

        internal GetStringOrDefault GetStringOrDefaultFunc;
        internal Save SaveFunc;
        internal Clear ClearFunc;
    }

    static class SessionStateRepository
    {
        public static StateRepositoryDelegates Get => new StateRepositoryDelegates
        {
            GetStringOrDefaultFunc = key => SessionState.GetString(key, string.Empty),
            SaveFunc = SessionState.SetString,
            ClearFunc = SessionState.EraseString,
        };
    }
}
