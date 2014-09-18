﻿using System;
using System.Collections.Generic;
using System.Linq;
using ExactTarget.TriggeredEmail.Core;
using ExactTarget.TriggeredEmail.Core.Configuration;
using ExactTarget.TriggeredEmail.ExactTargetApi;
using Attribute = ExactTarget.TriggeredEmail.ExactTargetApi.Attribute;

namespace ExactTarget.TriggeredEmail.Trigger
{
    public enum RequestQueueing
    {
        No = 0,
        Yes,
    }

    public enum Priority
    {
        Normal = 0,
        High
    }

    public class EmailTrigger : IEmailTrigger
    {
        private readonly IExactTargetConfiguration _config;

        public EmailTrigger(IExactTargetConfiguration config)
        {
            _config = config;
        }

        public void Trigger(ExactTargetTriggeredEmail exactTargetTriggeredEmail, RequestQueueing requestQueueing = RequestQueueing.No, Priority priority = Priority.Normal)
        {
            var clientId = _config.ClientId;
            var client =  SoapClientFactory.Manufacture(_config);

            var subscribers = new List<Subscriber>
                {
                    new Subscriber
                        {
                            EmailAddress = exactTargetTriggeredEmail.EmailAddress,
                            SubscriberKey = exactTargetTriggeredEmail.SubscriberKey ?? exactTargetTriggeredEmail.EmailAddress,
                            Attributes =
                                exactTargetTriggeredEmail.ReplacementValues.Select(value => new Attribute
                                    {
                                        Name = value.Key,
                                        Value = value.Value
                                    }).ToArray()
                        }
                };

            var tsd = new TriggeredSendDefinition
            {
                Client = clientId.HasValue ?  new ClientID { ID = clientId.Value, IDSpecified = true } : null,
                CustomerKey = exactTargetTriggeredEmail.ExternalKey
            };

            var ts = new TriggeredSend
            {
                Client = clientId.HasValue ? new ClientID { ID = clientId.Value, IDSpecified = true } : null,
                TriggeredSendDefinition = tsd,
                Subscribers = subscribers.ToArray()
            };

            var co = new CreateOptions
            {
                RequestType = requestQueueing == RequestQueueing.No ? RequestType.Synchronous : RequestType.Asynchronous,
                RequestTypeSpecified = true,
                QueuePriority = priority == Priority.High ? ExactTargetApi.Priority.High : ExactTargetApi.Priority.Medium,
                QueuePrioritySpecified = true
            };

            string requestId, status;
            var result = client.Create(
                co,
                new APIObject[] { ts },
                out requestId, out status);

            ExactTargetResultChecker.CheckResult(result.FirstOrDefault()); //we expect only one result because we've sent only one APIObject
        }
    }
}