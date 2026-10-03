using System;
using UnityEngine;

public class AppMetrica : MonoBehaviour
{
	[SerializeField]
	private string APIKey;

	[SerializeField]
	private bool ExceptionsReporting = true;

	[SerializeField]
	private uint SessionTimeoutSec = 10u;

	[SerializeField]
	private bool LoggingEnabled = true;

	private static bool _isInitialized = false;

	private bool _actualPauseStatus;

	private string prevExceptionLogString = string.Empty;

	private string prevExceptionStackTrace = string.Empty;

	private static IYandexAppMetrica _metrica = null;

	private static object syncRoot = new UnityEngine.Object();

	public static IYandexAppMetrica Instance
	{
		get
		{
			if (_metrica == null)
			{
				lock (syncRoot)
				{
					if (_metrica == null && Application.platform == RuntimePlatform.Android)
					{
						_metrica = new YandexAppMetricaAndroid();
					}
					if (_metrica == null)
					{
						_metrica = new YandexAppMetricaDummy();
					}
				}
			}
			return _metrica;
		}
	}

	private void SetupMetrica()
	{
		YandexAppMetricaConfig yandexAppMetricaConfig = new YandexAppMetricaConfig(APIKey);
		yandexAppMetricaConfig.SessionTimeout = (int)SessionTimeoutSec;
		yandexAppMetricaConfig.LoggingEnabled = LoggingEnabled;
		YandexAppMetricaConfig config = yandexAppMetricaConfig;
		Instance.ActivateWithConfiguration(config);
	}

	private void Awake()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			SetupMetrica();
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		Instance.OnResumeApplication();
	}

	private void OnEnable()
	{
		if (ExceptionsReporting)
		{
			ConsoleManager.LogCallback = (Action<string, string, LogType>)Delegate.Combine(ConsoleManager.LogCallback, new Action<string, string, LogType>(HandleLog));
		}
	}

	private void OnDisable()
	{
		if (ExceptionsReporting)
		{
			ConsoleManager.LogCallback = (Action<string, string, LogType>)Delegate.Remove(ConsoleManager.LogCallback, new Action<string, string, LogType>(HandleLog));
		}
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (_actualPauseStatus != pauseStatus)
		{
			_actualPauseStatus = pauseStatus;
			if (pauseStatus)
			{
				Instance.OnPauseApplication();
			}
			else
			{
				Instance.OnResumeApplication();
			}
		}
	}

	private void HandleLog(string condition, string stackTrace, LogType type)
	{
		if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && (prevExceptionLogString != condition || prevExceptionStackTrace != stackTrace))
		{
			Instance.ReportError(condition, stackTrace);
			prevExceptionLogString = condition;
			prevExceptionStackTrace = stackTrace;
		}
	}
}
