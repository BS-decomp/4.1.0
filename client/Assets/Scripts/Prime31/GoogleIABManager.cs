using System;
using System.Collections.Generic;

namespace Prime31
{
	public class GoogleIABManager : AbstractManager
	{
		public static event Action billingSupportedEvent;

		public static event Action<string> billingNotSupportedEvent;

		public static event Action<List<GooglePurchase>, List<GoogleSkuInfo>> queryInventorySucceededEvent;

		public static event Action<string> queryInventoryFailedEvent;

		public static event Action<string, string> purchaseCompleteAwaitingVerificationEvent;

		public static event Action<GooglePurchase> purchaseSucceededEvent;

		public static event Action<string, int> purchaseFailedEvent;

		public static event Action<GooglePurchase> consumePurchaseSucceededEvent;

		public static event Action<string> consumePurchaseFailedEvent;

		static GoogleIABManager()
		{
			AbstractManager.initialize(typeof(GoogleIABManager));
		}

		public void billingSupported(string empty)
		{
			billingSupportedEvent.fire();
		}

		public void billingNotSupported(string error)
		{
			billingNotSupportedEvent.fire(error);
		}

		public void queryInventorySucceeded(string json)
		{
			if (queryInventorySucceededEvent != null)
			{
				Dictionary<string, object> dictionary = json.dictionaryFromJson();
				queryInventorySucceededEvent(GooglePurchase.fromList(dictionary["purchases"] as List<object>), GoogleSkuInfo.fromList(dictionary["skus"] as List<object>));
			}
		}

		public void queryInventoryFailed(string error)
		{
			queryInventoryFailedEvent.fire(error);
		}

		public void purchaseCompleteAwaitingVerification(string json)
		{
			if (purchaseCompleteAwaitingVerificationEvent != null)
			{
				Dictionary<string, object> dictionary = json.dictionaryFromJson();
				string arg = dictionary["purchaseData"].ToString();
				string arg2 = dictionary["signature"].ToString();
				purchaseCompleteAwaitingVerificationEvent(arg, arg2);
			}
		}

		public void purchaseSucceeded(string json)
		{
			purchaseSucceededEvent.fire(new GooglePurchase(json.dictionaryFromJson()));
		}

		public void purchaseFailed(string json)
		{
			if (purchaseFailedEvent != null)
			{
				Dictionary<string, object> dictionary = Json.decode<Dictionary<string, object>>(json);
				purchaseFailedEvent(dictionary["result"].ToString(), int.Parse(dictionary["response"].ToString()));
			}
		}

		public void consumePurchaseSucceeded(string json)
		{
			if (consumePurchaseSucceededEvent != null)
			{
				consumePurchaseSucceededEvent.fire(new GooglePurchase(json.dictionaryFromJson()));
			}
		}

		public void consumePurchaseFailed(string error)
		{
			consumePurchaseFailedEvent.fire(error);
		}
	}
}
