using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace MaxIfConditionCountAnalyzer
{
    public class MaxIFConditionsInWorkflow : IRegisterAnalyzerConfiguration
    {
        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            if (!workflowAnalyzerConfigService.HasFeature(UiPath.Studio.Activities.Api.DesignFeatureKeys.WorkflowAnalyzerV4))
                return;

            var maxIFRule = new Rule<IWorkflowModel>("Maximum IF conditions in a Workflow", "ST-AIF-001", InspectionIFCount);
            maxIFRule.DefaultErrorLevel = System.Diagnostics.TraceLevel.Warning;

            maxIFRule.Parameters.Add("MaxIFCountInWorkflow", new Parameter()
            {
                DefaultValue = "3",
                Key = "MaxIFCountInWorkflow",
                LocalizedDisplayName = "Maximum IF conditions in a workflow"
            });

            workflowAnalyzerConfigService.AddRule<IWorkflowModel>(maxIFRule);
        }

        private InspectionResult InspectionIFCount(IWorkflowModel workflowModel, Rule configRule)
        {
            string thresholdValue = configRule.Parameters["MaxIFCountInWorkflow"].Value;
            if (string.IsNullOrEmpty(thresholdValue))
            {
                return new InspectionResult { HasErrors = false };
            }

            if (workflowModel.Root == null)
            {
                return new InspectionResult { HasErrors = false };
            }

            IActivityModel workflowActivities = workflowModel.Root;
            int numberOfIFCondition = IFConditionCount(workflowActivities, 0);

            if(numberOfIFCondition > Convert.ToInt32(thresholdValue))
            {
                var errorMessage = new List<InspectionMessage>();
                errorMessage.Add(new InspectionMessage()
                {
                    Message = $"IF conditions used many times. Current allowed threshold is {thresholdValue}. Code contains {numberOfIFCondition}"
                });


                return new InspectionResult
                {
                    HasErrors = true,
                    InspectionMessages = errorMessage,
                    ErrorLevel = configRule.ErrorLevel,
                    RecommendationMessage = "Avoid using multiple IF or nested IF conditions in a single Xaml. Try using Switch Case or split the workflow"

                };
            }
            else
            {
                return new InspectionResult { HasErrors = false };
            }
        }

        /// <summary>
        /// Find and return number of IF conditions present in the workflow
        /// </summary>
        /// <param name="container"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private int IFConditionCount(IActivityModel container, int count)
        {
            System.Diagnostics.Debug.WriteLine(container.Id + " " + container.DisplayName);
            Console.WriteLine(container.Id + " " + container.DisplayName);

            if (container.Children.Count() == 0)
                return count;

            foreach (var item in container.Children)
            {
                if (item.Type.Contains("System.Activities.Statements.If"))
                {
                    count++;
                }
                //recrusive call to get all the child elements
                count = IFConditionCount(item, count);
            }

            return count;
        }
    }
}
