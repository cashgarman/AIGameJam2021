using UnityEngine;

public interface IPickup
{
	bool PickedUp { get; }
	Transform transform { get; }	// HACK: Is this too hacky a way to handle getting at the transform?
	string name { get; }
}