//
// dkxce.WMIDeviceWatcher
// https://github.com/dkxce/WMIDeviceWatcher
//

using System;
using System.Collections.Generic;
using System.Management;

namespace dkxce
{
    public class WMIDeviceWatcher
    {        
        public enum EventType: byte
        {
            Unknowd = 0,
            DeviceUpdated = 1,
            DeviceAdded = 2,
            DeviceRemoved = 3,
            DeviceFixed = 4
        }

        private ManagementEventWatcher watcherUpd;
        private ManagementEventWatcher watcherAdd;
        private ManagementEventWatcher watcherRem;
        private MSolSvcService service = null;
        
        public WMIDeviceWatcher() { }
        public WMIDeviceWatcher(MSolSvcService service) { this.service = service; }
        ~WMIDeviceWatcher() { Stop(); }

        public void Start()
        {
            WqlEventQuery queryUpd = new WqlEventQuery() { EventClassName = "__InstanceModificationEvent", WithinInterval = new TimeSpan(0, 0, 3), Condition = "TargetInstance ISA 'Win32_PnPEntity'" };
            WqlEventQuery queryAdd = new WqlEventQuery() { EventClassName = "__InstanceCreationEvent", WithinInterval = new TimeSpan(0, 0, 3), Condition = "TargetInstance ISA 'Win32_PnPEntity'" };
            WqlEventQuery queryRem = new WqlEventQuery() { EventClassName = "__InstanceDeletionEvent", WithinInterval = new TimeSpan(0, 0, 3), Condition = "TargetInstance ISA 'Win32_PnPEntity'" };

            ManagementScope scope = new ManagementScope("root\\CIMV2");

            watcherUpd = new ManagementEventWatcher(scope, queryUpd);
            watcherUpd.EventArrived += DeviceUpdated;
            watcherUpd.Start();

            watcherAdd = new ManagementEventWatcher(scope, queryAdd);
            watcherAdd.EventArrived += DeviceAdded;
            watcherAdd.Start();

            watcherRem = new ManagementEventWatcher(scope, queryRem);
            watcherRem.EventArrived += DeviceRemoved;
            watcherRem.Start();

        }

        public void Stop()
        {
            if (watcherUpd != null) try { watcherUpd.Stop(); watcherUpd.Dispose(); watcherUpd = null; } catch { };
            if (watcherAdd != null) try { watcherAdd.Stop(); watcherAdd.Dispose(); watcherAdd = null; } catch { };
            if (watcherRem != null) try { watcherRem.Stop(); watcherRem.Dispose(); watcherRem = null; } catch { };
        }
        
        private void DeviceUpdated(object sender, EventArrivedEventArgs e) { DeviceNotified(1, e); }
        private void DeviceAdded(object sender, EventArrivedEventArgs e) { DeviceNotified(2, e); }
        private void DeviceRemoved(object sender, EventArrivedEventArgs e) { DeviceNotified(3, e); }

        private void DeviceNotified(int evId, EventArrivedEventArgs e)
        {
            string DeviceID = "";
            string ClassGUID = "";

            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];            
            try { DeviceID = instance.Properties["PNPDeviceID"].Value.ToString(); } catch { };
            try { ClassGUID = instance.Properties["ClassGuid"].Value.ToString(); } catch { };
            try { if(!string.IsNullOrEmpty(DeviceID)) OnDeviceEvent((EventType)evId, DeviceID, ClassGUID, instance); } catch { };            
        }

        protected virtual void OnDeviceEvent(EventType eventType, string DeviceID, string ClassGuid, ManagementBaseObject manObj)
        {
            string StatusText = String.Format("OnDeviceEvent: {0} {1} {2}", eventType, DeviceID, ClassGuid);

            ServiceLog.WriteDatedLn(StatusText);
            Console.WriteLine(StatusText);            
        }       
    }
}
