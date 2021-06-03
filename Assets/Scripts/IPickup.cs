using UnityEngine;

public interface IPickup
{
	bool IsPickedUp { get; set; }
	Transform transform { get; }	// HACK: Is this too hacky a way to handle getting at the transform?
	GameObject gameObject { get; }
	string name { get; }
	void OnPickedUp();
}