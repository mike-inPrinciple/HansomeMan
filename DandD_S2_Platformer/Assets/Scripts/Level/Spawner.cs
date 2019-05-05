using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour {

    private ParticleSystem ps;

    public Vector3 Spawn() {
        if (ps == null) {
            ps = transform.Find("PFX_Spawn").GetComponent<ParticleSystem>();
        }
        ps.Play();
        return transform.position;
    }

}
