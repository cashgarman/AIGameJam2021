using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Agents
{
	public class BotcoinMiner : Agent<BotcoinMiner>
	{
		public override string Name => "Botcoin Miner";
		
		[SerializeField] private Transform _pluginSlot;
		
		public override Transform PluginSlot => _pluginSlot;
		public override BaseAgent PluggedInto { get; set; }
		public override float AlreadyVisitedCooldownMultiplier => 30f;

		[Header("Energy")]
		[SerializeField] private float _maxBatteryLevel;
		[SerializeField] private float _batteryDrainPerSecond;
		[SerializeField] private float _miningDrainPerSecond;
		[SerializeField] private float _batteryLevelOverride;
		[SerializeField] private float _minBatteryLevelToReenable;
		[SerializeField] private Slider _energyDisplay;
		[SerializeField] private float _lowChargePercent;
		[SerializeField] private float _highChargePercent;
		[SerializeField] private float _energyToMineACoin;

		private float _batteryLevel;
		private float _energyLeftToMineNextCoin;
		
		[Header("Coins")]
		[SerializeField] private Botcoin _coinPrefab;
		[SerializeField] private Transform _coinSpawnPoint;
		[SerializeField] private float _coinThrowImpulse = 10f;
		[SerializeField] private float _coinThrowSpread = 15f;

		public float BatteryLevel
		{
			get => _batteryLevel;
			set
			{
				_batteryLevel = Mathf.Clamp(value, 0f, _maxBatteryLevel);
				_energyDisplay.value = _batteryLevel / _maxBatteryLevel;
				
				// If the battery has run out
				if (_batteryLevel <= 0f)
				{
					// Disable the miner
					OnDisabled();
				}
            
				// If the miner was disabled but now has power
				if (Disabled && _batteryLevel > _minBatteryLevelToReenable)
				{
					// Enable the minder
					OnEnabled();
				}
			}
		}
		
		public bool IsLow => _batteryLevel / _maxBatteryLevel < _lowChargePercent;
		public bool IsHigh => _batteryLevel / _maxBatteryLevel > _highChargePercent;

		protected override void Awake()
		{
			base.Awake();
			
			BatteryLevel = _batteryLevelOverride != 0 ? _batteryLevelOverride : _maxBatteryLevel;
			
			// Reset the energy to mine the next coin
			_energyLeftToMineNextCoin = _energyToMineACoin;
		}

		protected override void Update()
		{
			base.Update();
			
			// If the object plugged in is a battery
			if (PluggedInto is Battery battery)
			{
				// Transfer battery power from the battery to the miner
				var potentialEnergyTransfer = _batteryDrainPerSecond * Time.deltaTime;				// Maximum potential energy transfer
				var energyToTransfer = Mathf.Min(battery.Charge, potentialEnergyTransfer);          // Make sure not to over-discharge the battery
				energyToTransfer = Mathf.Min(energyToTransfer, _maxBatteryLevel - _batteryLevel);   // Make sure not to over-charge the robot
            
				// Transfer the energy
				battery.Charge -= energyToTransfer;
				BatteryLevel += energyToTransfer;
				
				// If the battery is empty
				if (battery.IsEmpty)
				{
					// Eject it
					UnplugFromSelf(battery);
				}
			}
			
			// If the miner is enabled
			if (!Disabled)
			{
				// Drain the miner's battery
				BatteryLevel -= _miningDrainPerSecond * Time.deltaTime;
				
				// Mine the next coin
				_energyLeftToMineNextCoin -= _miningDrainPerSecond * Time.deltaTime;
				
				// If a coin has been mined
				if (_energyLeftToMineNextCoin <= 0)
				{
					// Produce a botcoin!
					SpitOutCoin();
					
					// Reset the energy to mine the next coin
					_energyLeftToMineNextCoin = _energyToMineACoin;
				}
			}
		}
		
		private void SpitOutCoin()
		{
			// Create a new coin
			var coin = Instantiate(_coinPrefab, _coinSpawnPoint.position, Quaternion.Euler(Random.onUnitSphere));
			coin.ThrowFromTransform(_coinSpawnPoint, _coinThrowSpread, _coinThrowImpulse);
		}

		public override bool FilterBy(string filter, IComputer computer)
		{
			return filter switch
			{
				"pluggedin" => PluggedInto != null,
				"unplugged" => PluggedInto == null,
				"empty" => IsLow,
				"full" => IsHigh,
				_ => base.FilterBy(filter, computer)
			};
		}

		private void OnDisabled()
		{
			Debug.Log($"{Name} was disabled from lack of power");
			Disabled = true;
		}

		private void OnEnabled()
		{
			Debug.Log($"{Name} was re-enabled after being recharged");
			Disabled = false;
		}
	}
}