// TODO: MTT-8667 This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR

using System;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Multiplay.Authoring.Editor;
using Unity.Services.Multiplayer.Editor.Shared.DependencyInversion;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Utils;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Multiplay
{
    internal abstract class MultiplayNode : Node
    {
        [SerializeReference] protected IScopedServiceProvider m_ServiceProviderOverride;

        protected MultiplayNode(string name, IScopedServiceProvider serviceProviderOverride) : base(name)
        {
            m_ServiceProviderOverride = serviceProviderOverride;
        }

        protected IScopedServiceProvider CreateServiceProviderScope()
            => m_ServiceProviderOverride != null ? m_ServiceProviderOverride.CreateScope() : MultiplayAuthoringServices.Provider.CreateScope();

        protected async Task<IScopedServiceProvider> CreateAndValidateServiceProvider()
        {
            var provider = CreateServiceProviderScope();
            try
            {
                await ValidateEnvironment(provider);
                return provider;
            }
            catch (Exception)
            {
                provider.Dispose();
                throw;
            }
        }

        private async Task ValidateEnvironment(IScopedServiceProvider provider)
        {
            var environments = provider.GetService<IEnvironmentsApi>();
            await environments.RefreshAsync();
            var environmentValidation = await environments.ValidateEnvironmentAsync();
            if (environmentValidation.Failed)
                throw new InvalidOperationException(environmentValidation.ErrorMessage);
        }

        private string GetInputNotSetErrorMessage(string inputName) => $"{inputName} is not set";

        protected void ValidateInputIsSet(NodeInput<string> input, string inputName)
        {
            if (string.IsNullOrEmpty(GetInput(input)))
                throw new InvalidOperationException(GetInputNotSetErrorMessage(inputName));
        }

        protected void ValidateInputIsSet<T>(NodeInput<T> input, string inputName)
        {
            var value = GetInput(input);
            if (value == null || GetInput(input).Equals(default(T)))
                throw new InvalidOperationException(GetInputNotSetErrorMessage(inputName));
        }

        protected static void ValidateNameParameter(string value, string parameterName)
        {
            var regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9-]+$");

            if (!regex.IsMatch(value))
                throw new InvalidOperationException($"{parameterName} must only contain alphanumeric characters and hyphens");
        }
    }
}
#endif
