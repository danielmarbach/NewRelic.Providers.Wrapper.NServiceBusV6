using System;
using System.Collections.Generic;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.SystemExtensions;
using NServiceBus.Pipeline;

namespace NewRelic.Providers.Wrapper.NServiceBusV6
{
    public class ReceiveMessageWrapper : IWrapper
    {

        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(
                MethodExtensions.MatchesAny((Method) methodInfo.Method, "NServiceBus.Core",
                    "NServiceBus.LoadHandlersConnector", "Invoke",
                    "NServiceBus.Pipeline.IIncomingLogicalMessageContext,System.Func`2[NServiceBus.Pipeline.IInvokeHandlerContext,System.Threading.Tasks.Task]"),
                (string) null);
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall,
            IAgentWrapperApi agentWrapperApi)
        {
            var context = instrumentedMethodCall.MethodCall.MethodArguments
                .ExtractNotNullAs<IIncomingLogicalMessageContext>(0);
            var incomingLogicalMessage = context.Message;
            if (incomingLogicalMessage == null)
                throw new NullReferenceException("logicalMessage");
            Dictionary<string, string> headers = context.Headers;
            if (headers == null)
                throw new NullReferenceException("headers");
            string queueName = TryGetQueueName(incomingLogicalMessage);
            agentWrapperApi.CreateMessageBrokerTransaction(0, "NServiceBus", queueName);
            ISegment isegment = agentWrapperApi.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, 0,
                (MessageBrokerAction) 1, "NServiceBus", queueName);
            agentWrapperApi.ProcessInboundRequest(headers,
                new int?());

            return Delegates.GetDelegateFor(() =>
            {
                agentWrapperApi.EndSegment(isegment);
                agentWrapperApi.EndTransaction();
            }, null, agentWrapperApi.NoticeError);
        }

        private static string TryGetQueueName(LogicalMessage logicalMessage)
        {
            return logicalMessage.MessageType.FullName;
        }
    }
}
