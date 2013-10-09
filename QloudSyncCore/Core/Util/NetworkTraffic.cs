﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace QloudSyncCore.Core.Util
{
    public class NetworkTraffic
    {
        private PerformanceCounter bytesSentPerformanceCounter;
        private PerformanceCounter bytesReceivedPerformanceCounter;
        private int pid;
        private bool countersInitialized;

        public NetworkTraffic(int processID)
        {
            pid = processID;
            Console.WriteLine(processID);
            TryToInitializeCounters();
        }

        private void TryToInitializeCounters()
        {
            if (!countersInitialized)
            {
                PerformanceCounterCategory category = new PerformanceCounterCategory(".NET CLR Networking 4.0.0.0");

                var instanceNames = category.GetInstanceNames().Where(i => i.Contains(string.Format("p{0}", pid)));

                if (instanceNames.Any())
                {
                    Console.WriteLine("Entrou aqui?"); 
                    bytesSentPerformanceCounter = new PerformanceCounter();
                    bytesSentPerformanceCounter.CategoryName = ".NET CLR Networking 4.0.0.0";
                    bytesSentPerformanceCounter.CounterName = "Bytes Sent";
                    bytesSentPerformanceCounter.InstanceName = instanceNames.First();
                    bytesSentPerformanceCounter.ReadOnly = true;

                    bytesReceivedPerformanceCounter = new PerformanceCounter();
                    bytesReceivedPerformanceCounter.CategoryName = ".NET CLR Networking 4.0.0.0";
                    bytesReceivedPerformanceCounter.CounterName = "Bytes Received";
                    bytesReceivedPerformanceCounter.InstanceName = instanceNames.First();
                    bytesReceivedPerformanceCounter.ReadOnly = true;

                    countersInitialized = true;
                }
            }
        }

        public float GetBytesSent()
        {
            float bytesSent = 0;

            try
            {
                TryToInitializeCounters();
                bytesSent = bytesSentPerformanceCounter.RawValue;
            }
            catch { }

            return bytesSent;
        }

        public float GetBytesReceived()
        {
            float bytesSent = 0;

            try
            {
                TryToInitializeCounters();
                bytesSent = bytesReceivedPerformanceCounter.RawValue;
            }
            catch { }

            return bytesSent;
        }
    }
}
