#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.OPCUAServer;
using FTOptix.WebUI;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

public class raSDK1_NL_NavUsingTag : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]

    public void TagNodeIdLaunchButton()
    {
        // Get button object
        var lButton = Owner.Owner.GetObject(this.Owner.BrowseName);
        // Make Launch Object that will contain aliases
        var launchAliasObj = InformationModel.MakeObject("LaunchAliasObj");
        // Get each alias from Launch Button and add them into Launch Object, and assign NodeId values 
        foreach (var inpTag in lButton.Children)
        {
            if (inpTag.BrowseName.Contains("Ref_"))
            {
                // Make a variable with same name as alias of type NodeId
                var newVar = InformationModel.MakeVariable(inpTag.BrowseName, OpcUa.DataTypes.NodeId);
                // Assign alias value to new variable
                newVar.Value = ((UAManagedCore.UAVariable)inpTag).Value;
                // Add variable int launch object
                launchAliasObj.Add(newVar);
            }
        }
        string fpType = lButton.GetVariable("Cfg_DisplayType").Value;
        bool cfgCloseCurrent = lButton.GetVariable("Cfg_CloseCurrentDisplay").Value;
        DialogType dBFromString = null;
        IUANode logixTag = null;
        string faceplateTypeName = null;
        try
        {
            // Get Logix Tag with all its local tags and extended properties from first stored variable into launch object
            logixTag = InformationModel.Get(((UAVariable)launchAliasObj.GetVariable("Ref_Tag")).Value);
            // From Logix Tag assemble the name of Faceplate
            string infLib = ((string)logixTag.Children.GetVariable("Inf_Lib").RemoteRead().Value).Replace('-', '_');
            string infType = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value;
            faceplateTypeName = infLib + '_' + infType + '_' + fpType;
            
            // Find DialogBox from assembled Faceplate string
            var foundFp = Project.Current.Find(faceplateTypeName);
            // if found is DialogType, than it is a faceplate type
            if (foundFp.GetType() == typeof(DialogType))
            {
                dBFromString = (DialogType)foundFp;
            }
            else // found current instance of faceplate
            {
                // Get faceplate type from instance
                System.Reflection.PropertyInfo objType = foundFp.GetType().GetProperty("ObjectType");
                dBFromString = (DialogType)(objType.GetValue(foundFp, null));
            }
            // Launch DialogBox passing Launch Object that contains aliases as an alias 
            UICommands.OpenDialog(lButton, dBFromString, launchAliasObj.NodeId);
            // if configured to close dialog box containing launch button
            if (cfgCloseCurrent) 
            {
                CloseCurrentDB(Owner);
            }
        }
        catch
        {
            if (logixTag == null)
            {
                Log.Error("Faceplate Launch Button", "Tag Path not found");
            }
            else if (dBFromString == null)
            {
                Log.Error("Faceplate Launch Button", "Faceplate not found");
            }
        }
    }

    public void CloseCurrentDB(IUANode inputNode)
    {
        // if input node is of type Dialog, close it
        if (inputNode.GetType().BaseType.BaseType == typeof(Dialog))
        {
            // close dialog box
            ((Dialog)inputNode).Close();
            return;
        }
        // if input node is Main Window, no dialog box was found, return
        if (inputNode.GetType() == typeof(MainWindow))
        {
            return;
        }
        // continue search for Dialog or Main Window
        CloseCurrentDB(inputNode.Owner);
    }
}
