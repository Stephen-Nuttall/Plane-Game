using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Common.Editor
{
    static class UxmlBinding
    {
        public static bool Bind<T>(
            this INotifyValueChanged<T> control,
            [NotNull] Func<T> get,
            [NotNull] EventCallback<ChangeEvent<T>> set)
        {
            control.SetValueWithoutNotify(get.Invoke());

            if (control is not CallbackEventHandler callbackEventHandler)
            {
                return false;
            }

            callbackEventHandler.RegisterCallback(set);
            return true;
        }
    }
}
