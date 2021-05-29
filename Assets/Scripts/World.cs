using System;
using UnityEngine;

public class World : MonoBehaviour
{
	private void Update()
	{
		Time.timeScale = Input.GetKey(KeyCode.Tab) ? 20f : 1f;
	}
}
