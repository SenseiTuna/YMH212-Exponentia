/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.6.0
 * BUILD_DATE: 2026-05-18
 * BUILD_TIME: 20:40
 * DESCRIPTION: Chest etkilesimi; acilinca odul spawn eder ve tekrar acilmaz.
 */

using Exponentia.Interaction;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class ChestInteractable : MonoBehaviour, IInteractable
{
    [Header("Chest")]
    [SerializeField] private string interactionLabel = "Open Chest";
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool opened;

    [Header("Visuals")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;

    [Header("Rewards")]
    [SerializeField] private GameObject[] rewardPrefabs;
    [SerializeField] private Transform[] rewardSpawnPoints;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;

    private void Start()
    {
        RefreshVisuals();
    }

    public Vector3 GetInteractionPoint()
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public string GetInteractionLabel()
    {
        return opened ? "Opened" : interactionLabel;
    }

    public bool CanInteract(GameObject interactor)
    {
        return !opened;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        opened = true;
        // Turkish: Chest acilisinda odul spawn + event + görsel geçis tek noktada yapiliyor.
        SpawnRewards();
        RefreshVisuals();
        onOpened?.Invoke();
    }

    private void SpawnRewards()
    {
        if (rewardPrefabs == null || rewardPrefabs.Length == 0)
        {
            return;
        }

        if (rewardSpawnPoints == null || rewardSpawnPoints.Length == 0)
        {
            for (int i = 0; i < rewardPrefabs.Length; i++)
            {
                if (rewardPrefabs[i] != null)
                {
                    Instantiate(rewardPrefabs[i], transform.position, Quaternion.identity);
                }
            }
            return;
        }

        int count = Mathf.Min(rewardPrefabs.Length, rewardSpawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (rewardPrefabs[i] == null || rewardSpawnPoints[i] == null)
            {
                continue;
            }

            Instantiate(rewardPrefabs[i], rewardSpawnPoints[i].position, rewardSpawnPoints[i].rotation);
        }
    }

    private void RefreshVisuals()
    {
        if (closedVisual != null)
        {
            closedVisual.SetActive(!opened);
        }

        if (openedVisual != null)
        {
            openedVisual.SetActive(opened);
        }
    }
}