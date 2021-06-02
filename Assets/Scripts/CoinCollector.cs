using System.Collections;
using UnityEngine;

public class CoinCollector : MonoBehaviour
{
	[SerializeField] private float _delayBeforeCollect = 2f;

	private void OnTriggerEnter(Collider other)
	{
		// If a coin comes near
		var botcoin = other.gameObject.GetComponent<Botcoin>();
		if (botcoin != null)
		{
			// Collect the coin after a delay
			StartCoroutine(DelayCollectCoin(botcoin));
		}
	}

	private IEnumerator DelayCollectCoin(Botcoin botcoin)
	{
		yield return new WaitForSeconds(_delayBeforeCollect);
		
		botcoin.Collect(transform, () =>
		{
			++Game.Coins;
		});
	}
}
