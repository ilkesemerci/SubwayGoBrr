using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Analytics;

public class TrackManager : MonoBehaviour
{
    [Header("Track Settings")]
    [SerializeField] private GameObject[] _trackPrefabs;
    [SerializeField] private float _trackLength = 30f;
    [SerializeField] private int _numberOfTracks = 5;

    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    private List<GameObject> activeTracks = new List<GameObject>();
    private float spawnZ = 0f;
    private float safeZone = 60f;

    private int previousTrack;
    

    private void Start()
    {
        // Spawn a safe starting area first
        SpawnTrack(0);

        for (int i = 0; i < _numberOfTracks; i++)
        { 
            // then random tracks 
            SpawnTrack(Random.Range(1, _trackPrefabs.Length));
        }
    }

    private void Update()
    {
        // Check if the player has moved far enough to need a new track
        if (_playerTransform.position.z - safeZone > (spawnZ - _numberOfTracks * _trackLength))
        {
            SpawnTrack(Random.Range(1, _trackPrefabs.Length));
            DeleteOldestTrack();
        }
    }

    private void SpawnTrack(int prefabIndex)
    {
        if((prefabIndex != 0) && (prefabIndex == previousTrack))
        {
            prefabIndex = (prefabIndex + 1) % _trackPrefabs.Length;
            if(prefabIndex == 0) prefabIndex++;
        }

        GameObject go = Instantiate(_trackPrefabs[prefabIndex], transform.forward * spawnZ, transform.rotation);
        
        activeTracks.Add(go);
        
        spawnZ += _trackLength;

        previousTrack = prefabIndex;
    }

    private void DeleteOldestTrack()
    {
        Destroy(activeTracks[0]);
        
        activeTracks.RemoveAt(0);
    }

}
