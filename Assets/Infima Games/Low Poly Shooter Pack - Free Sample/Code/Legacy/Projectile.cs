using System;
using UnityEngine;
using System.Collections;
using InfimaGames.LowPolyShooterPack;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {

	[Range(5, 100)]
	[Tooltip("After how long time should the bullet prefab be destroyed?")]
	public float destroyAfter;
	[Tooltip("If enabled the bullet destroys on impact")]
	public bool destroyOnImpact = false;
	[Tooltip("Minimum time after impact that the bullet is destroyed")]
	public float minDestroyTime;
	[Tooltip("Maximum time after impact that the bullet is destroyed")]
	public float maxDestroyTime;

	[Header("Impact Effect Prefabs")]
	public Transform [] bloodImpactPrefabs;
	public Transform [] metalImpactPrefabs;
	public Transform [] dirtImpactPrefabs;
	public Transform []	concreteImpactPrefabs;

	[Header("Epic Toon FX Integration")]
	public GameObject[] toonTextImpactPrefabs;
	public GameObject[] toonEnvironmentalImpactPrefabs;

	private void Start ()
	{
		//Grab the game mode service, we need it to access the player character!
		var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		//Ignore the main player character's collision. A little hacky, but it should work.
		Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());
		
		//Start destroy timer
		StartCoroutine (DestroyAfter ());
	}

	private void SpawnImpact(Collision collision, bool isEnemy)
	{
		if (collision.contacts.Length == 0) return;
		
		var contact = collision.contacts[0];
		var normal = contact.normal;
		var rotation = Quaternion.LookRotation(normal);
		var position = contact.point;

		if (isEnemy)
		{
			if (toonTextImpactPrefabs != null && toonTextImpactPrefabs.Length > 0)
			{
				Instantiate(toonTextImpactPrefabs[Random.Range(0, toonTextImpactPrefabs.Length)], position, rotation);
			}
		}
		else
		{
			if (toonEnvironmentalImpactPrefabs != null && toonEnvironmentalImpactPrefabs.Length > 0)
			{
				Instantiate(toonEnvironmentalImpactPrefabs[Random.Range(0, toonEnvironmentalImpactPrefabs.Length)], position, rotation);
			}
		}
	}

	//If the bullet collides with anything
	private void OnCollisionEnter (Collision collision)
	{
		//Ignore collisions with other projectiles.
		if (collision.gameObject.GetComponent<Projectile>() != null)
			return;

		//If bullet collides with an enemy
		var enemyAI = collision.gameObject.GetComponentInParent<EnemyAI>();
		if (enemyAI != null) 
		{
			enemyAI.TakeDamage(25f);
			SpawnImpact(collision, true);

			// Award player a mid-air jump boost!
			var playerChar = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
			if (playerChar != null)
			{
				var mv = playerChar.GetComponent<Movement>();
				if (mv != null) mv.AddJumpBoost();
			}

			Destroy(gameObject);
			return;
		}
		//If destroy on impact is false, start 
		//coroutine with random destroy timer
		if (!destroyOnImpact) 
		{
			StartCoroutine (DestroyTimer ());
		}
		//Otherwise, destroy bullet on impact
		else 
		{
			Destroy (gameObject);
		}

		if (collision.transform.CompareTag("Target")) 
		{
			var target = collision.transform.gameObject.GetComponent<TargetScript>();
			if (target != null) target.isHit = true;
		}
		else if (collision.transform.CompareTag("ExplosiveBarrel")) 
		{
			var barrel = collision.transform.gameObject.GetComponent<ExplosiveBarrelScript>();
			if (barrel != null) barrel.explode = true;
		}
		else if (collision.transform.CompareTag("GasTank")) 
		{
			var tank = collision.transform.gameObject.GetComponent<GasTankScript>();
			if (tank != null) tank.isHit = true;
		}
		else 
		{
			SpawnImpact(collision, false);
		}
	}

	private IEnumerator DestroyTimer () 
	{
		//Wait random time based on min and max values
		yield return new WaitForSeconds
			(Random.Range(minDestroyTime, maxDestroyTime));
		//Destroy bullet object
		Destroy(gameObject);
	}

	private IEnumerator DestroyAfter () 
	{
		//Wait for set amount of time
		yield return new WaitForSeconds (destroyAfter);
		//Destroy bullet object
		Destroy (gameObject);
	}
}