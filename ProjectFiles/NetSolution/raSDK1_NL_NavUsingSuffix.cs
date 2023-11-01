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
using System.Collections.Generic;
using System.Linq;
using System.Data;
#endregion

public class raSDK1_NL_NavUsingSuffix : BaseNetLogic
{
    public override void Start()
    {
        //
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]

    public void TagAndSuffixLaunchBttn()
    {
        // Get button object
        var lButton = Owner.Owner.GetObject(this.Owner.BrowseName);
        // Get Alias from button
        //Alias launchAlias = (Alias)lButton.Children.Get("Inp_Tag1");
        UAVariable launchAlias = (UAManagedCore.UAVariable)lButton.Children.Get("Ref_BaseTag");
        // Get logix Tag from passed alias NodeId
        var tagNodeId = InformationModel.Get(launchAlias.Value);
        // Get Browse Path for the tag
        var launchAliasPath = GetOptixPathByNode(tagNodeId, "CommDrivers");
        // Add Suffix to the tag name
        launchAliasPath += lButton.Children.GetVariable("Cfg_Suffix").Value;
        // Get Dialog Box variable
        DialogType dBFromString = null;
        IUANode logixTag = null;
        var fpType = lButton.GetVariable("Cfg_DisplayType").Value;
        string faceplateTypeName = null;
        bool cfgCloseCurrent = lButton.GetVariable("Cfg_CloseCurrentDisplay").Value;

        try
        {
            // Get Logix Tag from tag+Suffix   
            logixTag = Project.Current.Get((string)launchAliasPath);
            // From Logix Tag assemble the name of Faceplate
            string infLib = (string)logixTag.Children.GetVariable("Inf_Lib").RemoteRead().Value;
            if (infLib.Contains("raM"))
            {
                string majorV = infLib.Replace('-', '_').Split('_')[1].Split('_')[0];
                string minorV = infLib.Replace('-', '_').Split('_')[2];
                faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + majorV + '_' + minorV + '_' + (string)fpType;
                //Log.Info("faceplate Type name with major minor:" + faceplateTypeName);
                //faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + "_v0_" + (string)fpType;
            }
            else
            {
                string majorV = infLib.Replace('-', '_').Split('_')[1].Split('_')[0];
                string minorV = infLib.Replace('-', '_').Split('_')[2];
                faceplateTypeName = (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + majorV + '_' + minorV + '_' + (string)fpType;
                //faceplateTypeName = infLib.Replace('-', '_') + '_' + (string)logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + '_' + (string)fpType;
            }
            //string faceplateTypeName = logixTag.Children.GetVariable("Inf_Type").RemoteRead().Value + "_v0_" + (string)fpType;
            // Find DialogBox from assembled Faceplate string
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
            UICommands.OpenDialog(lButton, dBFromString,launchAliasObj.NodeId);
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
    public string GetOptixPathByNode(IUANode inputNode, string topContainer)
    {
        List<string> pathToVar = new List<string>();

        FindBrowsePath(inputNode);
        if (pathToVar.Count > 0)
        {
            var launchAliasPath = ConstructBrowsePath();
            return launchAliasPath;
        }
        else
        {
            return null;
        }
        string ConstructBrowsePath()
        {
            string outStr = topContainer;
            for (long i = (pathToVar.LongCount() - 1); i >= 0; i--)
            {
                outStr = outStr + "/" + pathToVar[(int)i];
            }
            pathToVar = new List<string>();
            return outStr;
        }

        void FindBrowsePath(IUANode inputNode)
        {
            if (inputNode.Owner != null)
            {
                if (inputNode.BrowseName == topContainer)
                {
                    return;
                }
                pathToVar.Add(inputNode.BrowseName);
                FindBrowsePath(inputNode.Owner);
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
