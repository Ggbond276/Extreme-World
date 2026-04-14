using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public Collider miniMapBoundingBox;
    void Start()
    {
        MinimapManager.Instance.UpdateMiniMap(this.miniMapBoundingBox);
    }
    void Update()
    {
        
    }
}
