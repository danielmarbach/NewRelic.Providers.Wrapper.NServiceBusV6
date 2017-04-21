using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
            agentWrapperApi.CreateMessageBrokerTransaction(MessageBrokerDestinationType.Queue, "NServiceBus", queueName);
            var segment = agentWrapperApi.StartMessageBrokerSegment(instrumentedMethodCall.MethodCall, MessageBrokerDestinationType.Queue, MessageBrokerAction.Peek, "NServiceBus", queueName);
            agentWrapperApi.ProcessInboundRequest(headers);

            return Delegates.GetDelegateFor<Task>(null, task =>
            {
                agentWrapperApi.RemoveSegmentFromCallStack(segment);
                if (task == null)
                    return;
                if (SynchronizationContext.Current != null)
                    task.ContinueWith(responseTask => agentWrapperApi.HandleExceptions(() =>
                    {
                        agentWrapperApi.EndSegment(segment);
                        agentWrapperApi.EndTransaction();
                    }), TaskScheduler.FromCurrentSynchronizationContext());
                else
                    task.ContinueWith(responseTask => agentWrapperApi.HandleExceptions(() =>
                    {
                        agentWrapperApi.EndSegment(segment);
                        agentWrapperApi.EndTransaction();
                    }), TaskContinuationOptions.ExecuteSynchronously);
            }, ex =>
            {
                if (ex != null)
                    agentWrapperApi.NoticeError(ex);
                agentWrapperApi.EndSegment(segment);
            });
        }

        private static string TryGetQueueName(LogicalMessage logicalMessage)
        {
            return logicalMessage.MessageType.FullName;
        }
    }
}
