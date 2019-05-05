using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour {

    [SerializeField] private PICKUPTYPE pickUpType;
    private Collider c;
    private GameObject graphics;
    private ParticleSystem ps;

    public void Setup() {
        if (c == null) {
            c = GetComponent<Collider>();
        }
        if (graphics == null) {
            graphics = transform.Find("Graphics").gameObject;
        }
        c.enabled = true;
        graphics.SetActive(true);
        switch (pickUpType) {
            case PICKUPTYPE.coin:
                if (ps == null) {
                    ps = transform.Find("PFX_Coin").GetComponent<ParticleSystem>();
                }
                break;
        }
    }

    public PICKUPTYPE Collect() {
        c.enabled = false;
        graphics.SetActive(false);
        ps.Play();
        return pickUpType;
    }

}
