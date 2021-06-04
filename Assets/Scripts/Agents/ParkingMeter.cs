using System;
using System.Collections;
using TMPro;
using UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Agents
{
	public class ParkingMeter : Agent<ParkingMeter>, IHackable
	{
		public override string Name => "Parking Meter";
		
		[SerializeField] private ProgressUI _progress;
		[SerializeField] private float _delayBetweenCoinsCollected;
		[SerializeField] private int _startingCoins;

		public override Transform PluginSlot { get; }
		public override BaseAgent PluggedInto { get; set; }
		public override float AlreadyVisitedCooldownMultiplier => 10f;

		private Coroutine _hackingCoroutine;
		private int _coinsCollected;
		[SerializeField] private Botcoin _coinPrefab;
		[SerializeField] private Transform _coinSpawnPoint;
		[SerializeField] private float _delayBetweenCoinDispensing = 0.2f;
		[SerializeField] private int _maxCoins = 20;
		[SerializeField] private float _coinThrowImpulse = 10f;
		[SerializeField] private float _coinThrowSpread = 15f;
		[SerializeField] private TMP_Text _numCoinsText;

		public override bool FilterBy(string filter, IComputer computer)
		{
			// NOTE: Note used
			return true;
		}

		public float HackingDifficulty => 5f;
		public bool IsCrimeToHack => true;

		private void Start()
		{
			// Start with some coin
			_coinsCollected = _startingCoins;
			
			// Start collecting coins over time
			StartCoroutine(CollectCoins());
		}

		protected override void Update()
		{
			base.Update();

			_numCoinsText.text = $"Coins: {_coinsCollected}";
		}

		private IEnumerator CollectCoins()
		{
			yield return new WaitForSeconds(_delayBetweenCoinsCollected);
			_coinsCollected = Math.Min(_coinsCollected + 1, _maxCoins);
		}

		public void OnHacked()
		{
			Debug.Log($"{Name} has been hacked");
			
			// For all the coins the parking meter has collected
			if (_coinsCollected > 0)
				StartCoroutine(SpitOutCoins());
		}

		private IEnumerator SpitOutCoins()
		{
			// For all the coins collected
			while (_coinsCollected > 0)
			{
				// Create a new coin
				var coin = Instantiate(_coinPrefab, _coinSpawnPoint.position, Quaternion.Euler(Random.onUnitSphere));
				coin.ThrowFromTransform(_coinSpawnPoint, _coinThrowSpread, _coinThrowImpulse);
				--_coinsCollected;

				yield return new WaitForSeconds(_delayBetweenCoinDispensing);
			}
		}

		public void OnHackingStarted(Robot hacker, Action callback)
		{
			if (IsCrimeToHack)
			{
				// Flag the hacker as a criminal
				hacker.FlagACriminal();
				
				// Make a shout out that a crime is in progress
				Shout(ShoutType.CrimeInProgress);
			}
			
			Debug.Log($"{hacker.Name} is hacking {Name}");
			_hackingCoroutine = StartCoroutine(StartHacking(hacker, callback));
		}

		public void OnHackingInterrupted()
		{
			Debug.Log($"Hacking of {Name} interrupted");
			StopCoroutine(_hackingCoroutine);
			_progress.End();
		}

		private IEnumerator StartHacking(Robot hacker, Action callback)
		{
			var duration = HackingDifficulty / hacker.HackingSpeed;
			_progress.Begin("Hacking", duration);
			yield return new WaitForSeconds(duration);
			OnHacked();
			callback();
		}
	}
}
