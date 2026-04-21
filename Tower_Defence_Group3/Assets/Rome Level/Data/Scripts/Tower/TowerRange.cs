using UnityEngine;

public class TowerRange : MonoBehaviour
{
    [SerializeField] GameObject rangeIndicator;
    public PlayerTrack player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rangeIndicator == null)
            return;
        float diameter = player.maxDistance * 2f;
        rangeIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);
    }
}
