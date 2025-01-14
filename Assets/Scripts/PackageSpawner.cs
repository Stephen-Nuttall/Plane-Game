using System.Collections;
using UnityEngine;

public class PackageSpawner : MonoBehaviour
{
    [SerializeField] float timeBetweenSpawns = 10f;
    [SerializeField] float initialSpawnDelay = 5f;
    BalloonPackage currentPackage;
    bool readyToSpawn = false;

    void Start()
    {
        StartCoroutine(WaitToSpawn(initialSpawnDelay));
    }

    void Update()
    {
        // wait until current package is claimed to spawn another
        if (readyToSpawn && !currentPackage.gameObject.activeInHierarchy)
        {
            StartCoroutine(WaitToSpawn(timeBetweenSpawns));
        }
    }

    void SpawnPackage()
    {
        int randNum = Random.Range(0, transform.childCount);
        currentPackage = transform.GetChild(randNum).GetComponent<BalloonPackage>();
        currentPackage.gameObject.SetActive(true);
        readyToSpawn = true;
    }

    IEnumerator WaitToSpawn(float seconds)
    {
        readyToSpawn = false;
        yield return new WaitForSeconds(seconds);
        SpawnPackage();
    }
}
