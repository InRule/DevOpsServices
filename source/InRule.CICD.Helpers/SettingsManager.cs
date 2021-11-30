using Microsoft.Azure;
using System;
using System.Configuration;
using static InRule.CICD.Helpers.IHelper;

namespace InRule.CICD.Helpers
{
    public class SettingsManager
    {
        public static string Get(string settingName)
        {
            var isCloudRaw = ConfigurationManager.AppSettings["IsCloudBased"];
            bool isCloud;
            if (bool.TryParse(isCloudRaw, out isCloud))
            {
                if (isCloud)
                {
                    return CloudConfigurationManager.GetSetting(settingName);
                }
            }

            return ConfigurationManager.AppSettings[settingName] != null ? ConfigurationManager.AppSettings[settingName].Trim() : "";
        }

        public static InRuleEventHelperType GetHandlerType(string handlerMoniker)
        {
            InRuleEventHelperType handlerType;
            if (Enum.IsDefined(typeof(InRuleEventHelperType), handlerMoniker))
                Enum.TryParse(handlerMoniker, out handlerType);
            else
            {
                string handlerTypeInConfig = Get($"{handlerMoniker}.Type");
                Enum.TryParse(handlerTypeInConfig, out handlerType);
            }

            return handlerType;
        }
    }
}
