using UnityEngine;
using UnityEngine.UI;

namespace MGUI
{
    public class TransparentGraphic : MaskableGraphic
    {
        public override void SetMaterialDirty()
        {

        }

        public override void SetVerticesDirty()
        {

        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            return null;
        }

        // 确保在UI中不做渲染
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            //vh.Clear();
        }
    }
}