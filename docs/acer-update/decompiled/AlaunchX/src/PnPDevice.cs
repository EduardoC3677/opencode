using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace AlaunchX;

public class PnPDevice
{
	public class DeviceInformation
	{
		public string DeviceID;

		public string Name;

		public string Manufacturer;

		public string Description;

		public string ClassGuid;

		public string Service;

		public string Status;

		public List<string> HardwareIDList;

		public string VendorID
		{
			get
			{
				try
				{
					return DeviceID.Split(new char[1] { '\\' })[1].Split(new char[1] { '&' })[0];
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		public string ProductID
		{
			get
			{
				try
				{
					return DeviceID.Split(new char[1] { '\\' })[1].Split(new char[1] { '&' })[1];
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		public DeviceInformation()
		{
			DeviceID = string.Empty;
			Name = string.Empty;
			Manufacturer = string.Empty;
			Description = string.Empty;
			ClassGuid = string.Empty;
			Service = string.Empty;
			Status = string.Empty;
			HardwareIDList = null;
		}

		public DeviceInformation(string deviceID, string name, string manufacturer, string description, string classGuid, string service, string status, List<string> hardwareIDList = null)
		{
			DeviceID = deviceID;
			Name = name;
			Manufacturer = manufacturer;
			Description = description;
			ClassGuid = classGuid;
			Service = service;
			Status = status;
			HardwareIDList = hardwareIDList;
		}

		public void AddHardwareID(string hardwareID)
		{
			if (HardwareIDList == null)
			{
				HardwareIDList = new List<string>();
			}
			HardwareIDList.Add(hardwareID);
		}
	}

	public static bool GetDevice(string classGuid, out DeviceInformation[] deviceInformation)
	{
		return GetAllDevices(out deviceInformation, "classGuid='" + classGuid + "'");
	}

	public static bool GetDeviceByDeviceID(string deviceID, out DeviceInformation deviceInformation)
	{
		deviceInformation = null;
		if (GetAllDevices(out var deviceInformationList, "DeviceID='" + deviceID + "'"))
		{
			deviceInformation = deviceInformationList[0];
			return true;
		}
		return false;
	}

	public static bool GetDriverVersionByDeviceID(out string driverVersion, string deviceID)
	{
		driverVersion = null;
		if (GetAllDriverVersion(out var driverVersions, "DeviceID='" + deviceID.Replace("\\", "\\\\") + "'") && driverVersions.Count() > 0)
		{
			driverVersion = driverVersions[0];
			return true;
		}
		return false;
	}

	public static bool GetAllDevices(out DeviceInformation[] deviceInformationList, string queryConditions = null)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		List<DeviceInformation> list = new List<DeviceInformation>();
		deviceInformationList = null;
		try
		{
			ManagementObjectSearcher val = new ManagementObjectSearcher("select * from Win32_PnPEntity" + (string.IsNullOrEmpty(queryConditions) ? "" : (" where " + queryConditions)));
			try
			{
				ManagementObjectCollection val2 = val.Get();
				if (val2 == null)
				{
					return false;
				}
				ManagementObjectEnumerator enumerator = val2.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ManagementObject val3 = (ManagementObject)enumerator.Current;
						DeviceInformation deviceInformation = new DeviceInformation();
						if (((ManagementBaseObject)val3)["DeviceID"] != null)
						{
							deviceInformation.DeviceID = ((ManagementBaseObject)val3)["DeviceID"].ToString();
						}
						if (((ManagementBaseObject)val3)["Name"] != null)
						{
							deviceInformation.Name = ((ManagementBaseObject)val3)["Name"].ToString();
						}
						if (((ManagementBaseObject)val3)["Manufacturer"] != null)
						{
							deviceInformation.Manufacturer = ((ManagementBaseObject)val3)["Manufacturer"].ToString();
						}
						if (((ManagementBaseObject)val3)["Description"] != null)
						{
							deviceInformation.Description = ((ManagementBaseObject)val3)["Description"].ToString();
						}
						if (((ManagementBaseObject)val3)["ClassGuid"] != null)
						{
							deviceInformation.ClassGuid = ((ManagementBaseObject)val3)["ClassGuid"].ToString();
						}
						if (((ManagementBaseObject)val3)["Service"] != null)
						{
							deviceInformation.Service = ((ManagementBaseObject)val3)["Service"].ToString();
						}
						if (((ManagementBaseObject)val3)["Status"] != null)
						{
							deviceInformation.Service = ((ManagementBaseObject)val3)["Status"].ToString();
						}
						if (((ManagementBaseObject)val3)["HardwareID"] != null)
						{
							string[] array = (string[])((ManagementBaseObject)val3)["HardwareID"];
							foreach (string text in array)
							{
								if (!string.IsNullOrEmpty(text))
								{
									deviceInformation.AddHardwareID(text);
								}
							}
						}
						list.Add(deviceInformation);
						val3.Dispose();
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
				val2.Dispose();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
			return false;
		}
		deviceInformationList = list.ToArray();
		return true;
	}

	public static bool GetAllDriverVersion(out string[] driverVersions, string queryConditions = null)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		List<string> list = new List<string>();
		driverVersions = null;
		try
		{
			ManagementObjectSearcher val = new ManagementObjectSearcher("select * from Win32_PnPSignedDriver" + (string.IsNullOrEmpty(queryConditions) ? "" : (" where " + queryConditions)));
			try
			{
				ManagementObjectCollection val2 = val.Get();
				if (val2 == null)
				{
					return false;
				}
				ManagementObjectEnumerator enumerator = val2.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ManagementObject val3 = (ManagementObject)enumerator.Current;
						if (((ManagementBaseObject)val3)["DriverVersion"] != null)
						{
							list.Add(((ManagementBaseObject)val3)["DriverVersion"].ToString());
						}
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
			return false;
		}
		driverVersions = list.ToArray();
		return true;
	}
}
