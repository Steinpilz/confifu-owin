using OwinConfiguration = System.Action<Owin.IAppBuilder>;

namespace Confifu.Owin.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Confifu.Abstractions;
    using global::Owin;

    public static class AppConfigExtensions
    {
        public static IAppConfig UseOwin(this IAppConfig appConfig, Action<Config> configurator = null)
        {
            var config = appConfig.EnsureConfig("Owin", () => new Config(appConfig), c => { c.InitDefaults(); });
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

        public class Config
        {
            readonly IAppConfig appConfig;

            OwinConfiguration configuration = appBuilder => { };
            readonly StagesConfigurationBuilder stagesConfiguration = new StagesConfigurationBuilder();

            internal Config(IAppConfig appConfig) => this.appConfig = appConfig;

            internal void InitDefaults()
            {
                this.stagesConfiguration.AddConfiguration("default", x => this.configuration(x));

                this.appConfig.SetOwinConfiguration(this.stagesConfiguration.Build);
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

            public Config AddConfiguration(string stage, OwinConfiguration configuration)
            {
                this.stagesConfiguration.AddConfiguration(stage, configuration);
                return this;
            }

            public Config OrderStages(params string[] stages)
            {
                for (var i = 0; i < stages.Length - 1; i++) this.OrderStages(stages[i], stages[i + 1]);
                return this;
            }

            public Config OrderStages(string firstStage, string nextStage)
            {
                this.stagesConfiguration.Order(firstStage, nextStage);
                return this;
            }
        }
    }

    class StagesConfigurationBuilder
    {
        public Dictionary<string, List<StageOwinConfiguration>> Configurations { get; }
            = new Dictionary<string, List<StageOwinConfiguration>>();

        public Dictionary<string, HashSet<string>> Orders { get; }
            = new Dictionary<string, HashSet<string>>();

        public OwinConfiguration Build()
        {
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            var orderedStages = new List<StageOwinConfiguration>();

            IEnumerable<string> dependentStages(string stage) =>
                this.Orders.TryGetValue(stage, out var stages) ? stages : Enumerable.Empty<string>();

            IEnumerable<StageOwinConfiguration> configuration(string stage) =>
                this.Configurations.TryGetValue(stage, out var result)
                    ? result.AsEnumerable()
                    : Enumerable.Empty<StageOwinConfiguration>();

            void visitStage(string stage)
            {
                if (visiting.Contains(stage))
                    throw new InvalidOperationException($"Circular dependency detected for stage {stage}");

                if (visited.Contains(stage))
                    return;

                visited.Add(stage);
                visiting.Add(stage);

                foreach (var dependentStage in dependentStages(stage)) visitStage(dependentStage);

                orderedStages.AddRange(configuration(stage));

                visiting.Remove(stage);
            }

            void topSort()
            {
                foreach (var stage in this.Configurations.Keys)
                    visitStage(stage);
            }

            topSort();
            return appBuilder =>
            {
                foreach (var stage in orderedStages)
                    stage.Configuration(appBuilder);
            };
        }

        public void AddConfiguration(string stage, OwinConfiguration configuration)
        {
            if (string.IsNullOrEmpty(stage)) throw new ArgumentException("message", nameof(stage));

            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            if (!this.Configurations.TryGetValue(stage, out var configurations))
                this.Configurations[stage] = configurations = new List<StageOwinConfiguration>();

            configurations.Add(new StageOwinConfiguration(stage, configuration));
        }

        public void Order(string firstStage, string nextStage)
        {
            if (string.IsNullOrEmpty(firstStage)) throw new ArgumentException("message", nameof(firstStage));

            if (string.IsNullOrEmpty(nextStage)) throw new ArgumentException("message", nameof(nextStage));

            if (!this.Orders.TryGetValue(nextStage, out var firstStages))
                this.Orders[nextStage] = firstStages = new HashSet<string>();

            firstStages.Add(firstStage);
        }
    }

    class StageOwinConfiguration
    {
        public StageOwinConfiguration(string stage, OwinConfiguration configuration)
        {
            this.Stage = stage;
            this.Configuration = configuration;
        }

        public string Stage { get; }
        public OwinConfiguration Configuration { get; }
    }
}