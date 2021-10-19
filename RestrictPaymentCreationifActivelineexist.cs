using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace SoftchiefPlugins
{
    public class RestrictPaymentCreationifActivelineexist : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Obtain the target entity from the input parameters.  
            Entity entityPayment = (Entity)context.InputParameters["Target"];
           
            if (entityPayment.LogicalName != "cr2e3_studentpayment")
                return;

            //your code goes here
            var customerID = entityPayment.GetAttributeValue<EntityReference>("cr2e3_customer").Id;
         
            var fetchXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='cr2e3_studentpaymentline'>
                                <attribute name='cr2e3_studentpaymentlineid' />
                                <attribute name='cr2e3_name' />
                                <attribute name='createdon' />
                                <order attribute='cr2e3_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                                <link-entity name='cr2e3_studentpayment' from='cr2e3_studentpaymentid' to='cr2e3_payment' link-type='inner' alias='ac'>
                                  <filter type='and'>
                                    <condition attribute='cr2e3_customer' operator='eq' uitype='contact' value='{0}' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";

            fetchXML = string.Format(fetchXML, customerID);

            EntityCollection ecPLines =  service.RetrieveMultiple(new FetchExpression(fetchXML));


            if (ecPLines.Entities.Count>0)
            {
                throw new InvalidPluginExecutionException("There are already active payment lines exist whicyh are due. SO you cannot create new payments.");
            }
        }
    }
}
