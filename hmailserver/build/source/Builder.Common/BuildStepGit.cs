// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Builder.Common
{
    class BuildStepGit : BuildStep
    {
        private string m_sDirectory;
        private GITAction m_eAction;

        public enum GITAction
        {
            RevertLocalChanges = 1,
            Pull = 2
        }


        public BuildStepGit(Builder oBuilder, GITAction a, string sDirectory)
        {
            m_oBuilder = oBuilder;

            m_eAction = a;
            m_sDirectory = sDirectory;
        }


        public override string Name
        {
            get
            {
                switch (m_eAction)
                {
                    case GITAction.RevertLocalChanges:
                        return "Git Reset " + m_sDirectory;
                    case GITAction.Pull:
                        return "Git Pull " + m_sDirectory;
                }

                return "<Unknown>";
            }
        }

        public override void Run()
        {
            string sDirectory = ExpandMacros(m_sDirectory);

            string sCommand = "";
            switch (m_eAction)
            {
                case GITAction.RevertLocalChanges:
                  m_oBuilder.Log("Resetting " + sDirectory + "...\r\n", true);
                    sCommand = "checkout .";
                    break;
                case GITAction.Pull:
                    m_oBuilder.Log("Pulling " + sDirectory + "...\r\n", true);
                    sCommand = "pull";
                    break;
                default:
                    throw new Exception("Failed. Unknown action.");
            }

            ProcessLauncher launcher = new ProcessLauncher();
            launcher.Output += new ProcessLauncher.OutputDelegate(launcher_Output);
            string output;

            int exitCode = launcher.LaunchProcess(ExpandMacros(m_oBuilder.ParameterGitPath), ExpandMacros(sCommand),sDirectory, out output);

            if (exitCode != 0)
                throw new Exception(string.Format("{0} failed. Exit code: {1}. Output: {2}", sCommand, exitCode, output));
        }

        void launcher_Output(string output)
        {
           m_oBuilder.Log(output, true);
        }

    }
}
