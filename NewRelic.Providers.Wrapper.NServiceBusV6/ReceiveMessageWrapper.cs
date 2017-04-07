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
                methodInfo.Method.MatchesAny("NServiceBus.Core",
                    "NServiceBus.LoadHandlersConnector", "Invoke",
                    "NServiceBus.Pipeline.IIncomingLogicalMessageContext,System.Func`2[NServiceBus.Pipeline.IInvokeHandlerContext,System.Threading.Tasks.Task]"));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall,
            IAgentWrapperApi agentWrapperApi)
        {
            var context = instrumentedMethodCall.MethodCall.MethodArguments
                .ExtractNotNullAs<IIncomingLogicalMessageContext>(0);
            var incomingLogicalMessage = context.Message;
            if (incomingLogicalMessage == null)
                throw new NullReferenceException("logicalMessage");
            var headers = context.Headers;
            if (headers == null)
                throw new NullReferenceException("headers");
            var queueName = TryGetQueueName(incomingLogicalMessage);
            agentWrapperApi.CreateMessageBrokerTransaction(0, "NServiceBus", queueName);
            var isegment = agentWrapperApi.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, 0,
                (MessageBrokerAction) 1, "NServiceBus", queueName);
            agentWrapperApi.ProcessInboundRequest(headers);
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
