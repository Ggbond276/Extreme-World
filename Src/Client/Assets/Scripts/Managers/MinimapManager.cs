using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    class MinimapManager : Singleton<MinimapManager>
    {
        public UIMiniMap minimap;
        private Collider miniMapBoundingBox;
        public Collider MiniMapBoundingBox
        {
            get
            {
                return miniMapBoundingBox;
            }
        }
        public Transform PlayerTransform
        {
            get
            {
                if (User.Instance.CurrentCharacterObject == null) 
                    return null;
                return User.Instance.CurrentCharacterObject.transform;
            }
        }
        public Sprite LoadCurrentMinimap()
        {
            return Resloader.Load<Sprite>("UI/Minimap/" + User.Instance.CurrentMapData.MiniMap);
        }
        public void UpdateMiniMap(Collider miniMapBoundingBox)
        {
            this.miniMapBoundingBox = miniMapBoundingBox;
            if (minimap != null)
                this.minimap.UpdateMap();
        }
    }
}
