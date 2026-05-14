using UnityEngine;
using UnityEngine.UI;

namespace MGUI
{
    [AddComponentMenu("MGUI/UI/MRawImage")]
    public class MRawImage : RawImage
    {
        [Header("圆形显示")] [SerializeField] private bool isCircle = false;
        [SerializeField] [Range(3, 100)] private int circleSegments = 40;
        [SerializeField] [Range(0f, 5f)] private float circleFeather = 1f;

        [Header("旋转角度")] [SerializeField] private float angle = 0;

        [Header("翻转")] [SerializeField] private ImageFlip flip = ImageFlip.None;

        public ImageFlip Flip
        {
            get { return flip; }
            set
            {
                flip = value;
                UpdateGeometry();
            }
        }

        //旋转、翻转
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            if (isCircle) {
                GenerateCircleMesh(toFill);
            }

            if (angle % 360 != 0) {
                var uiVertex = new UIVertex();
                float radians = angle * Mathf.Deg2Rad;
                var sin = Mathf.Sin(radians);
                var cos = Mathf.Cos(radians);
                int verCount = toFill.currentVertCount;
                for (int i = 0; i < verCount; i++) {
                    toFill.PopulateUIVertex(ref uiVertex, i);
                    var pos = uiVertex.position;
                    uiVertex.position = new Vector3(cos * pos.x - sin * pos.y, sin * pos.x + cos * pos.y, pos.z);
                    toFill.SetUIVertex(uiVertex, i);
                }
            }

            if (flip == ImageFlip.Horizontal) {
                var rectCenter = rectTransform.rect.center;
                int verCount = toFill.currentVertCount;
                for (int i = 0; i < verCount; i++) {
                    var uiVertex = new UIVertex();
                    toFill.PopulateUIVertex(ref uiVertex, i);
                    var pos = uiVertex.position;
                    uiVertex.position = new Vector3(pos.x + (rectCenter.x - pos.x) * 2, pos.y, pos.z);
                    toFill.SetUIVertex(uiVertex, i);
                }
            }
            else if (flip == ImageFlip.Vertical) {
                var rectCenter = rectTransform.rect.center;
                int verCount = toFill.currentVertCount;
                for (int i = 0; i < verCount; i++) {
                    var uiVertex = new UIVertex();
                    toFill.PopulateUIVertex(ref uiVertex, i);
                    var pos = uiVertex.position;
                    uiVertex.position = new Vector3(pos.x, pos.y + (rectCenter.y - pos.y) * 2, pos.z);
                    toFill.SetUIVertex(uiVertex, i);
                }
            }
            else if (flip == ImageFlip.Both) {
                var rectCenter = rectTransform.rect.center;
                int verCount = toFill.currentVertCount;
                for (int i = 0; i < verCount; i++) {
                    var uiVertex = new UIVertex();
                    toFill.PopulateUIVertex(ref uiVertex, i);
                    var pos = uiVertex.position;
                    uiVertex.position = new Vector3(pos.x + (rectCenter.x - pos.x) * 2,
                        pos.y + (rectCenter.y - pos.y) * 2, pos.z);
                    toFill.SetUIVertex(uiVertex, i);
                }
            }
        }

        /// <summary>
        /// 生成圆形网格，通过顶点绘制三角形实现圆形显示，不使用Shader，不影响合批。
        /// 边缘添加一圈alpha渐变顶点实现抗锯齿。
        /// </summary>
        private void GenerateCircleMesh(VertexHelper toFill)
        {
            if (toFill.currentVertCount < 4) return;

            UIVertex vert = default;
            Vector3[] corners = new Vector3[4];
            Vector2[] uvCorners = new Vector2[4];

            for (int i = 0; i < 4; i++) {
                toFill.PopulateUIVertex(ref vert, i);
                corners[i] = vert.position;
                uvCorners[i] = vert.uv0;
            }

            Color32 color32 = color;
            Color32 clearColor = new Color32(color32.r, color32.g, color32.b, 0);

            float posMinX = corners[0].x;
            float posMaxX = corners[2].x;
            float posMinY = corners[0].y;
            float posMaxY = corners[1].y;

            float posCenterX = (posMinX + posMaxX) * 0.5f;
            float posCenterY = (posMinY + posMaxY) * 0.5f;
            float halfW = (posMaxX - posMinX) * 0.5f;
            float halfH = (posMaxY - posMinY) * 0.5f;
            float radius = Mathf.Min(halfW, halfH);

            float uvMinX = uvCorners[0].x;
            float uvMaxX = uvCorners[2].x;
            float uvMinY = uvCorners[0].y;
            float uvMaxY = uvCorners[1].y;

            float uvCenterX = (uvMinX + uvMaxX) * 0.5f;
            float uvCenterY = (uvMinY + uvMaxY) * 0.5f;
            float uvHalfW = (uvMaxX - uvMinX) * 0.5f * (radius / Mathf.Max(halfW, 0.001f));
            float uvHalfH = (uvMaxY - uvMinY) * 0.5f * (radius / Mathf.Max(halfH, 0.001f));

            // 抗锯齿：内圈半径略微收缩，外圈半径略微扩展，中间alpha渐变
            float feather = circleFeather;
            float innerRadius = radius - feather;
            float outerRadius = radius + feather;
            if (innerRadius < 0f) innerRadius = 0f;

            float innerUvScale = innerRadius / Mathf.Max(radius, 0.001f);
            float outerUvScale = outerRadius / Mathf.Max(radius, 0.001f);

            toFill.Clear();

            // 顶点0：中心
            UIVertex centerVertex = UIVertex.simpleVert;
            centerVertex.position = new Vector3(posCenterX, posCenterY, 0);
            centerVertex.color = color32;
            centerVertex.uv0 = new Vector2(uvCenterX, uvCenterY);
            toFill.AddVert(centerVertex);

            // 顶点 [1 ~ circleSegments+1]：内圈（不透明边缘）
            // 顶点 [circleSegments+2 ~ 2*(circleSegments+1)]：外圈（全透明边缘）
            float angleStep = 2f * Mathf.PI / circleSegments;
            for (int i = 0; i <= circleSegments; i++) {
                float a = i * angleStep;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                // 内圈顶点（不透明）
                UIVertex innerVert = UIVertex.simpleVert;
                innerVert.position = new Vector3(posCenterX + cos * innerRadius, posCenterY + sin * innerRadius, 0);
                innerVert.color = color32;
                innerVert.uv0 = new Vector2(uvCenterX + cos * uvHalfW * innerUvScale, uvCenterY + sin * uvHalfH * innerUvScale);
                toFill.AddVert(innerVert);

                // 外圈顶点（全透明）
                UIVertex outerVert = UIVertex.simpleVert;
                outerVert.position = new Vector3(posCenterX + cos * outerRadius, posCenterY + sin * outerRadius, 0);
                outerVert.color = clearColor;
                outerVert.uv0 = new Vector2(uvCenterX + cos * uvHalfW * outerUvScale, uvCenterY + sin * uvHalfH * outerUvScale);
                toFill.AddVert(outerVert);
            }

            int innerStart = 1;
            int outerStart = 2;
            int stride = 2; // 每个角度有2个顶点（内圈+外圈）

            for (int i = 0; i < circleSegments; i++) {
                int innerCurr = innerStart + i * stride;
                int innerNext = innerStart + (i + 1) * stride;
                int outerCurr = outerStart + i * stride;
                int outerNext = outerStart + (i + 1) * stride;

                // 内部实心三角形（中心 → 内圈当前 → 内圈下一个）
                toFill.AddTriangle(0, innerCurr, innerNext);

                // 边缘渐变带（内圈当前 → 外圈当前 → 外圈下一个）
                toFill.AddTriangle(innerCurr, outerCurr, outerNext);
                // 边缘渐变带（内圈当前 → 外圈下一个 → 内圈下一个）
                toFill.AddTriangle(innerCurr, outerNext, innerNext);
            }
        }

        public void Clear()
        {
            flip = ImageFlip.None;
            color = Color.clear;
            texture = null;
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }
    }
}