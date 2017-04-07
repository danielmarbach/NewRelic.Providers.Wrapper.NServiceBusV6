using NewRelic.Agent.Extensions.Providers.Wrapper;
using NewRelic.SystemExtensions;
using NServiceBus.Pipeline;

namespace NewRelic.Providers.Wrapper.NServiceBusV6
{
    public class SendMessageWrapper : IWrapper
    {
        public CanWrapResponse CanWrap(InstrumentedMethodInfo methodInfo)
        {
            return new CanWrapResponse(methodInfo.Method.MatchesAny("NServiceBus.Core", "NServiceBus.Pipeline`1[NServiceBus.Pipeline.IOutgoingSendContext]", "Invoke", "NServiceBus.Pipeline.IOutgoingSendContext"));
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgentWrapperApi agentWrapperApi)
        {
            var context = instrumentedMethodCall.MethodCall.MethodArguments.ExtractNotNullAs<IOutgoingSendContext>(0);
            AttachCatHeaders(agentWrapperApi, context);
            var queueName = TryGetQueueName(context.Message);
            var segment = agentWrapperApi.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, MessageBrokerDestinationType.Queue, MessageBrokerAction.Produce, "NServiceBus", queueName);
            return Delegates.GetDelegateFor(() => agentWrapperApi.EndSegment(segment));
        }

        private static void AttachCatHeaders(IAgentWrapperApi agentWrapperApi, IOutgoingContext context)
        {
            var outboundRequestHeaders = agentWrapperApi.GetOutboundRequestHeaders();
            foreach (var header in outboundRequestHeaders)
            {
                if (header.Value != null && header.Key != null)
                {
                    context.Headers[header.Key] = header.Value;
                }
            }
        }

        private static string TryGetQueueName(OutgoingLogicalMessage logicalMessage)
        {
            return logicalMessage.MessageType.FullName;
        }
    }
}