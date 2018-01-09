using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NLog.Targets;
using NLog;
using NLog.Targets.Wrappers;
using System.Xml.Linq;

namespace MetTerminal2
{
    public partial class FormChangeLog : Form
    {
        public FormChangeLog()
        {
            InitializeComponent();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Text (.txt)|*.txt|Log (.log)|*.log";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string SaveFilePath = sfd.FileName;
                    textBoxFilePath.Text = SaveFilePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxFilePath.Text))
            {
                if (LogManager.Configuration != null && LogManager.Configuration.ConfiguredNamedTargets.Count != 0)
                {
                    Target target = LogManager.Configuration.FindTargetByName("file");
                    if (target == null)
                    {
                        throw new Exception("Could not find target named: " + "file");
                    }

                    FileTarget fileTarget = null;
                    WrapperTargetBase wrapperTarget = target as WrapperTargetBase;

                    // Unwrap the target if necessary.
                    if (wrapperTarget == null)
                    {
                        fileTarget = target as FileTarget;
                    }
                    else
                    {
                        fileTarget = wrapperTarget.WrappedTarget as FileTarget;
                    }

                    if (fileTarget == null)
                    {
                        throw new Exception("Could not get a FileTarget from " + target.GetType());
                    }

                    fileTarget.FileName = textBoxFilePath.Text;
                    LogManager.ReconfigExistingLoggers();

                    var nlogConfigFile = "NLog.config";
                    var xdoc = XDocument.Load(nlogConfigFile);
                    var ns = xdoc.Root.GetDefaultNamespace();
                    var fTarget = xdoc.Descendants(ns + "target")
                             .FirstOrDefault(t => (string)t.Attribute("name") == "file");
                    fTarget.SetAttributeValue("fileName", textBoxFilePath.Text);
                    xdoc.Save(nlogConfigFile);

                    this.Close();
                }
            }
        }

        private void FormChangeLog_Load(object sender, EventArgs e)
        {
            if (LogManager.Configuration != null && LogManager.Configuration.ConfiguredNamedTargets.Count != 0)
            {
                Target target = LogManager.Configuration.FindTargetByName("file");

                FileTarget fileTarget = null;
                WrapperTargetBase wrapperTarget = target as WrapperTargetBase;

                // Unwrap the target if necessary.
                if (wrapperTarget == null)
                {
                    fileTarget = target as FileTarget;
                }
                else
                {
                    fileTarget = wrapperTarget.WrappedTarget as FileTarget;
                }

                textBoxFilePath.Text = fileTarget.FileName.ToString().Replace("\'", "");
            }
        }
    }
}
