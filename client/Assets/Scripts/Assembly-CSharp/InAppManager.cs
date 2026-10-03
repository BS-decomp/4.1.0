using System.Collections.Generic;
using FreeJSON;
using Prime31;
using UnityEngine;

public class InAppManager : MonoBehaviour
{
	private List<GooglePurchase> Purchases = new List<GooglePurchase>();

	private List<GoogleSkuInfo> Skus = new List<GoogleSkuInfo>();

	private static InAppManager instance;

	private void Start()
	{
		instance = this;
		Init();
	}

	private void OnEnable()
	{
		GoogleIABManager.billingSupportedEvent += billingSupportedEvent;
		GoogleIABManager.billingNotSupportedEvent += billingNotSupportedEvent;
		GoogleIABManager.queryInventorySucceededEvent += queryInventorySucceededEvent;
		GoogleIABManager.purchaseSucceededEvent += purchaseSucceededEvent;
		GoogleIABManager.purchaseFailedEvent += purchaseFailedEvent;
		GoogleIABManager.consumePurchaseSucceededEvent += consumePurchaseSucceededEvent;
		GoogleIABManager.consumePurchaseFailedEvent += consumePurchaseFailedEvent;
	}

	private void OnDisable()
	{
		GoogleIABManager.billingSupportedEvent -= billingSupportedEvent;
		GoogleIABManager.billingNotSupportedEvent -= billingNotSupportedEvent;
		GoogleIABManager.queryInventorySucceededEvent -= queryInventorySucceededEvent;
		GoogleIABManager.purchaseSucceededEvent -= purchaseSucceededEvent;
		GoogleIABManager.purchaseFailedEvent -= purchaseFailedEvent;
		GoogleIABManager.consumePurchaseSucceededEvent -= consumePurchaseSucceededEvent;
		GoogleIABManager.consumePurchaseFailedEvent -= consumePurchaseFailedEvent;
	}

	public static void Init()
	{
		string publicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAkW8ls1E1B9/P6CXoZKl0tBNP+EUX6FIwbGbm/9LYW5iVOXASnng3+egvlSPnMekvcv9p/NoDRRY01pZhmlq1uShTlmaNT2pJipP2YXcyDvIPuO2rQSGKG8dIYmTAaIcVqqOl+22BQ321M8sSnCvhNOirCFbAEG5dCC0SoT3AOTFYB0GH5QbioLt0P+oV+a37c3GXbNJwlsMEmfeGxLEbgrpmrKfGT2E1JZta3JmcAXj+SVsxo5jOytiS7SluhBopirVU2nDDy+MawAVLCBGgOfeTfNBfFiwiFkLK7sS/wqRzl93/mlv2uZI62MNT/zHdfDWI+1szhUrckwvEwuGeHQIDAQAB";
		GoogleIAB.init(publicKey);
		GoogleIAB.setAutoVerifySignatures(true);
	}

	private void billingSupportedEvent()
	{
		UpdateQueryInventory();
	}

	private void UpdateQueryInventory()
	{
		string[] skus = new string[12]
		{
			"com.rexetstudio.blockstrike.m5000", "com.rexetstudio.blockstrike.m10000", "com.rexetstudio.blockstrike.m20000", "com.rexetstudio.blockstrike.m30000", "com.rexetstudio.blockstrike.g100", "com.rexetstudio.blockstrike.g250", "com.rexetstudio.blockstrike.g600", "com.rexetstudio.blockstrike.g1000", "com.rexetstudio.blockstrike.ads", "com.rexetstudio.blockstrike.allweapons",
			"com.rexetstudio.blockstrike.allskins", "com.rexetstudio.blockstrike.fullpack"
		};
		GoogleIAB.queryInventory(skus);
	}

	private void billingNotSupportedEvent(string error)
	{
	}

	public static string GetPrice(string sku)
	{
		if (Application.isEditor)
		{
			return "0,00$";
		}
		for (int i = 0; i < instance.Skus.Count; i++)
		{
			if (instance.Skus[i].productId == sku)
			{
				return instance.Skus[i].price;
			}
		}
		return string.Empty;
	}

	public static bool GetPurchase(string sku)
	{
		if (Application.isEditor)
		{
			return false;
		}
		for (int i = 0; i < instance.Purchases.Count; i++)
		{
			if (instance.Purchases[i].productId == sku)
			{
				return true;
			}
		}
		return false;
	}

	private void queryInventorySucceededEvent(List<GooglePurchase> purchases, List<GoogleSkuInfo> skus)
	{
		Skus.Clear();
		Purchases.Clear();
		Skus = skus;
		for (int i = 0; i < purchases.Count; i++)
		{
			if (Verify(purchases[i]))
			{
				Purchases.Add(purchases[i]);
				if (isConsume(purchases[i]))
				{
					GoogleIAB.consumeProduct(purchases[i].productId);
				}
			}
		}
		EventManager.Dispatch("UpdateInApp");
	}

	public static void Purchase(string sku)
	{
		GoogleIAB.purchaseProduct(sku);
	}

	private void purchaseSucceededEvent(GooglePurchase purchase)
	{
		if (Verify(purchase))
		{
			if (isConsume(purchase))
			{
				GoogleIAB.consumeProduct(purchase.productId);
				CheckConsume(purchase);
			}
			string inAppPurchase = purchase.productId.Substring(purchase.productId.LastIndexOf(".") + 1);
			AccountManager.SetInAppPurchase(inAppPurchase);
			UIToast.Show("Purchase Complete");
			SendFirebase(purchase);
		}
		else
		{
			UIToast.Show(Localization.Get("Error"));
		}
		EventManager.Dispatch("UpdateInApp");
	}

	private void purchaseFailedEvent(string error, int response)
	{
		UIToast.Show(Localization.Get("Error") + ": " + error);
	}

	public static void Consume(string sku)
	{
		GoogleIAB.purchaseProduct(sku);
	}

	private void consumePurchaseSucceededEvent(GooglePurchase purchase)
	{
		UpdateQueryInventory();
	}

	private void consumePurchaseFailedEvent(string error)
	{
		UpdateQueryInventory();
	}

	private bool Verify(GooglePurchase purchase)
	{
		string originalJson = purchase.originalJson;
		string signature = purchase.signature;
		string publicKey = "<RSAKeyValue><Modulus>kW8ls1E1B9/P6CXoZKl0tBNP+EUX6FIwbGbm/9LYW5iVOXASnng3+egvlSPnMekvcv9p/NoDRRY01pZhmlq1uShTlmaNT2pJipP2YXcyDvIPuO2rQSGKG8dIYmTAaIcVqqOl+22BQ321M8sSnCvhNOirCFbAEG5dCC0SoT3AOTFYB0GH5QbioLt0P+oV+a37c3GXbNJwlsMEmfeGxLEbgrpmrKfGT2E1JZta3JmcAXj+SVsxo5jOytiS7SluhBopirVU2nDDy+MawAVLCBGgOfeTfNBfFiwiFkLK7sS/wqRzl93/mlv2uZI62MNT/zHdfDWI+1szhUrckwvEwuGeHQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
		return AndroidNativeFunctions.VerifyGooglePlayPurchase(originalJson, signature, publicKey);
	}

	private bool isConsume(GooglePurchase purchase)
	{
		switch (purchase.productId)
		{
		case "com.rexetstudio.blockstrike.m5000":
			return true;
		case "com.rexetstudio.blockstrike.m10000":
			return true;
		case "com.rexetstudio.blockstrike.m20000":
			return true;
		case "com.rexetstudio.blockstrike.m30000":
			return true;
		case "com.rexetstudio.blockstrike.g100":
			return true;
		case "com.rexetstudio.blockstrike.g250":
			return true;
		case "com.rexetstudio.blockstrike.g600":
			return true;
		case "com.rexetstudio.blockstrike.g1000":
			return true;
		default:
			return false;
		}
	}

	private void CheckConsume(GooglePurchase purchase)
	{
		switch (purchase.productId)
		{
		case "com.rexetstudio.blockstrike.m5000":
			AccountManager.SetMoney1(5000, true);
			break;
		case "com.rexetstudio.blockstrike.m10000":
			AccountManager.SetMoney1(10000, true);
			break;
		case "com.rexetstudio.blockstrike.m20000":
			AccountManager.SetMoney1(20000, true);
			break;
		case "com.rexetstudio.blockstrike.m30000":
			AccountManager.SetMoney1(30000, true);
			break;
		case "com.rexetstudio.blockstrike.g100":
			AccountManager.SetGold1(100, true);
			break;
		case "com.rexetstudio.blockstrike.g250":
			AccountManager.SetGold1(250, true);
			break;
		case "com.rexetstudio.blockstrike.g600":
			AccountManager.SetGold1(600, true);
			break;
		case "com.rexetstudio.blockstrike.g1000":
			AccountManager.SetGold1(1000, true);
			break;
		}
		EventManager.Dispatch("AccountUpdate");
	}

	private GoogleSkuInfo GetSku(string productId)
	{
		for (int i = 0; i < Skus.Count; i++)
		{
			if (Skus[i].productId == productId)
			{
				return Skus[i];
			}
		}
		return null;
	}

	private void SendFirebase(GooglePurchase purchase)
	{
		Firebase firebase = new Firebase();
		JsonObject jsonObject = new JsonObject();
		jsonObject.Add("token", purchase.purchaseToken);
		firebase.Child("Players").Child("InAppPurchase").Child(AccountManager.AccountID)
			.Child(purchase.purchaseTime.ToString())
			.UpdateValue(jsonObject.ToString());
	}
}
