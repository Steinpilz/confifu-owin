using Confifu.Abstractions;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OwinConfiguration = System.Action<Owin.IAppBuilder>;

namespace Confifu.Owin.Config
{
    public static class AppConfigExtensions
    {
        public class Config
        {
            private readonly IAppConfig appConfig;

            OwinConfiguration configuration = appBuilder => { };

            internal Config(IAppConfig appConfig)
            {
                this.appConfig = appConfig;
            }

            internal void InitDefaults()
            {
                this.appConfig.SetOwinConfiguration(() => configuration);
            }

            public Config SetConfiguration(OwinConfiguration configuration)
            {
                this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                return this;
            }

            public Config AddConfiguration(OwinConfiguration configuration)
            {
                this.configuration += configuration ?? throw new ArgumentNullException(nameof(configuration));
                return this;
            }
        }

        public static IAppConfig UseOwin(this IAppConfig appConfig, Action<Config> configurator = null)
        {
            var config = appConfig.EnsureConfig("Owin", () => new Config(appConfig), c =>
            {
                c.InitDefaults();
            });
            configurator?.Invoke(config);
            return appConfig;
        }

        internal static IAppConfig SetOwinConfiguration(
            this IAppConfig appConfig, 
            Func<OwinConfiguration> configurationFactory
            )
        {
            appConfig["Owin:ConfigurationFactory"] = configurationFactory;
            return appConfig;
        }

        public static Action<IAppBuilder> GetOwinConfiguration(this IAppConfig appConfig)
        {
            var configurationFactory = appConfig["Owin:ConfigurationFactory"] as Func<OwinConfiguration>;
            return configurationFactory?.Invoke() ?? (_ => { });
        }
    }
}
