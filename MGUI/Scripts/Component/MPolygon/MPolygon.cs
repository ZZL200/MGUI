using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class MPolygon : MaskableGraphic
{
    [Header("几边形")] [Range(3, 100)] public int sides = 3; // 边数
    public float radius = 100f; // 半径（像素）
    public float maxValue = 10f; // 每个半径上数值的最大值
    public List<float> dataValues = new List<float>(); // 每个半径上数值

    List<UIVertex> vertices = new List<UIVertex>();

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        vertices.Clear();
        // 确保数据有效
        if (sides < 3 || dataValues == null || dataValues.Count != sides)
        {
            return;
        }

        Vector3 center = Vector3.zero;

        // 绘制
        DrawPolygon(vh, center);
    }

    // 绘制数据多边形
    private void DrawPolygon(VertexHelper vh, Vector3 center)
    {
        int vertexCount = sides;

        // 创建顶点
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 position = GetPoint(center, i);
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertices.Add(vertex);
        }

        // 添加顶点
        int startIndex = vh.currentVertCount;
        for (int i = 0; i < vertices.Count; i++)
        {
            vh.AddVert(vertices[i]);
        }

        // 添加中心点
        UIVertex centerVertex = UIVertex.simpleVert;
        centerVertex.position = center;
        //centerVertex.color = color * 0.5f;
        centerVertex.color = color;
        vh.AddVert(centerVertex);
        int centerIndex = startIndex + vertices.Count;

        // 创建三角形（三角扇）
        for (int i = 0; i < vertexCount; i++)
        {
            int nextIndex = (i + 1) % vertexCount;
            vh.AddTriangle(
                centerIndex,
                startIndex + i,
                startIndex + nextIndex
            );
        }
    }

    //平分360度，获取每条线上的顶点
    private Vector3 GetPoint(Vector3 center, int index)
    {
        float value = Mathf.Clamp(dataValues[index], 0, maxValue);
        float pointRadius = radius * (value / maxValue);
        float angle = 90f + index * (-360f / sides);
        float angleRadians = angle * Mathf.Deg2Rad;

        return center + new Vector3(
            pointRadius * Mathf.Cos(angleRadians),
            pointRadius * Mathf.Sin(angleRadians),
            0
        );
    }

    // 设置维度数量
    public void SetSides(int count)
    {
        sides = Mathf.Clamp(count, 3, 100);

        while (dataValues.Count < sides)
        {
            dataValues.Add(maxValue);
        }

        while (dataValues.Count > sides)
        {
            dataValues.RemoveAt(dataValues.Count - 1);
        }

        SetVerticesDirty();
    }

    //设置某个顶点的位置,dimension从0开始正上方得点，顺时针
    public void SetAxisValue(int dimension, float value)
    {
        if (dimension < 0 || dimension >= sides)
        {
            return;
        }

        value = Mathf.Clamp(value, 0, maxValue);

        while (dataValues.Count <= dimension)
        {
            dataValues.Add(value);
        }

        dataValues[dimension] = value;
        SetVerticesDirty();
    }
}