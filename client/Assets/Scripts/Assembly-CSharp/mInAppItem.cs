using UnityEngine;

public class mInAppItem : MonoBehaviour
{
	public enum ItemList
	{
		Purchase = 0,
		Consume = 1
	}

	public ItemList Item;

	public string Sku;

	public UILabel PriceLabel;

	private bool isPurchase;

	private void Start()
	{
		PriceLabel.text = InAppManager.GetPrice(Sku);
		if (Item == ItemList.Purchase)
		{
			CheckPurchase();
			EventManager.AddListener("UpdateInApp", CheckPurchase);
		}
	}

	private void CheckPurchase()
	{
		if (InAppManager.GetPurchase(Sku))
		{
			UIWidget component = GetComponent<UIWidget>();
			component.alpha = 0.5f;
			isPurchase = true;
		}
	}

	private void OnClick()
	{
		if (!isPurchase)
		{
			if (!AccountManager.isConnect)
			{
				UIToast.Show(Localization.Get("Connection account"));
			}
			else if (Item == ItemList.Purchase)
			{
				InAppManager.Purchase(Sku);
			}
			else
			{
				InAppManager.Consume(Sku);
			}
		}
	}
}
