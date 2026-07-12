using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PickupOrb : MonoBehaviour
{
    public enum OrbType { Health, Ammo }

    [SerializeField] private OrbType orbType = OrbType.Health;
    [SerializeField] private float healAmount = 0f; // 0 = full heal, matching FillAmmunition(0)'s full-refill behavior
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;

        if (orbType == OrbType.Health)
        {
            playerHealth.Heal(healAmount);
        }
        else
        {
            // Only fill active (equipped) weapons. Holstered weapons are inactive and have
            // never run Start(), so their internal magazine reference is still null -
            // calling FillAmmunition on them dereferences null and hard-crashes on WebGL.
            // They initialize to full ammo when first equipped anyway.
            var weapons = playerHealth.GetComponentsInChildren<WeaponBehaviour>(false);
            foreach (var weapon in weapons)
            {
                if (!weapon.gameObject.activeInHierarchy) continue;
                weapon.FillAmmunition(0);
            }
        }

        Destroy(gameObject);
    }
}
