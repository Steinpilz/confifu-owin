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
            StagesConfigurationBuilder stagesConfiguration = new StagesConfigurationBuilder();

            internal Config(IAppConfig appConfig)
            {
                this.appConfig = appConfig;
            }

            internal void InitDefaults()
            {
                stagesConfiguration.AddConfiguration("default", x => configuration(x));

                this.appConfig.SetOwinConfiguration(stagesConfiguration.Build);
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
                for(var i = 0; i < stages.Length-1; i++)
                {
                    OrderStages(stages[i], stages[i + 1]);
                }
                return this;
            }

            public Config OrderStages(string firstStage, string nextStage)
            {
                this.stagesConfiguration.Order(firstStage, nextStage);
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

    class StagesConfigurationBuilder
    {
        public Dictionary<string, List<StageOwinConfiguration>> Configurations { get; }
            = new Dictionary<string, List<StageOwinConfiguration>>();
        public Dictionary<string, HashSet<string>> Orders { get; }
            = new Dictionary<string, HashSet<string>>();

        public OwinConfiguration Build()
        {
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> visiting = new HashSet<string>();

            var orderedStages = new List<StageOwinConfiguration>();

            IEnumerable<string> dependentStages(string stage)
                => Orders.TryGetValue(stage, out HashSet<string> stages) ? stages : Enumerable.Empty<string>();

            void visitStage(string stage)
            {
                if (visiting.Contains(stage))
                    throw new InvalidOperationException($"Circular dependency detected for stage {stage}");

                if (visited.Contains(stage))
                    return;
                
                visited.Add(stage);
                visiting.Add(stage);
                
                foreach (var dependentStage in dependentStages(stage))
                {
                    visitStage(dependentStage);
                }

                orderedStages.AddRange(Configurations[stage]);

                visiting.Remove(stage);
            }

            void topSort()
            {
                foreach (var stage in Configurations.Keys)
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
            if (string.IsNullOrEmpty(stage))
            {
                throw new ArgumentException("message", nameof(stage));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!Configurations.TryGetValue(stage, out List<StageOwinConfiguration> configurations))
                Configurations[stage] = configurations = new List<StageOwinConfiguration>();

            configurations.Add(new StageOwinConfiguration(stage, configuration));
        }

        public void Order(string firstStage, string nextStage)
        {
            if (string.IsNullOrEmpty(firstStage))
            {
                throw new ArgumentException("message", nameof(firstStage));
            }

            if (string.IsNullOrEmpty(nextStage))
            {
                throw new ArgumentException("message", nameof(nextStage));
            }

            if (!Orders.TryGetValue(nextStage, out HashSet<string> firstStages))
                Orders[nextStage] = firstStages = new HashSet<string>();

            firstStages.Add(firstStage);
        }
    }

    class StageOwinConfiguration
    {
        public string Stage { get; }
        public OwinConfiguration Configuration { get; }

        public StageOwinConfiguration(string stage, OwinConfiguration configuration)
        {
            Stage = stage;
            Configuration = configuration;
        }
    }

    class StageOrder
    {
        public string FirstStage { get; }
        public string NextStage { get; }

        public StageOrder(string firstStage, string nextStage)
        {
            FirstStage = firstStage;
            NextStage = nextStage;
        }
    }
}
