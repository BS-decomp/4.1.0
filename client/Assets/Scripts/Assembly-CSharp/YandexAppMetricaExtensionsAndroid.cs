using UnityEngine;

public static class YandexAppMetricaExtensionsAndroid
{
	public static AndroidJavaObject ToLocation(this Coordinates self)
	{
		AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.location.Location", string.Empty);
		androidJavaObject.Call("setLatitude", self.Latitude);
		androidJavaObject.Call("setLongitude", self.Longitude);
		return androidJavaObject;
	}
}
