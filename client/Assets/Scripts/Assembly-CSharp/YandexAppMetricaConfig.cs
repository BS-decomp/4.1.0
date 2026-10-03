using System;

[Serializable]
public sealed class YandexAppMetricaConfig
{
	public string ApiKey { get; private set; }

	public string AppVersion { get; set; }

	public Coordinates Location { get; set; }

	public int? SessionTimeout { get; set; }

	public bool? ReportCrashesEnabled { get; set; }

	public bool? TrackLocationEnabled { get; set; }

	public bool? LoggingEnabled { get; set; }

	public bool? CollectInstalledApps { get; set; }

	public YandexAppMetricaPreloadInfo PreloadInfo { get; set; }

	public YandexAppMetricaConfig(string apiKey)
	{
		ApiKey = apiKey;
	}
}
