using System.Collections;

public interface IYandexAppMetrica
{
	bool CollectInstalledApps { get; set; }

	string LibraryVersion { get; }

	int LibraryApiLevel { get; }

	void ActivateWithAPIKey(string apiKey);

	void ActivateWithConfiguration(YandexAppMetricaConfig config);

	void OnResumeApplication();

	void OnPauseApplication();

	void ReportEvent(string message);

	void ReportEvent(string message, Hashtable parameters);

	void ReportEvent(string message, string key, string value);

	void ReportError(string condition, string stackTrace);

	void SetTrackLocationEnabled(bool enabled);

	void SetLocation(Coordinates coordinates);

	void SetSessionTimeout(uint sessionTimeoutSeconds);

	void SetReportCrashesEnabled(bool enabled);

	void SetCustomAppVersion(string appVersion);

	void SetLoggingEnabled();

	void SetEnvironmentValue(string key, string value);
}
