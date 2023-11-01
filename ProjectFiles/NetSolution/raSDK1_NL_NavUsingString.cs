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
#endregion

public class raSDK1_NL_NavUsingString : BaseNetLogic
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

    public void TagPathLinkLaunchBttn()
    {
        // Get button object
        var lButton = Owner.Owner.GetObject(this.Owner.BrowseName);
        // Get the NodeId of logix member tag that holds navigation path string
        var linkMember = InformationModel.Get(lButton.GetVariable("Ref_Nav").Value);
        // Get CommDrivers folder in Optix
        var commDrivers = Project.Current.Find("CommDrivers");
        // Get the path string
        string lpath = ((UAManagedCore.UAVariable)linkMember).Value;
        string controllerShortcut = null;
        string tag = null;
        string container = null;
        IUANode logixTag = null;
        if (lpath.Contains("["))
        {
            controllerShortcut = lpath.Split(']')[0].Split('[')[1];
            if (lpath.Contains('.'))
            {
                tag = lpath.Split(']')[1].Split('.')[1];
                container = lpath.Split(']')[1].Split('.')[0];
            }
            else
            {
                tag = lpath.Split(']')[1];
                container = lpath.Split(']')[1];
                if (!lpath.Contains("Program:"))
                {
                    container = "Controller Tags";
                }
            }
            logixTag = commDrivers.Get("RAEtherNet_IPDriver1").Get(controllerShortcut).Get("Tags").Get(container).Get(tag);
        }
        else
        {
            logixTag = Project.Current.Get(lpath);
        }
        var fpType = lButton.GetVariable("Cfg_DisplayType").Value;

        DialogType dBFromString = null;
        string faceplateTypeName = null;
        bool cfgCloseCurrent = lButton.GetVariable("Cfg_CloseCurrentDisplay").Value;

        try
        {
            // From tag get DialogBox name to navigate to
            string infLib = (string)logixTag.Children.GetVariable("Inf_Lib").RemoteRead().Value;
            if (infLib == "raM")
            {
                string majorV = infLib.Replace('-', '_').Split('_')[1].Split('_')[0];
                string minorV = infLib.Replace('-', '_').Split('_')[2];
                faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + majorV + '_' + minorV + '_' + (string)fpType;

                //faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + "_v0_" + (string)fpType;
            }
            else
            {
                string majorV = infLib.Replace('-', '_').Split('_')[1].Split('_')[0];
                string minorV = infLib.Replace('-', '_').Split('_')[2];
                faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + majorV + '_' + minorV + '_' + (string)fpType;

                //faceplateTypeName = infLib.Replace('-', '_') + '_' + (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + (string)fpType;
            }
            // Find DialogBox from string
            dBFromString = (DialogType)Project.Current.Find(faceplateTypeName);
            // Make Launch Object that will contain aliases
            var launchAliasObj = InformationModel.MakeObject("LaunchAliasObj");
            // Make new variable "Inp_Tag1"
            var newVar = InformationModel.MakeVariable("Ref_Tag", OpcUa.DataTypes.NodeId);
            // Assign value of Logix Tag NodeId to new variable 
            newVar.Value = logixTag.NodeId;
            // Add new variable into the launch object
            launchAliasObj.Add(newVar);
            // Launch DialogBox passing Launch Object that contains two aliases as an alias 
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
