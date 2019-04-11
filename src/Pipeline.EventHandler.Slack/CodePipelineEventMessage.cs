using System;

namespace JasonWadsworth.Pipeline.EventHandler.Slack
{
    public class CodePipelineEventMessage
    {
        public string Version { get; set; }

        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("detail-type")]
        public string DetailType { get; set; }

        public string Account { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Region { get; set; }

        public MessageDetail Detail { get; set; }

        public class MessageDetail
        {
            public string Pipeline { get; set; }

            public string Stage { get; set; }

            public string Action { get; set; }

            public string State { get; set; }

            public string Region { get; set; }
        }
    }
}