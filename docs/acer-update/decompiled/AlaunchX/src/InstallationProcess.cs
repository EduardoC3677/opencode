namespace AlaunchX;

internal interface InstallationProcess
{
	void UpdateUIforModuleInfo(int totalModuleNum, int currentModuleNum, string moduleName);

	void TriggerReboot();
}
