using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace SoftchiefPlugins
{
    public class RestrictTermChange : IPlugin
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

            Entity PreImage = (Entity)context.PreEntityImages["Image"];
            Entity PostImage = (Entity)context.PostEntityImages["Image"];

            var preTerm = PreImage.GetAttributeValue<int>("cr2e3_term");
            var postTerm = PostImage.GetAttributeValue<int>("cr2e3_term");

            var fees = PreImage.GetAttributeValue<Money>("cr2e3_fees").Value;

            if (postTerm>= preTerm)
            {
                throw new InvalidPluginExecutionException("You cannot change to a higher term. Please use lower term.");

            }
            else
            {
                            
                var paymentid = entityPayment.Id;

                //Delete records
                QueryExpression qe = new QueryExpression();
                qe.EntityName = "cr2e3_studentpaymentline";
                qe.ColumnSet = new ColumnSet("cr2e3_studentpaymentlineid", "cr2e3_name");
                EntityReference entPaymentLookup = new EntityReference("cr2e3_studentpayment", paymentid);                  
                qe.Criteria.AddCondition(new ConditionExpression("cr2e3_payment",ConditionOperator.Equal,paymentid));
                
                EntityCollection ec =  service.RetrieveMultiple(qe);
                
                for (int i = 0; i < ec.Entities.Count; i++)
                {
                    service.Delete("cr2e3_studentpaymentline", ec.Entities[i].Id);
                }
       
                //create new payment line as per the new term.
                for (int i = 0; i < postTerm; i++)
                {
                    // create payment lines
                    Entity entPLines = new Entity();
                    entPLines.LogicalName = "cr2e3_studentpaymentline";
                    entPLines["cr2e3_name"] = "new line " + i;
                    entPLines["cr2e3_amount"] = new Money(fees / postTerm);
                    entPLines["cr2e3_installmentnumber"] = i + 1;
                    entPLines["cr2e3_payment"] = entPaymentLookup;
                    service.Create(entPLines);
                }
              
            }

        }
    }
}
