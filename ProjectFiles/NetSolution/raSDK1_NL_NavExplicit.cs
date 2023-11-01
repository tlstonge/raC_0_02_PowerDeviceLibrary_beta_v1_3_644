#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.UI;
using FTOptix.System;
using FTOptix.RAEtherNetIP;
using FTOptix.NetLogic;
using FTOptix.CommunicationDriver;
using FTOptix.CoreBase;
using FTOptix.SerialPort;
using FTOptix.Core;
#endregion

public class raSDK1_NL_NavExplicit : BaseNetLogic
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

    public void TagNativeLaunchButton()
    {
        // Get button object
        var lButton = Owner.Owner.GetObject(this.Owner.BrowseName);
        // Make Launch Object that will contain aliases
        var launchAliasObj = InformationModel.MakeObject("LaunchAliasObj");
        // Get each alias from Launch Button and add them into Launch Object, and assign NodeId values 
        IUANode dBNodeId = null;
        foreach (var inpTag in lButton.Children)
        {
            if (inpTag.BrowseName.Contains("Ref_"))  // & !inpTag.BrowseName.Contains("Ref_DialogBox") & (inpTag.GetType() == typeof(UAVariable)))
            {
                // Make a variable with same name as alias of type NodeId
                var newVar = InformationModel.MakeVariable(inpTag.BrowseName, OpcUa.DataTypes.NodeId);
                // Assign alias value to new variable
                newVar.Value = ((UAManagedCore.UAVariable)inpTag).Value;
                // Add variable int launch object
                launchAliasObj.Add(newVar);
            }
            else if (inpTag.BrowseName.Contains("Cfg_DialogBox"))
            {
                dBNodeId = inpTag;
            }
        }
        bool cfgCloseCurrent = lButton.GetVariable("Cfg_CloseCurrentDisplay").Value;
        DialogType commonDb = null;
        try
        {
            // Get common dialog box node id
            commonDb = (DialogType)InformationModel.Get(((UAVariable)dBNodeId).Value);
            // Launch DialogBox passing Launch Object that contains aliases as an alias 
            UICommands.OpenDialog(lButton, commonDb, launchAliasObj.NodeId);
            // if configured to close dialog box containing launch button
            if (cfgCloseCurrent)
            {
                CloseCurrentDB(Owner);
            }
        }
        catch
        {
            if (commonDb == null)
            {
                Log.Error("Faceplate Launch Button", "Common Dialog Box not found");
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
