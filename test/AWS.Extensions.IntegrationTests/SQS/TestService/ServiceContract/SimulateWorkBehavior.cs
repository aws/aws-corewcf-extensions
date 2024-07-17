using System.Collections.ObjectModel;
using AWS.CoreWCF.Extensions.SQS.Channels;
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;

namespace AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract
{
    public class SimulateWorkMessageInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            // simulate auth, routing, etc work being done
            Thread.Sleep(TimeSpan.FromMilliseconds(25));

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState) { }
    }

    /// <summary>
    /// Simulates auth, routing, etc work that would be done in a real-world system.  Importantly,
    /// this work is done on the Message Pump before the Message is dispatched onto a new thread
    /// by the Service Dispatch pipeline.
    /// <para />
    /// Using this <see cref="IServiceBehavior"/> allows testing of the impact of setting
    /// <see cref="AwsSqsTransport.ConcurrencyLevel"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SimulateWorkBehavior : Attribute, IServiceBehavior
    {
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            var endpoints = serviceHostBase
                .ChannelDispatchers.OfType<ChannelDispatcher>()
                .SelectMany(dispatcher => dispatcher.Endpoints);

            foreach (var endpointDispatcher in endpoints)
            {
                endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SimulateWorkMessageInspector());
            }
        }

        #region Unimplemented IServiceBevahior methods

        void IServiceBehavior.Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }

        void IServiceBehavior.AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters
        ) { }

        #endregion
    }
}
