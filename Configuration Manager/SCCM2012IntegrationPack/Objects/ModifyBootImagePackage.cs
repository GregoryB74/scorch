using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SystemCenter.Orchestrator.Integration;
using System.Management;
using System.Management.Instrumentation;
using SCCM2012Interop;
using Microsoft.ConfigurationManagement;
using Microsoft.ConfigurationManagement.ManagementProvider;
using Microsoft.ConfigurationManagement.ManagementProvider.WqlQueryEngine;


namespace SCCM2012IntegrationPack
{
    [Activity("Modify SCCM Boot Image Package")]
    public class ModifyBootImagePackage : IActivity
    {
        private ConnectionCredentials settings;
        private String userName = String.Empty;
        private String password = String.Empty;
        private String SCCMServer = String.Empty;

        private int ObjCount = 0;

        [ActivityConfiguration]
        public ConnectionCredentials Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        public void Design(IActivityDesigner designer)
        {
            designer.AddInput("Package ID").WithDefaultValue("ABC00000");

            //Setup WQL Connection and WMI Management Scope
            WqlConnectionManager connection = CM2012Interop.connectSCCMServer(settings.SCCMSERVER, settings.UserName, settings.Password);

            String[] propertyNameChoices = CM2012Interop.getSCCMObjectPropertyNames(connection, "SMS_BootImagePackage");
            String[] propertyTypeChoices = new String[] { "StringValue", "DateTimeValue", "IntegerValue", "BooleanValue" };

            foreach (String propertyName in propertyNameChoices)
            {
                designer.AddInput(propertyName + " : Property Type").WithListBrowser(propertyTypeChoices).WithDefaultValue("StringValue").NotRequired();
                designer.AddInput(propertyName + " : Property Value").NotRequired();
            }

            designer.AddCorellatedData(typeof(bootImagePackage));
            designer.AddOutput("Number of Packages");
        }
        public void Execute(IActivityRequest request, IActivityResponse response)
        {
            SCCMServer = settings.SCCMSERVER;
            userName = settings.UserName;
            password = settings.Password;

            String objID = request.Inputs["Package ID"].AsString();
          
            //Setup WQL Connection and WMI Management Scope
            WqlConnectionManager connection = CM2012Interop.connectSCCMServer(SCCMServer, userName, password);
            using(connection)
            {  
                String[] propertyNameChoices = CM2012Interop.getSCCMObjectPropertyNames(connection, "SMS_BootImagePackage");
                foreach (String propertyName in propertyNameChoices)
                {
                    if ((request.Inputs.Contains(propertyName + " : Property Type")) && (request.Inputs.Contains(propertyName + " : Property Value")))
                    {
                        CM2012Interop.modifySCCMBootImagePackage(connection, objID, request.Inputs[(propertyName + " : Property Type")].AsString(), propertyName, request.Inputs[(propertyName + " : Property Value")].AsString());
                    }
                }

                IResultObject col = null;
                col = CM2012Interop.getSCCMBootImagePackage(connection, "PackageID LIKE '" + objID + "'");

                if (col != null)
                {
                    response.WithFiltering().PublishRange(getObjects(col));

                }
                response.Publish("Number of Packages", ObjCount);
            }
        }
        private IEnumerable<bootImagePackage> getObjects(IResultObject objList)
        {
            foreach (IResultObject obj in objList)
            {
                ObjCount++;
                yield return new bootImagePackage(obj);
            }
        }
    }
}

