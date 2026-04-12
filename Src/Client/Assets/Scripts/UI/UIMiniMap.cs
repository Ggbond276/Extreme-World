using Managers;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMiniMap : MonoBehaviour
{
    //1. 地图
    public Image minimap;
    //2. 箭头
    public Image arrow;
    //3. 地图名字
    public Text mapName;
    //4. 获取地图的长宽高数据
    public Collider minimapBouingBox;
    //5. 当前角色的位置
    public Transform playerTransform;
    //6. 箭头
    public Image Arrow;
    
    void Start()
    {
        this.InitMap();
    }
    void InitMap()
    {
        this.mapName.text = User.Instance.CurrentMapData.Name;
        // User.Instance.CurrentMapData.MiniMap
        if(this.minimap.overrideSprite == null)
            this.minimap.overrideSprite = MinimapManager.Instance.LoadCurrentMinimap();
        // 由于不同地图尺寸不一样所以要做一个适应
        this.minimap.SetNativeSize();
        this.minimap.transform.localPosition = Vector3.zero;
    }
    void Update()
    {
        if(this.playerTransform == null)
        {
            this.playerTransform = MinimapManager.Instance.PlayerTransform;
        }
        if (minimapBouingBox == null || playerTransform == null)
            return;
        float realWidth = minimapBouingBox.bounds.size.x;
        float realHeight = minimapBouingBox.bounds.size.z;

        float relaX = playerTransform.position.x - minimapBouingBox.bounds.min.x;
        float relaY = playerTransform.position.z - minimapBouingBox.bounds.min.z;

        float pivotX = relaX / realWidth;
        float pivotY = relaY / realHeight;

        this.minimap.rectTransform.pivot = new Vector2(pivotX, pivotY);
        this.minimap.rectTransform.localPosition = Vector2.zero;

        float playerYRotation = playerTransform.eulerAngles.y;
        this.Arrow.rectTransform.localEulerAngles = new Vector3(0, 0, -playerYRotation);
    }
}
