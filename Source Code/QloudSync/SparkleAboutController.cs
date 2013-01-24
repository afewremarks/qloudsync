//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Net;
using System.Threading;

 

namespace QloudSync {

    public class SparkleAboutController {

        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public event Action VersionUpToDateEvent = delegate { };
        public event Action CheckingForNewVersionEvent = delegate { };

        public event NewVersionEventDelegate NewVersionEvent = delegate { };
        public delegate void NewVersionEventDelegate (string new_version_string);

        public readonly string WebsiteLinkAddress       = "http://www.sparkleshare.org/";
        public readonly string CreditsLinkAddress       = "http://www.github.com/hbons/SparkleShare/tree/master/legal/AUTHORS";
        public readonly string ReportProblemLinkAddress = "http://www.github.com/hbons/SparkleShare/issues";
        public readonly string DebugLogLinkAddress      = "file://";



        public SparkleAboutController ()
        {
            Program.Controller.ShowAboutWindowEvent += delegate {
                ShowWindowEvent ();
            };
        }


        public void WindowClosed ()
        {
            HideWindowEvent ();
        }

       


        private bool UpdateRequired (string running_version_string, string latest_version_string)
        {
            if (running_version_string == null)
                throw new ArgumentNullException ("running_version_string");

            if (string.IsNullOrWhiteSpace (running_version_string))
                throw new ArgumentException ("running_version_string");

            if (latest_version_string == null)
                throw new ArgumentNullException ("latest_version_string");

            if (string.IsNullOrWhiteSpace (latest_version_string))
                throw new ArgumentException ("latest_version_string");

            int running_major;
            int running_minor;
            int running_micro;
            try {
                string [] running_split = running_version_string.Split ('.');
                running_major = int.Parse (running_split [0]);
                running_minor = int.Parse (running_split [1]);
                running_micro = int.Parse (running_split [2]);

            } catch (Exception e) {
                throw new FormatException ("running_version_string", e);
            }

            int latest_major;
            int latest_minor;
            int latest_micro;
            try {
                string [] latest_split = latest_version_string.Split ('.');
                latest_major = int.Parse (latest_split [0]);
                latest_minor = int.Parse (latest_split [1]);
                latest_micro = int.Parse (latest_split [2]);

            } catch (Exception e) {
                throw new FormatException ("latest_version_string", e);
            }

            bool higher_major = latest_major > running_major;
            bool higher_minor = latest_major == running_major && latest_minor > running_minor;
            bool higher_micro = latest_major == running_major && latest_minor == running_minor && latest_micro > running_micro;

            return (higher_major || higher_minor || higher_micro);
        }
    }
}
