using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour {

    private CharacterController[] enemies;
    private Vector3[] enemyPositions;
    private bool[] enemyAlive;
    private Spawner spawner;
    private PickUp[] coins;

    void Start() {
        Setup();
    }

    public void Setup() {
        enemies = transform.Find("Enemies").GetComponentsInChildren<CharacterController>();
        enemyPositions = new Vector3[enemies.Length];
        enemyAlive = new bool[enemies.Length];
        for (int i = 0; i < enemies.Length; i++) {
            enemyPositions[i] = enemies[i].transform.position;
            enemies[i].gameObject.SetActive(false);
        }
        GameManager.script.hans.gameObject.SetActive(false);
        spawner = transform.Find("PlayerSpawners/Spawner").GetComponent<Spawner>();
        coins = transform.Find("Coins").GetComponentsInChildren<PickUp>();
    }

    public void ResetLevel() {
        for (int i = 0; i < enemies.Length; i++) {
            enemyAlive[i] = true;
            enemies[i].gameObject.SetActive(true);
            enemies[i].Setup(i, enemyPositions[i]);
        }
        for (int i = 0; i < coins.Length; i++) {
            coins[i].Setup();
        }
        GameManager.script.hans.gameObject.SetActive(true);
        GameManager.script.hans.Setup(-1, spawner.Spawn());
    }

    public void KillCharacter(int id) {
        if (id > -1) {
            enemyAlive[id] = false;
            enemies[id].gameObject.SetActive(false);
            if (CheckVictory()) {
                GameManager.script.StartTMGoRoutine(HEADERTYPE.win);
                GameManager.script.hans.Win();
            }
        } else {
            GameManager.script.StartTMGoRoutine(HEADERTYPE.death);
        }
    }

    bool CheckVictory() {
        for (int i = 0; i < enemyAlive.Length; i++) {
            if (enemyAlive[i]) {
                return false;
            }
        }
        return true;
    }

}
