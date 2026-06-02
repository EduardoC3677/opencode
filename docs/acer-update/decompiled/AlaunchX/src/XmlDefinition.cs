using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace AlaunchX;

public class XmlDefinition
{
	public class APBundlePolicy
	{
		[XmlRoot("SCL")]
		public class APBundlePolicyTemplate
		{
			public class ProgramTemplate
			{
				public class RuleTemplate
				{
					public class LocalizationTemplate
					{
						[XmlAttribute("type")]
						public string Type { get; set; }

						[XmlText]
						public string InnerText { get; set; }
					}

					public class DependencyTemplate
					{
						[XmlAttribute("type")]
						public string Type { get; set; }

						[XmlText]
						public string InnerText { get; set; }
					}

					[XmlElement("Localization")]
					public LocalizationTemplate Localization { get; set; }

					[XmlElement("OSSKU")]
					public string OSSKU { get; set; }

					[XmlElement("Brand")]
					public string Brand { get; set; }

					[XmlElement("FormFactor")]
					public string FormFactor { get; set; }

					[XmlArray("IM")]
					[XmlArrayItem("Dependency")]
					public List<DependencyTemplate> IM { get; set; }

					public List<string> GetLocalizationList()
					{
						return Localization.InnerText.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}

					public List<string> GetOSSKUList()
					{
						return OSSKU.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}

					public List<string> GetBrandList()
					{
						return Brand.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}

					public List<string> GetFormFactorList()
					{
						return FormFactor.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}
				}

				[XmlAttribute("pid")]
				public string pid { get; set; }

				[XmlAttribute("ProgramType")]
				public string ProgramType { get; set; }

				[XmlAttribute("Product")]
				public string Product { get; set; }

				[XmlElement("Rule")]
				public RuleTemplate Rule { get; set; }

				public string GetProduct()
				{
					string text = Product;
					int num = Product.IndexOf('(');
					int num2 = Product.IndexOf(')');
					if (num != -1 && num2 != -1 && num2 > num)
					{
						if (num == 0)
						{
							text = Product.Substring(num2 + 1);
						}
						else if (num > 0)
						{
							text = Product.Substring(0, num);
						}
					}
					return text.Trim();
				}

				public bool IsValidForInstalltion()
				{
					bool flag = false;
					bool flag2 = false;
					bool flag3 = false;
					bool flag4 = false;
					string text = "Software\\OEM\\GCMReadiness";
					using (RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text, writable: true))
					{
						if (registryKey == null)
						{
							Utility.Logger(LogLevel.Error, "Fail to open regstry key: HKLM\\" + text);
						}
						else
						{
							try
							{
								object value = registryKey.GetValue("Formfactor");
								if (value == null)
								{
									Utility.Logger(LogLevel.Error, "Cannot find \"Formfactor\" in HKLM\\" + text);
								}
								else
								{
									string formFactor = (string)value;
									Utility.Logger(LogLevel.Info, "Get FormFactor=" + formFactor);
									if (Rule.GetFormFactorList().Any((string x) => string.Equals(x, "All", StringComparison.OrdinalIgnoreCase)) || Rule.GetFormFactorList().Any((string x) => string.Equals(x, formFactor, StringComparison.OrdinalIgnoreCase)))
									{
										Utility.Logger(LogLevel.Info, "FormFactor is valid");
										flag = true;
									}
									else
									{
										Utility.Logger(LogLevel.Info, "FormFactor is invalid");
									}
								}
							}
							catch (Exception ex)
							{
								Utility.Logger(LogLevel.Error, "Catch exception: " + ex.Message);
								Utility.Logger(LogLevel.Error, "Fail to get \"Formfactor\" in HKLM\\" + text);
							}
							try
							{
								object value2 = registryKey.GetValue("Brand");
								if (value2 == null)
								{
									Utility.Logger(LogLevel.Error, "Cannot find \"Brand\" in HKLM\\" + text);
								}
								else
								{
									string brand = (string)value2;
									Utility.Logger(LogLevel.Info, "Get Brand=" + brand);
									if (Rule.GetBrandList().Any((string x) => string.Equals(x, "All", StringComparison.OrdinalIgnoreCase)) || Rule.GetBrandList().Any((string x) => string.Equals(x, brand, StringComparison.OrdinalIgnoreCase)))
									{
										Utility.Logger(LogLevel.Info, "Brand is valid");
										flag2 = true;
									}
									else
									{
										Utility.Logger(LogLevel.Info, "Brand is invalid");
									}
								}
							}
							catch (Exception ex2)
							{
								Utility.Logger(LogLevel.Error, "Catch exception: " + ex2.Message);
								Utility.Logger(LogLevel.Error, "Fail to get \"Brand\" in HKLM\\" + text);
							}
							try
							{
								object value3 = registryKey.GetValue("OSSKU");
								if (value3 == null)
								{
									Utility.Logger(LogLevel.Error, "Cannot find \"OSSKU\" in HKLM\\" + text);
								}
								else
								{
									string ossku = (string)value3;
									Utility.Logger(LogLevel.Info, "Get OSSKU=" + ossku);
									if (Rule.GetOSSKUList().Any((string x) => string.Equals(x, "All", StringComparison.OrdinalIgnoreCase)) || Rule.GetOSSKUList().Any((string x) => string.Equals(x, ossku, StringComparison.OrdinalIgnoreCase)))
									{
										Utility.Logger(LogLevel.Info, "OSSKU is valid");
										flag3 = true;
									}
									else
									{
										Utility.Logger(LogLevel.Info, "OSSKU is invalid");
									}
								}
							}
							catch (Exception ex3)
							{
								Utility.Logger(LogLevel.Error, "Catch exception: " + ex3.Message);
								Utility.Logger(LogLevel.Error, "Fail to get \"OSSKU\" in HKLM\\" + text);
							}
							if (string.Equals(Rule.Localization.Type, "IR", StringComparison.OrdinalIgnoreCase) || string.Equals(Rule.Localization.Type, "OL", StringComparison.OrdinalIgnoreCase))
							{
								Utility.Logger(LogLevel.Info, "Localization type: " + Rule.Localization.Type);
								try
								{
									object value4 = registryKey.GetValue(Rule.Localization.Type);
									if (value4 == null)
									{
										Utility.Logger(LogLevel.Error, "Cannot find \"" + Rule.Localization.Type + "\" in HKLM\\" + text);
									}
									else
									{
										string text2 = (string)value4;
										Utility.Logger(LogLevel.Info, "Get " + Rule.Localization.Type + "=" + text2);
										List<string> localizationValueList = text2.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
										if (Rule.GetLocalizationList().Any((string x) => string.Equals(x, "All", StringComparison.OrdinalIgnoreCase)) || Rule.GetLocalizationList().Any((string x) => localizationValueList.Any((string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase))))
										{
											Utility.Logger(LogLevel.Info, "Localization(" + Rule.Localization.Type + ") is valid");
											flag4 = true;
										}
										else
										{
											Utility.Logger(LogLevel.Info, "Localization(" + Rule.Localization.Type + ") is invalid");
										}
									}
								}
								catch (Exception ex4)
								{
									Utility.Logger(LogLevel.Error, "Catch exception: " + ex4.Message);
									Utility.Logger(LogLevel.Error, "Fail to get \"" + Rule.Localization.Type + "\" in HKLM\\" + text);
								}
							}
						}
					}
					bool flag5 = true;
					if (Rule.IM.Count != 0 && Rule.IM.Any((RuleTemplate.DependencyTemplate x) => !Data.GCMIMWhiteList.Contains(x.InnerText)))
					{
						string text3 = "Software\\OEM\\GCMReadiness\\IM";
						using RegistryKey registryKey2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text3, writable: true);
						if (registryKey2 == null)
						{
							Utility.Logger(LogLevel.Error, "Fail to open regstry key: HKLM\\" + text3);
							flag5 = false;
						}
						else
						{
							foreach (RuleTemplate.DependencyTemplate item in Rule.IM)
							{
								if (Data.GCMIMWhiteList.Contains(item.InnerText))
								{
									Utility.Logger(LogLevel.Info, item.InnerText + " is in the GCM IM_WhiteList, no need to check.");
									continue;
								}
								try
								{
									object value5 = registryKey2.GetValue(item.InnerText);
									if (value5 == null)
									{
										Utility.Logger(LogLevel.Error, "Cannot find \"" + item.InnerText + "\" in HKLM\\" + text3);
										flag5 = false;
										break;
									}
									string text4 = (string)value5;
									Utility.Logger(LogLevel.Info, "Get " + item.InnerText + "=" + text4);
									if (string.Equals(item.Type, "Required", StringComparison.OrdinalIgnoreCase))
									{
										Utility.Logger(LogLevel.Info, "Dependency type: Required");
										if (string.Equals(text4, "True", StringComparison.OrdinalIgnoreCase))
										{
											Utility.Logger(LogLevel.Info, item.InnerText + " is valid");
											continue;
										}
										Utility.Logger(LogLevel.Info, item.InnerText + " is invalid");
										flag5 = false;
									}
									else if (string.Equals(item.Type, "Opt-out", StringComparison.OrdinalIgnoreCase))
									{
										Utility.Logger(LogLevel.Info, "Dependency type: Opt-out");
										if (string.Equals(text4, "False", StringComparison.OrdinalIgnoreCase))
										{
											Utility.Logger(LogLevel.Info, item.InnerText + " is valid");
											continue;
										}
										Utility.Logger(LogLevel.Info, item.InnerText + " is invalid");
										flag5 = false;
									}
									else
									{
										Utility.Logger(LogLevel.Info, "Undefined dependency type: " + item.Type);
										flag5 = false;
									}
								}
								catch (Exception ex5)
								{
									Utility.Logger(LogLevel.Error, "Catch exception: " + ex5.Message);
									Utility.Logger(LogLevel.Error, "Fail to get \"" + item.InnerText + "\" in HKLM\\" + text3);
									flag5 = false;
								}
								break;
							}
						}
					}
					else
					{
						Utility.Logger(LogLevel.Info, "No need to check IM since there is no IM requirement for this program");
					}
					return flag && flag2 && flag3 && flag4 && flag5;
				}
			}

			public class RegionTemplate
			{
				public class PlacementTemplate
				{
					public class TouchPointTemplate
					{
						[XmlAttribute("value")]
						public int value { get; set; }

						[XmlAttribute("pid")]
						public string pid { get; set; }

						[XmlAttribute("group")]
						public string group { get; set; }

						[XmlAttribute("bundle")]
						public string bundle { get; set; }

						[XmlText]
						public string InnerText { get; set; }

						public void GetOrder(out int order, out int orderInBundle)
						{
							order = 0;
							orderInBundle = -1;
							string[] array = InnerText.Split(new string[1] { "-" }, StringSplitOptions.RemoveEmptyEntries);
							if (array.Length > 1)
							{
								try
								{
									order = Convert.ToInt32(array[0]);
								}
								catch (Exception)
								{
									order = 0;
								}
								try
								{
									orderInBundle = Convert.ToInt32(array[1]);
									return;
								}
								catch (Exception)
								{
									orderInBundle = -1;
									return;
								}
							}
							order = Convert.ToInt32(array[0]);
						}
					}

					[XmlAttribute("type")]
					public string type { get; set; }

					[XmlElement("TouchPoint")]
					public List<TouchPointTemplate> TouchPoints { get; set; }
				}

				[XmlAttribute("name")]
				public string name { get; set; }

				[XmlElement("Placement")]
				public List<PlacementTemplate> Placements { get; set; }
			}

			[XmlAttribute("SCL-id")]
			public string SCL_id { get; set; }

			[XmlAttribute("MRDVersion")]
			public string MRDVersion { get; set; }

			[XmlAttribute("MRDPAGE-id")]
			public string MRDPAGE_id { get; set; }

			[XmlAttribute("AppsTemplate-id")]
			public string AppsTemplate_id { get; set; }

			[XmlElement("MRD_OSSKU")]
			public string MRD_OSSKU { get; set; }

			[XmlElement("MRD_IMAGE_MODIFIERS")]
			public string MRD_IMAGE_MODIFIERS { get; set; }

			[XmlElement("MRD_IMAGE_MODIFIERS_SHIPPED")]
			public string MRD_IMAGE_MODIFIERS_SHIPPED { get; set; }

			[XmlElement("MRD_FormFactor")]
			public string MRD_FormFactor { get; set; }

			[XmlElement("MRD_BRAND")]
			public string MRD_BRAND { get; set; }

			[XmlArray("Programs")]
			[XmlArrayItem("Program")]
			public List<ProgramTemplate> Programs { get; set; }

			[XmlArray("Regions")]
			[XmlArrayItem("Region")]
			public List<RegionTemplate> Regions { get; set; }

			public bool IsNeedToPin(string programpid, string placementType, string imageRegion)
			{
				bool result = false;
				RegionTemplate regionTemplate = Regions.FirstOrDefault((RegionTemplate x) => string.Equals(x.name, imageRegion, StringComparison.OrdinalIgnoreCase));
				if (regionTemplate != null)
				{
					RegionTemplate.PlacementTemplate placementTemplate = regionTemplate.Placements.FirstOrDefault((RegionTemplate.PlacementTemplate x) => string.Equals(x.type, placementType, StringComparison.OrdinalIgnoreCase));
					if (placementTemplate != null)
					{
						RegionTemplate.PlacementTemplate.TouchPointTemplate touchPointTemplate = placementTemplate.TouchPoints.FirstOrDefault((RegionTemplate.PlacementTemplate.TouchPointTemplate x) => string.Equals(x.pid, programpid, StringComparison.OrdinalIgnoreCase));
						if (touchPointTemplate != null)
						{
							int order = 0;
							int orderInBundle = -1;
							switch (placementType)
							{
							case "SM":
							case "NS":
								touchPointTemplate.GetOrder(out order, out orderInBundle);
								if (order != 0)
								{
									result = true;
								}
								break;
							case "DT":
							case "DS":
								result = true;
								break;
							case "TS":
								if (touchPointTemplate.value == 1)
								{
									touchPointTemplate.GetOrder(out order, out orderInBundle);
									if (order != 0)
									{
										result = true;
									}
								}
								break;
							case "NA":
								if (touchPointTemplate.value == 2)
								{
									result = true;
								}
								break;
							case "MF":
								touchPointTemplate.GetOrder(out order, out orderInBundle);
								if (order != 0)
								{
									result = true;
								}
								break;
							case "RM":
								result = true;
								break;
							}
						}
					}
				}
				return result;
			}
		}

		public string FilePath = string.Empty;

		public APBundlePolicyTemplate APBundlePolicyInfo;

		public APBundlePolicy(string filePath)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			FilePath = filePath;
			XmlSerializer val = new XmlSerializer(typeof(APBundlePolicyTemplate));
			try
			{
				XmlReader val2 = XmlReader.Create(FilePath);
				try
				{
					APBundlePolicyInfo = (APBundlePolicyTemplate)val.Deserialize(val2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex.ToString());
				APBundlePolicyInfo = null;
			}
		}
	}

	public class WOS
	{
		[XmlRoot("getWOS")]
		public class WOSTemplate
		{
			public class WOSsTemplate
			{
				[XmlAttribute("Count")]
				public int Count { get; set; }

				[XmlElement("WOS")]
				public List<WOSDetailInfoTemplate> WOS { get; set; }
			}

			public class WOSDetailInfoTemplate
			{
				[XmlAttribute("CPU")]
				public string CPU { get; set; }

				[XmlAttribute("SKU")]
				public string SKU { get; set; }

				[XmlAttribute("PartNumber")]
				public string PartNumber { get; set; }

				[XmlAttribute("Parent_PartNumber")]
				public string Parent_PartNumber { get; set; }

				[XmlAttribute("ShortName")]
				public string ShortName { get; set; }

				[XmlAttribute("SWBOM_Abbreviation")]
				public string SWBOM_Abbreviation { get; set; }

				[XmlAttribute("SkuDisplayName")]
				public string SkuDisplayName { get; set; }
			}

			[XmlElement("Ret")]
			public string Ret { get; set; }

			[XmlElement("WOSs")]
			public WOSsTemplate WOSs { get; set; }

			[XmlElement("Des")]
			public string Des { get; set; }
		}

		public string FilePath = string.Empty;

		public WOSTemplate WOSInfo;

		public WOS(string filePath)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			FilePath = filePath;
			XmlSerializer val = new XmlSerializer(typeof(WOSTemplate));
			try
			{
				XmlReader val2 = XmlReader.Create(FilePath);
				try
				{
					WOSInfo = (WOSTemplate)val.Deserialize(val2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex.ToString());
				WOSInfo = null;
			}
		}

		public List<string> GetParentPNList(string wosPN)
		{
			List<string> list = new List<string>();
			foreach (WOSTemplate.WOSDetailInfoTemplate wO in WOSInfo.WOSs.WOS)
			{
				if (!string.Equals(wO.PartNumber, wosPN, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string[] array = wO.Parent_PartNumber.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					if (!list.Contains(text))
					{
						list.Add(text);
					}
					foreach (string parentPN in GetParentPNList(text))
					{
						if (!list.Contains(parentPN))
						{
							list.Add(parentPN);
						}
					}
				}
			}
			return list;
		}

		public List<string> GetChildPNList(string wosPN)
		{
			List<string> list = new List<string>();
			foreach (WOSTemplate.WOSDetailInfoTemplate wO in WOSInfo.WOSs.WOS)
			{
				if (!Enumerable.Contains(wO.Parent_PartNumber.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries), wosPN))
				{
					continue;
				}
				if (!list.Contains(wO.PartNumber))
				{
					list.Add(wO.PartNumber);
				}
				foreach (string childPN in GetChildPNList(wO.PartNumber))
				{
					if (!list.Contains(childPN))
					{
						list.Add(childPN);
					}
				}
			}
			return list;
		}
	}

	public class Lang
	{
		[XmlRoot("getLanguage")]
		public class LangTemplate
		{
			public class LanguagesTemplate
			{
				[XmlAttribute("Count")]
				public int Count { get; set; }

				[XmlElement("Language")]
				public List<LanguageTemplate> Language { get; set; }
			}

			public class LanguageTemplate
			{
				[XmlAttribute("PartNumber")]
				public string PartNumber { get; set; }

				[XmlAttribute("Name")]
				public string Name { get; set; }

				[XmlAttribute("SWBOM_LANG_CODE")]
				public string SWBOM_LANG_CODE { get; set; }

				[XmlAttribute("Lookup")]
				public string Lookup { get; set; }

				[XmlAttribute("LangIDShortname")]
				public string LangIDShortname { get; set; }

				[XmlAttribute("BaseImg")]
				public string BaseImg { get; set; }
			}

			[XmlElement("Ret")]
			public string Ret { get; set; }

			[XmlElement("Languages")]
			public LanguagesTemplate Languages { get; set; }

			[XmlElement("Des")]
			public string Des { get; set; }
		}

		public string FilePath = string.Empty;

		public LangTemplate LangInfo;

		public Lang(string filePath)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			FilePath = filePath;
			XmlSerializer val = new XmlSerializer(typeof(LangTemplate));
			try
			{
				XmlReader val2 = XmlReader.Create(FilePath);
				try
				{
					LangInfo = (LangTemplate)val.Deserialize(val2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex.ToString());
				LangInfo = null;
			}
		}
	}

	public class ModuleEnc
	{
		[XmlRoot("ModuleInfo")]
		public class ModuleInfoTemplate
		{
			public class CreatorTemplate
			{
				[XmlAttribute("Name")]
				public string Name { get; set; }

				[XmlAttribute("PN")]
				public string PN { get; set; }
			}

			public class TypeTemplate
			{
				[XmlAttribute("Name")]
				public string Name { get; set; }

				[XmlAttribute("PN")]
				public string PN { get; set; }
			}

			public class ModuleTemplate
			{
				[XmlAttribute("Name")]
				public string Name { get; set; }

				[XmlAttribute("PN")]
				public string PN { get; set; }

				[XmlAttribute("Class")]
				public string Class { get; set; }

				[XmlAttribute("Provider")]
				public string Provider { get; set; }

				[XmlAttribute("Serial")]
				public string Serial { get; set; }

				[XmlAttribute("GUID")]
				public string GUID { get; set; }

				[XmlAttribute("Alias")]
				public string Alias { get; set; }
			}

			public class WOSTemplate
			{
				[XmlAttribute("CPU")]
				public string CPU { get; set; }

				[XmlAttribute("SKU")]
				public string SKU { get; set; }

				[XmlAttribute("PN")]
				public string PN { get; set; }
			}

			public class ProcessTemplate
			{
				[XmlAttribute("Period")]
				public string Period { get; set; }

				[XmlAttribute("Order")]
				public string Order { get; set; }

				[XmlAttribute("Exec")]
				public string Exec { get; set; }

				[XmlAttribute("Params")]
				public string Params { get; set; }

				[XmlAttribute("RetCode")]
				public string RetCode { get; set; }

				[XmlAttribute("Timeout")]
				public int Timeout { get; set; }

				[XmlAttribute("Check")]
				public string Check { get; set; }

				[XmlAttribute("OS-Sku")]
				public string OSSku { get; set; }

				[XmlAttribute("OS-Language")]
				public string OSLanguage { get; set; }

				[XmlAttribute("Country")]
				public string Country { get; set; }

				[XmlAttribute("VersionCheck")]
				public string VersionCheck { get; set; }

				[XmlAttribute("FileCheck")]
				public string FileCheck { get; set; }

				[XmlAttribute("RegistryPath")]
				public string RegistryPath { get; set; }

				[XmlAttribute("KeyValuePair")]
				public string KeyValuePair { get; set; }
			}

			public class ItemTemplate
			{
				[XmlAttribute("Index")]
				public string Index { get; set; }

				[XmlAttribute("DisplayName")]
				public string DisplayName { get; set; }

				[XmlAttribute("Version")]
				public string Version { get; set; }

				[XmlAttribute("Check")]
				public string Check { get; set; }

				[XmlAttribute("Validation")]
				public string Validation { get; set; }
			}

			[XmlElement("ModulePN")]
			public string ModulePN { get; set; }

			[XmlElement("Creator")]
			public CreatorTemplate Creator { get; set; }

			[XmlElement("Type")]
			public TypeTemplate Type { get; set; }

			[XmlElement("Module")]
			public ModuleTemplate Module { get; set; }

			[XmlElement("WOS")]
			public WOSTemplate WOS { get; set; }

			[XmlElement("Lang")]
			public string Lang { get; set; }

			[XmlElement("ModuleVersion")]
			public string ModuleVersion { get; set; }

			[XmlElement("DriverVersion")]
			public string DriverVersion { get; set; }

			[XmlElement("OptionVersion")]
			public string OptionVersion { get; set; }

			[XmlElement("ProjectCodeName")]
			public string ProjectCodeName { get; set; }

			[XmlElement("OdmName")]
			public string OdmName { get; set; }

			[XmlElement("ModuleTimeout")]
			public string ModuleTimeout { get; set; }

			[XmlElement("InstallPreiod")]
			public string InstallPeriod { get; set; }

			[XmlElement("Reboot")]
			public string Reboot { get; set; }

			[XmlElement("Delete")]
			public string Delete { get; set; }

			[XmlElement("AutoRun")]
			public string AutoRun { get; set; }

			[XmlElement("InstallPath")]
			public string InstallPath { get; set; }

			[XmlElement("Description")]
			public string Description { get; set; }

			[XmlArray("ProcessList")]
			[XmlArrayItem("Process")]
			public List<ProcessTemplate> ProcessList { get; set; }

			[XmlArray("ReinstallProcessList")]
			[XmlArrayItem("Process")]
			public List<ProcessTemplate> ReinstallProcessList { get; set; }

			[XmlArray("RegItemList")]
			[XmlArrayItem("Item")]
			public List<ItemTemplate> RegItemList { get; set; }

			[XmlElement("ModuleProperty")]
			public string ModuleProperty { get; set; }

			[XmlElement("HWIDs")]
			public string HWIDs { get; set; }

			[XmlElement("AutoCheckHWID")]
			public string AutoCheckHWID { get; set; }

			[XmlElement("CheckRemove")]
			public string CheckRemove { get; set; }
		}

		public string FilePath = string.Empty;

		public ModuleInfoTemplate ModuleInfo;

		public ModuleEnc(string filePath)
		{
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Expected O, but got Unknown
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			FilePath = filePath;
			if (string.Equals(Path.GetExtension(FilePath), ".enc", StringComparison.OrdinalIgnoreCase))
			{
				string plainText = null;
				if (CryptData.DecryptFile(FilePath, ref plainText) == CryptResult.CryptSuccess)
				{
					XmlSerializer val = new XmlSerializer(typeof(ModuleInfoTemplate));
					try
					{
						using StringReader stringReader = new StringReader(plainText);
						ModuleInfo = (ModuleInfoTemplate)val.Deserialize((TextReader)stringReader);
					}
					catch (Exception ex)
					{
						Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex.ToString());
						ModuleInfo = null;
					}
				}
			}
			if (!string.Equals(Path.GetExtension(FilePath), ".xml", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			XmlSerializer val2 = new XmlSerializer(typeof(ModuleInfoTemplate));
			try
			{
				XmlReader val3 = XmlReader.Create(FilePath);
				try
				{
					ModuleInfo = (ModuleInfoTemplate)val2.Deserialize(val3);
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			catch (Exception ex2)
			{
				Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex2.ToString());
				ModuleInfo = null;
			}
		}
	}

	public class ProductInformation
	{
		[XmlRoot("ProductInfo")]
		public class PrdInfoTemplate
		{
			public class InstalledAppTemplate
			{
				[XmlAttribute("Name")]
				public string Name = string.Empty;

				[XmlAttribute("Version")]
				public string Version = string.Empty;

				[XmlAttribute("StartTime")]
				public string StartTime = string.Empty;

				[XmlAttribute("EndTime")]
				public string EndTime = string.Empty;

				[XmlAttribute("SpentTime")]
				public string SpentTime = string.Empty;
			}

			public class InstalledDriverTemplate
			{
				[XmlAttribute("Name")]
				public string Name = string.Empty;

				[XmlAttribute("DriverVersion")]
				public string DriverVersion = string.Empty;

				[XmlAttribute("StartTime")]
				public string StartTime = string.Empty;

				[XmlAttribute("EndTime")]
				public string EndTime = string.Empty;

				[XmlAttribute("SpentTime")]
				public string SpentTime = string.Empty;
			}

			[XmlElement("SerialNumber")]
			public string SerialNumber = string.Empty;

			[XmlElement("CreateDate")]
			public string CreateDate = string.Empty;

			[XmlElement("ModelName")]
			public string ModelName = string.Empty;

			[XmlElement("OS")]
			public string OS = string.Empty;

			[XmlElement("OSBuild")]
			public string OSVersion = string.Empty;

			[XmlElement("BIOSVersion")]
			public string BIOSVersion = string.Empty;

			[XmlElement("SLIRCDID")]
			public string SLIRCDID = string.Empty;

			[XmlElement("RSLKitID")]
			public string RSLKitID = string.Empty;

			[XmlElement("RCDPN")]
			public string RCDPN = string.Empty;

			[XmlElement("SCDPN")]
			public string SCDPN = string.Empty;

			[XmlArray("LPCD")]
			[XmlArrayItem("PN")]
			public List<string> LPCDPNList = new List<string>();

			[XmlArray("InstalledApps")]
			[XmlArrayItem("InstalledApp")]
			public List<InstalledAppTemplate> InstalledApps = new List<InstalledAppTemplate>();

			[XmlArray("InstalledDrivers")]
			[XmlArrayItem("InstalledDriver")]
			public List<InstalledDriverTemplate> InstalledDrivers = new List<InstalledDriverTemplate>();

			public void GetMainInfo()
			{
				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
				//IL_008a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0274: Unknown result type (might be due to invalid IL or missing references)
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Expected O, but got Unknown
				Utility.Logger(LogLevel.Info, "Get main info of current machine");
				try
				{
					ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS").Get().GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							ManagementObject val = (ManagementObject)enumerator.Current;
							SerialNumber = ((ManagementBaseObject)val)["SerialNumber"].ToString().Trim();
						}
					}
					finally
					{
						((IDisposable)enumerator)?.Dispose();
					}
				}
				catch (Exception)
				{
					SerialNumber = string.Empty;
				}
				CreateDate = DateTime.Now.ToString("yyyy/MM/dd");
				try
				{
					ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("select * from Win32_ComputerSystem").Get().GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							ManagementBaseObject current = enumerator.Current;
							ModelName = current["Model"].ToString();
						}
					}
					finally
					{
						((IDisposable)enumerator)?.Dispose();
					}
				}
				catch (Exception)
				{
					ModelName = string.Empty;
				}
				if (Directory.Exists("C:\\OEM\\Preload\\Command"))
				{
					string[] files = Directory.GetFiles("C:\\OEM\\Preload\\Command", "POP*.ini");
					if (files.Length == 1)
					{
						OS = Utility.GetIniKeyValue(files[0], "Main", "SKU");
					}
				}
				using (RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
				{
					if (registryKey != null)
					{
						try
						{
							string text = string.Empty;
							string text2 = string.Empty;
							string text3 = string.Empty;
							string text4 = string.Empty;
							object value = registryKey.GetValue("CurrentMajorVersionNumber");
							if (value != null)
							{
								text = ((int)value).ToString();
							}
							value = registryKey.GetValue("CurrentMinorVersionNumber");
							if (value != null)
							{
								text2 = ((int)value).ToString();
							}
							value = registryKey.GetValue("CurrentBuildNumber");
							if (value != null)
							{
								text3 = (string)value;
							}
							value = registryKey.GetValue("UBR");
							if (value != null)
							{
								text4 = ((int)value).ToString();
							}
							if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3))
							{
								OSVersion = text + "." + text2 + "." + text3;
								if (!string.IsNullOrEmpty(text4))
								{
									OSVersion = OSVersion + "." + text4;
								}
							}
						}
						catch (Exception)
						{
							OSVersion = string.Empty;
						}
					}
				}
				try
				{
					ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("select * from Win32_BIOS").Get().GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							ManagementBaseObject current2 = enumerator.Current;
							string text5 = current2["Manufacturer"].ToString();
							string text6 = current2["SMBIOSBIOSVersion"].ToString();
							BIOSVersion = text5 + " " + text6;
						}
					}
					finally
					{
						((IDisposable)enumerator)?.Dispose();
					}
				}
				catch (Exception)
				{
					BIOSVersion = string.Empty;
				}
				if (File.Exists("C:\\OEM\\NAPP\\OSLRCD.dat"))
				{
					SLIRCDID = Utility.GetIniKeyValue("C:\\OEM\\NAPP\\OSLRCD.dat", "MAIN", "SLIRCD ID");
					RSLKitID = Utility.GetIniKeyValue("C:\\OEM\\NAPP\\OSLRCD.dat", "MAIN", "RSLKit ID");
				}
				if (Directory.Exists("C:\\OEM\\NAPP"))
				{
					RCDPN = Utility.GetIniKeyValue("C:\\OEM\\NAPP\\RCD.dat", "MAIN", "RCD NO");
					SCDPN = Utility.GetIniKeyValue("C:\\OEM\\NAPP\\SystemCD.dat", "Main", "SCD_NO");
					string[] files2 = Directory.GetFiles("C:\\OEM\\NAPP", "LPCD*.dat");
					foreach (string filePath in files2)
					{
						LPCDPNList.Add(Utility.GetIniKeyValue(filePath, "MAIN", "SWBOM PN"));
					}
				}
			}
		}

		public string FilePath = string.Empty;

		public PrdInfoTemplate PrdInfo;

		public ProductInformation(string filePath)
		{
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			FilePath = filePath;
			if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
			{
				string plainText = string.Empty;
				if (CryptData.DecryptFile(filePath, ref plainText) == CryptResult.CryptSuccess)
				{
					XmlSerializer val = new XmlSerializer(typeof(PrdInfoTemplate));
					try
					{
						XmlReader val2 = XmlReader.Create((Stream)new MemoryStream(Encoding.UTF8.GetBytes(plainText)));
						try
						{
							PrdInfo = (PrdInfoTemplate)val.Deserialize(val2);
							return;
						}
						finally
						{
							((IDisposable)val2)?.Dispose();
						}
					}
					catch (Exception ex)
					{
						Utility.Logger(LogLevel.Error, "Deserialize fail, catched exception: " + ex.ToString());
						PrdInfo = null;
						return;
					}
				}
				Utility.Logger(LogLevel.Error, "Failed to decrypt " + filePath);
				PrdInfo = null;
			}
			else
			{
				PrdInfo = new PrdInfoTemplate();
			}
		}

		public void SaveToFile()
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			XmlSerializer val = new XmlSerializer(typeof(PrdInfoTemplate));
			try
			{
				MemoryStream memoryStream = new MemoryStream();
				using StreamWriter streamWriter = new StreamWriter(memoryStream);
				val.Serialize((TextWriter)streamWriter, (object)PrdInfo);
				memoryStream.Position = 0L;
				using StreamReader streamReader = new StreamReader(memoryStream);
				if (CryptData.EncryptSaveFile(FilePath, streamReader.ReadToEnd()) != CryptResult.CryptSuccess)
				{
					Utility.Logger(LogLevel.Error, "Failed to encrypt " + FilePath);
				}
			}
			catch (Exception ex)
			{
				Utility.Logger(LogLevel.Error, ex.ToString());
			}
		}

		public PrdInfoTemplate.InstalledAppTemplate FindAppByName(string appName)
		{
			return PrdInfo.InstalledApps.FirstOrDefault((PrdInfoTemplate.InstalledAppTemplate x) => string.Equals(x.Name, appName));
		}

		public PrdInfoTemplate.InstalledDriverTemplate FindDriverByName(string driverName)
		{
			return PrdInfo.InstalledDrivers.FirstOrDefault((PrdInfoTemplate.InstalledDriverTemplate x) => string.Equals(x.Name, driverName));
		}

		public void RemoveInstalledApp(string appName)
		{
			int index = PrdInfo.InstalledApps.FindIndex((PrdInfoTemplate.InstalledAppTemplate x) => string.Equals(x.Name, appName));
			PrdInfo.InstalledApps.RemoveAt(index);
		}
	}
}
