using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class Botcoin : SerializedMonoBehaviour
{
	private Rigidbody _rigidBody;
	[SerializeField] private float _collectAnimationDuration = .2f;

	private void Awake()
	{
		_rigidBody = GetComponent<Rigidbody>();
	}

	public void ThrowFromTransform(Transform _transform, float spread, float impulse)
	{
		// Calculate a direction for the coin using the direction and spread
		var randomDirection = _transform.forward +
		                      Random.Range(-spread, spread) * _transform.up +
		                      Random.Range(-spread, spread) * _transform.right;
		
		// Throw the coin!
		_rigidBody.AddForce(randomDirection * impulse, ForceMode.Impulse);
	}

	public void Collect(Transform collector, Action callback)
	{
		transform.DOMove(collector.position, _collectAnimationDuration).OnComplete(() => callback());
	}
}