using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Formatting.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace JasonWadsworth.Pipeline.EventHandler.Slack
{
    public class Function
    {
        private readonly Microsoft.Extensions.Logging.ILogger logger;

        private readonly string webhookUrl;

        private readonly IEnumerable<string> tags;

        private readonly HttpClient httpClient = new HttpClient();

        public Function()
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            webhookUrl = configuration["Pipeline:EventHandler:Slack:WebhookUrl"];
            var tagList = configuration["Pipeline:EventHandler:Slack:TagList"];
            if (!string.IsNullOrEmpty(tagList))
            {
                tags = tagList.Split(",");
            }
            else
                tags = new string[0];

            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var loggerConfig = new Serilog.LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "CodePipeline.EventHandler.Slack")
                .WriteTo.Console(new JsonFormatter());

            Serilog.Log.Logger = loggerConfig.CreateLogger();
            loggerFactory.AddSerilog();

            logger = serviceProvider.GetRequiredService<ILogger<Function>>();
        }


        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(CodePipelineEventMessage input, ILambdaContext context)
        {
            var pipelineUrl = $"https://{input.Region}.console.aws.amazon.com/codesuite/codepipeline/pipelines/{input.Detail.Pipeline}/view";

            var message = new
            {
                Blocks = new object[]
                {
                    new { Type = "divider" },
                    new { Type = "section", Text = new { Type = "mrkdwn", Text = $"*{input.Detail.Pipeline} Pipeline Update in {input.Account}*\n\n<{pipelineUrl}|Click for Details>" } },
                    new { Type = "section", Text = new { Type = "mrkdwn", Text = string.Join(' ', tags.Select(t => $"<{t}>")) } },
                    new { Type = "section", Fields = new object[]
                    {
                        new { Type = "mrkdwn", Text = $"*State*\n{input.Detail.State}" },
                        new { Type = "mrkdwn", Text = $"*Stage*\n{input.Detail.Stage}" },
                        new { Type = "mrkdwn", Text = $"*Action*\n{input.Detail.Action}" },
                        new { Type = "mrkdwn", Text = $"*Region*\n{input.Detail.Region}" },
                        new { Type = "mrkdwn", Text = $"*Time*\n{input.Time}" },
                    } },
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
            });

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                // TODO: add some retry logic
                using (var response = await httpClient.SendAsync(requestMessage))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogError((int)response.StatusCode, "Failed to send message to Slack. StatusCode was {StatusCode}", response.StatusCode);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(500, e, "Failed to send message to Slack");
            }

            return input.Account;
        }
    }
}
