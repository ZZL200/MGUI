using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MText : Text, IPointerClickHandler
{
    [Header("是否开启超链接")] [SerializeField] 
    private bool useLink = false;
    // 超链接开始结束index数据
    private List<Info> linkInfoList = new List<Info>();
    private Action<string> onLinkClick;

    [Header("是否开启横线")] [SerializeField] 
    private bool useLine = false;
    [Header("横线宽度")] [SerializeField] [Range(0f, 200f)] 
    private float lineHeight = 2f;
    [Header("横线位置")] [SerializeField] 
    private float lineOffset = 0;
    [Header("横线颜色")] [SerializeField] 
    private Color32 lineColor = Color.white;

    private UIVertex[] lineUIVertexs = new UIVertex[4];

    //下划线开始结束index数据
    private List<Info> lineInfoList = new List<Info>();

    private UICharInfo[] characters;

    private UILineInfo[] lines;

    // 可视的字符个数
    private int characterCount = 0;

    private Stack<Info> infoPool = new Stack<Info>();
    
    // 标签匹配结构
    private struct TagMatch
    {
        public int Index;          // 标签在原始文本中的位置
        public int ContentStart;   // 内容开始位置
        public int ContentLength;  // 内容长度
        public int TotalLength;    // 包含标签的总长度
    }

    public enum GradientType
    {
        Horizontal = 0,
        Vertical = 1,
    }

    [Header("是否开启渐变")] [SerializeField] private bool useGradient = false;
    [Header("渐变颜色1")] [SerializeField] private Color32 gradientColor1 = Color.white;
    [Header("渐变颜色2")] [SerializeField] private Color32 gradientColor2 = Color.white;
    [Header("渐变方向")] [SerializeField] private GradientType gradientType = GradientType.Vertical;

    [Header("渐变偏移")] [SerializeField] [Range(-1, 1)]
    private float gradientOffet = 0;

    [Header("是否开启阴影")] [SerializeField] private bool useShadow = false;
    [Header("阴影偏移")] [SerializeField] private Vector2 shadowOffset = Vector2.zero;
    [Header("阴影倾斜")] [SerializeField] private float shadowslant = 0;
    [Header("阴影颜色")] [SerializeField] private Color32 shadowColor = Color.white;

    [TextArea(3, 10)] [SerializeField] protected string _text = String.Empty;

    protected override void Awake()
    {
        base.Awake();
        text = _text;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    public bool UseLink
    {
        get { return useLink; }
        set
        {
            if (useLink == value)
            {
                return;
            }

            useLink = value;

            text = _text;

            if (useLink)
            {
            }
            else
            {
                linkInfoToPool();
            }
        }
    }

    public bool UseLine
    {
        get { return useLine; }
        set
        {
            if (useLine == value)
            {
                return;
            }

            useLine = value;

            text = _text;

            if (useLine)
            {

            }
            else
            {
                lineInfoToPool();
            }
        }
    }

    public bool UseGradient
    {
        get { return useGradient; }
        set
        {
            if (useGradient == value)
            {
                return;
            }

            useGradient = value;

            SetVerticesDirty();
            SetLayoutDirty();
        }
    }

    public bool UseShadow
    {
        get { return useShadow; }
        set
        {
            if (useShadow == value)
            {
                return;
            }

            useShadow = value;

            SetVerticesDirty();
            SetLayoutDirty();
        }
    }

    public override string text
    {
        get { return m_Text; }
        set
        {
            _text = value;
            if (useLine)
            {
                string input = _text;
                if (useLink)
                {
                    input = input.Replace("<k>", "");
                    input = input.Replace("</k>", "");
                }

                m_Text = getLineInfo(input);
            }
            else
            {
                m_Text = value;
            }

            if (useLink)
            {
                string input = _text;
                if (useLine)
                {
                    input = input.Replace("<e>", "");
                    input = input.Replace("</e>", "");
                }

                m_Text = getLinkInfo(input);
            }

            SetVerticesDirty();
            SetLayoutDirty();
        }
    }

    private string getLineInfo(string input)
    {
        lineInfoToPool();

        //下划线判断
        var matches = FindTags(input, "<e>", "</e>");
        int len = matches.Count;
        if (len > 0)
        {
            for (int i = 0; i < len; i++)
            {
                Info info;
                if (infoPool.Count > 0)
                {
                    info = infoPool.Pop();
                }
                else
                {
                    info = new Info();
                }

                var r = matches[i];
                info.StartIndex = r.Index - i * 7;
                info.EndIndex = info.StartIndex + r.ContentLength - 1;
                info.StringValue = replaceRichText(input.Substring(r.ContentStart, r.ContentLength));
                info.rect = default;
                lineInfoList.Add(info);
            }

            input = input.Replace("<e>", "");
            input = input.Replace("</e>", "");
            return input;
        }

        return input;
    }

    private string getLinkInfo(string input)
    {
        linkInfoToPool();

        //超链接判断
        var matches = FindTags(input, "<k>", "</k>");
        int len = matches.Count;
        if (len > 0)
        {
            for (int i = 0; i < len; i++)
            {
                Info info;
                if (infoPool.Count > 0)
                {
                    info = infoPool.Pop();
                }
                else
                {
                    info = new Info();
                }

                var r = matches[i];
                info.StartIndex = r.Index - i * 7;
                info.EndIndex = info.StartIndex + r.ContentLength - 1;
                info.StringValue = replaceRichText(input.Substring(r.ContentStart, r.ContentLength));
                info.rect = default;
                linkInfoList.Add(info);
            }

            input = input.Replace("<k>", "");
            input = input.Replace("</k>", "");
        }

        return input;
    }
    
    // 查找所有指定标签
    private List<TagMatch> FindTags(string text, string openTag, string closeTag)
    {
        var matches = new List<TagMatch>();
        int searchStart = 0;
            
        while (searchStart < text.Length)
        {
            int openIndex = text.IndexOf(openTag, searchStart);
            if (openIndex < 0) break;
                
            int contentStart = openIndex + openTag.Length;
                
            int closeIndex = text.IndexOf(closeTag, contentStart);
            if (closeIndex < 0) break;
                
            matches.Add(new TagMatch
            {
                Index = openIndex,
                ContentStart = contentStart,
                ContentLength = closeIndex - contentStart,
                TotalLength = closeIndex + closeTag.Length - openIndex
            });
                
            searchStart = closeIndex + closeTag.Length;
        }
            
        return matches;
    }

    //换掉富文本
    private string replaceRichText(string str)
    {
        str = str.Replace("<e>", "");
        str = str.Replace("</e>", "");
        str = str.Replace("<k>", "");
        str = str.Replace("</k>", "");
        str = str.Replace("<b>", "");
        str = str.Replace("</b>", "");
        str = str.Replace("<i>", "");
        str = str.Replace("</i>", "");
        str = str.Replace("<size>", "");
        str = str.Replace("</size>", "");
        str = RemoveColorTags(str);
        str = str.Replace("</color>", "");
        str = str.Replace("\n", "");
        str = str.Replace("\t", "");
        str = str.Replace("\r", "");
        return str;
    }
    
    // 手动移除 color 标签
    private string RemoveColorTags(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
            
        var result = new StringBuilder(str.Length);
        int pos = 0;
            
        while (pos < str.Length)
        {
            int colorStart = str.IndexOf("<color=", pos);
            if (colorStart < 0)
            {
                result.Append(str.Substring(pos));
                break;
            }
                
            result.Append(str.Substring(pos, colorStart - pos));
                
            int colorEnd = str.IndexOf('>', colorStart);
            if (colorEnd < 0) break;
                
            pos = colorEnd + 1;
        }
            
        return result.ToString();
    }

    private void lineInfoToPool()
    {
        int len = lineInfoList.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var info = lineInfoList[i];
            infoPool.Push(info);
            lineInfoList.Remove(info);
        }

        lineInfoList.Clear();
    }

    private void linkInfoToPool()
    {
        int len = linkInfoList.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var info = linkInfoList[i];
            infoPool.Push(info);
            linkInfoList.Remove(info);
        }

        linkInfoList.Clear();
    }

    public void AddHyperLinkListener(Action<string> callBack)
    {
        onLinkClick = callBack;
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        int len = linkInfoList.Count;
        if (len > 0 && onLinkClick != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                eventData.pressEventCamera, out var lp);

            for (int i = 0; i < len; i++)
            {
                var lineInfo = linkInfoList[i];
                if (lineInfo.rect.Contains(lp))
                {
                    onLinkClick?.Invoke(lineInfo.StringValue);
                    return;
                }
            }
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);

        // 当宽或高足够小则不处理
        if (rectTransform.rect.size.x <= 0 || rectTransform.rect.size.y <= 0 || vh.currentVertCount <= 0) return;

        UIVertex vertex = UIVertex.simpleVert;

        if (useShadow)
        {
            int len = vh.currentVertCount;
            for (int i = 0; i < len; i += 4)
            {
                vh.PopulateUIVertex(ref vertex, i);
                lineUIVertexs[0] = vertex;
                vertex.position += new Vector3(shadowOffset.x + shadowslant, shadowOffset.y, 0);
                vertex.color = shadowColor;
                vh.SetUIVertex(vertex, i);

                vh.PopulateUIVertex(ref vertex, i + 1);
                lineUIVertexs[1] = vertex;
                vertex.position += new Vector3(shadowOffset.x + shadowslant, shadowOffset.y, 0);
                vertex.color = shadowColor;
                vh.SetUIVertex(vertex, i + 1);

                vh.PopulateUIVertex(ref vertex, i + 2);
                lineUIVertexs[2] = vertex;
                vertex.position += new Vector3(shadowOffset.x, shadowOffset.y, 0);
                vertex.color = shadowColor;
                vh.SetUIVertex(vertex, i + 2);

                vh.PopulateUIVertex(ref vertex, i + 3);
                lineUIVertexs[3] = vertex;
                vertex.position += new Vector3(shadowOffset.x, shadowOffset.y, 0);
                vertex.color = shadowColor;
                vh.SetUIVertex(vertex, i + 3);

                vh.AddUIVertexQuad(lineUIVertexs);
            }
        }

        if (useGradient) //渐变
        {
            int startIndex = 0;
            int endIndex = 0;
            if (useShadow)
            {
                startIndex = vh.currentVertCount / 2;
                endIndex = vh.currentVertCount;
            }
            else
            {
                endIndex = vh.currentVertCount;
            }

            if (gradientType == GradientType.Vertical)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    vh.PopulateUIVertex(ref vertex, i);
                    if (i % 4 == 0 || i % 4 == 1)
                    {
                        vertex.color = Color32.Lerp(gradientColor1, gradientColor2, -gradientOffet);
                    }
                    else
                    {
                        vertex.color = Color32.Lerp(gradientColor2, gradientColor1, gradientOffet);
                    }

                    vh.SetUIVertex(vertex, i);
                }
            }
            else
            {
                float preferred_Width = preferredWidth;
                float rect_width = rectTransform.rect.width;
                float width = rect_width;
                float centerOffsetX = 0;
                if (horizontalOverflow == HorizontalWrapMode.Overflow)
                {
                    width = preferred_Width;
                    if (preferred_Width > rect_width)
                    {
                        if (alignment == TextAnchor.LowerLeft || alignment == TextAnchor.MiddleLeft ||
                            alignment == TextAnchor.UpperLeft)
                        {
                            centerOffsetX = -(preferred_Width - rect_width) * 0.5f;
                        }
                        else if (alignment == TextAnchor.LowerRight || alignment == TextAnchor.MiddleRight ||
                                 alignment == TextAnchor.UpperRight)
                        {
                            centerOffsetX = (preferred_Width - rect_width) * 0.5f;
                        }
                    }
                    else if (preferred_Width < rect_width)
                    {
                        if (alignment == TextAnchor.LowerLeft || alignment == TextAnchor.MiddleLeft ||
                            alignment == TextAnchor.UpperLeft)
                        {
                            centerOffsetX = (rect_width - preferred_Width) * 0.5f;
                        }
                        else if (alignment == TextAnchor.LowerRight || alignment == TextAnchor.MiddleRight ||
                                 alignment == TextAnchor.UpperRight)
                        {
                            centerOffsetX = -(rect_width - preferred_Width) * 0.5f;
                        }
                    }
                }
                else
                {
                    width = rect_width > preferred_Width ? preferred_Width : rect_width;

                    if (preferred_Width < rect_width)
                    {
                        if (alignment == TextAnchor.LowerLeft || alignment == TextAnchor.MiddleLeft ||
                            alignment == TextAnchor.UpperLeft)
                        {
                            centerOffsetX = (rect_width - preferred_Width) * 0.5f;
                        }
                        else if (alignment == TextAnchor.LowerRight || alignment == TextAnchor.MiddleRight ||
                                 alignment == TextAnchor.UpperRight)
                        {
                            centerOffsetX = -(rect_width - preferred_Width) * 0.5f;
                        }
                    }
                }

                for (int i = startIndex; i < endIndex; i++)
                {
                    vh.PopulateUIVertex(ref vertex, i);
                    var offet = (vertex.position.x + centerOffsetX) / width + 0.5f -
                                gradientOffet; //+0.5是为了将offset大概约束到 -1到1 这个范围内
                    vertex.color = Color32.Lerp(gradientColor1, gradientColor2, offet);
                    vh.SetUIVertex(vertex, i);
                }
            }
        }

        if (useLine || useLink)
        {
            characters = cachedTextGenerator.GetCharactersArray();
            lines = cachedTextGenerator.GetLinesArray();
            //显示的字符数量
            characterCount = cachedTextGenerator.characterCount;
        }

        if (useLine && characterCount > 0) //横线
        {
            //顶点需要uv信息,如果直接使用(0,0),(0,1)做uv的映射,显示出来的是整张font纹理,所以注册一个字符，取这个字符的uv
            font.RequestCharactersInTexture("-", fontSize, fontStyle);

            int len = lineInfoList.Count;
            if (len > 0)
            {
                drawCustomLine(vh); //根据富文本标识画横线
            }
            else
            {
                drawAllLine(vh); //开启横线，但是没有富文本标识，那么就给每行文字都画横线
            }
        }

        if (useLink && characterCount > 0) //超链接
        {
            int len = linkInfoList.Count;
            if (len > 0)
            {
                getCustomLinkInfo(vh); //根据富文本标识获取超链接
            }
            else
            {
                getAllLinkInfo(vh); //开启超连接，但是没有富文本标识，那么就整个文本是一个超链接
            }
        }
    }

    /// 从font纹理中获取指定字符的uv
    private Vector2 getCharUV()
    {
        CharacterInfo info;
        if (font.GetCharacterInfo('-', out info, fontSize, fontStyle))
        {
            return (info.uvBottomLeft + info.uvBottomRight + info.uvTopLeft + info.uvTopRight) * 0.25f;
        }

        return Vector2.zero;
    }

    // 显示自定义起止点下划线
    private void drawCustomLine(VertexHelper vh)
    {
        var uv0 = getCharUV();

        var charsMaxIndex = characterCount - 1;

        for (int i = 0; i < lineInfoList.Count; i++)
        {
            var underLineInfo = lineInfoList[i];
            var startIndex = underLineInfo.StartIndex;
            var endIndex = underLineInfo.EndIndex;

            if (startIndex < 0) startIndex = 0;
            if (endIndex > charsMaxIndex) endIndex = charsMaxIndex;

            if (startIndex >= characterCount) continue;

            var lineIndex0 = getCharInLineIndex(startIndex);
            var lineIndex1 = getCharInLineIndex(endIndex);
            if (lineIndex0 != lineIndex1)
            {
                // 跨行
                for (int j = lineIndex0; j <= lineIndex1; j++)
                {
                    var lineStartCharIndex = startIndex;
                    var lineEndCharIndex = endIndex;
                    if (j == lineIndex0)
                    {
                        // 第一行,从指定起始字索引到改行末尾字索引(改行末尾索引是下一行的起始索引-1得到)
                        lineEndCharIndex = lines[j + 1].startCharIdx - 1;
                    }
                    else if (j == lineIndex1)
                    {
                        // 最后一行,从改行起始字索引到指定字索引
                        lineStartCharIndex = lines[j].startCharIdx;
                    }
                    else
                    {
                        // 中间行,从改行起始字所以到该行末尾字索引
                        lineStartCharIndex = lines[j].startCharIdx;
                        lineEndCharIndex = lines[j + 1].startCharIdx - 1;
                    }

                    addUIVertexQuad(vh, lines[j], lineStartCharIndex, lineEndCharIndex, uv0);
                }
            }
            else
            {
                // 在同一行
                addUIVertexQuad(vh, lines[lineIndex0], startIndex, endIndex, uv0);
            }
        }
    }

    // 显示所有下划线
    private void drawAllLine(VertexHelper vh)
    {
        var uv0 = getCharUV();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var endIndex = 0;
            if (i + 1 < lines.Length)
            {
                // 通过下一行的起始索引减1得到这一行最后一个字符索引位置
                var nextLineStartCharIdx = lines[i + 1].startCharIdx;
                endIndex = nextLineStartCharIdx - 1;
            }
            else
            {
                // 最后一行的最后字符索引位置
                endIndex = characterCount - 1;
            }

            addUIVertexQuad(vh, line, line.startCharIdx, endIndex, uv0);
        }
    }

    private void addUIVertexQuad(VertexHelper vh, UILineInfo lineInfo, int startIndex, int endIndex, Vector2 uv0)
    {
        if (endIndex < startIndex) return;

        var charInfo = characters[startIndex];
        var startPosX = charInfo.cursorPos.x;
        startPosX = startPosX / pixelsPerUnit; // cursorPos是原始大小下的信息,但文本在不同分辨率下会被进一步缩放处理,所以要将比例带入计算

        charInfo = characters[endIndex];
        var endPosX = charInfo.cursorPos.x;
        endPosX = (endPosX + charInfo.charWidth) / pixelsPerUnit; //最后一个字符位置x是左边，需要加上字符宽度到右边

        var centerY = (lineInfo.topY - (lineInfo.height - lineInfo.leading) * 0.5f) / pixelsPerUnit;

        var halfHeight = lineHeight * 0.5f;

        // 左上
        var pos0 = new Vector3(startPosX, centerY + halfHeight + lineOffset, 0);
        // 右上
        var pos1 = new Vector3(endPosX, centerY + halfHeight + lineOffset, 0);
        // 右下
        var pos2 = new Vector3(endPosX, centerY - halfHeight + lineOffset, 0);
        // 左下
        var pos3 = new Vector3(startPosX, centerY - halfHeight + lineOffset, 0);

        var uiVertexs = UIVertex.simpleVert;
        uiVertexs.uv0 = uv0;
        uiVertexs.color = lineColor;

        uiVertexs.position = pos0;
        lineUIVertexs[0] = uiVertexs;

        uiVertexs.position = pos1;
        lineUIVertexs[1] = uiVertexs;

        uiVertexs.position = pos2;
        lineUIVertexs[2] = uiVertexs;

        uiVertexs.position = pos3;
        lineUIVertexs[3] = uiVertexs;

        vh.AddUIVertexQuad(lineUIVertexs);
    }

    // 获取一个字符索引所在的行
    private int getCharInLineIndex(int charIndex)
    {
        var len = lines.Length - 1;

        // 是否在最后一行
        if (charIndex >= lines[len].startCharIdx && charIndex < characters.Length) return len;

        for (int i = 0; i < len; i++)
        {
            var line = lines[i];
            if (charIndex >= line.startCharIdx && charIndex < lines[i + 1].startCharIdx) return i;
        }

        return -1;
    }

    //根据linkInfoList获取超链接Rects
    private void getCustomLinkInfo(VertexHelper vh)
    {
        UICharInfo _tempVertex;

        var charsMaxIndex = characterCount - 1;

        for (int i = 0; i < linkInfoList.Count; i++)
        {
            var lineInfo = linkInfoList[i];
            var startIndex = lineInfo.StartIndex;
            var endIndex = lineInfo.EndIndex;

            if (startIndex < 0) startIndex = 0;
            if (endIndex > charsMaxIndex) endIndex = charsMaxIndex;

            if (startIndex >= characterCount) continue;

            var lineIndex0 = getCharInLineIndex(startIndex);
            var lineIndex1 = getCharInLineIndex(endIndex);
            if (lineIndex0 != lineIndex1) // 跨行
            {
                for (int j = lineIndex0; j <= lineIndex1; j++)
                {
                    var lineStartCharIndex = startIndex;
                    var lineEndCharIndex = endIndex;
                    if (j == lineIndex0)
                    {
                        // 第一行,从指定起始字索引到改行末尾字索引(改行末尾索引是下一行的起始索引-1得到)
                        lineEndCharIndex = lines[j + 1].startCharIdx - 1;
                    }
                    else if (j == lineIndex1)
                    {
                        // 最后一行,从改行起始字索引到指定字索引
                        lineStartCharIndex = lines[j].startCharIdx;
                    }
                    else
                    {
                        // 中间行,从改行起始字所以到该行末尾字索引
                        lineStartCharIndex = lines[j].startCharIdx;
                        lineEndCharIndex = lines[j + 1].startCharIdx - 1;
                    }

                    //将下划线里面的文本添加一个Rect
                    var line = lines[j];
                    float w = 0;
                    float h = line.height - line.leading;
                    for (int k = lineStartCharIndex; k <= lineEndCharIndex; k++)
                    {
                        _tempVertex = characters[k];
                        w += _tempVertex.charWidth;
                    }

                    _tempVertex = characters[lineStartCharIndex];
                    var rect = new Rect(
                        new Vector2(_tempVertex.cursorPos.x / pixelsPerUnit,
                            (line.topY - line.height + line.leading) / pixelsPerUnit),
                        new Vector2(w / pixelsPerUnit, h / pixelsPerUnit));
                    lineInfo.rect = rect;
                }
            }
            else // 在同一行
            {
                //将下划线里面的文本添加一个Rect
                var line = lines[lineIndex0];
                float w = 0;
                float h = line.height - line.leading;
                for (int k = startIndex; k <= endIndex; k++)
                {
                    _tempVertex = characters[k];
                    w += _tempVertex.charWidth;
                }

                _tempVertex = characters[startIndex];
                var rect = new Rect(
                    new Vector2(_tempVertex.cursorPos.x / pixelsPerUnit,
                        (line.topY - line.height + line.leading) / pixelsPerUnit),
                    new Vector2(w / pixelsPerUnit, h / pixelsPerUnit));
                lineInfo.rect = rect;
            }
        }
    }

    //获取超链接Rects
    private void getAllLinkInfo(VertexHelper vh)
    {
        Info info;
        if (infoPool.Count > 0)
        {
            info = infoPool.Pop();
        }
        else
        {
            info = new Info();
        }

        info.StringValue = text;

        UICharInfo _tempVertex;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var startIndex = line.startCharIdx;
            var endIndex = 0;
            if (i + 1 < lines.Length)
            {
                // 通过下一行的起始索引减1得到这一行最后一个字符索引位置
                var nextLineStartCharIdx = lines[i + 1].startCharIdx;
                endIndex = nextLineStartCharIdx - 1;
            }
            else
            {
                // 最后一行的最后字符索引位置
                endIndex = characterCount - 1;
            }

            //将下划线里面的文本添加一个Rect
            float w = 0;
            float h = line.height - line.leading;
            for (int k = startIndex; k <= endIndex; k++)
            {
                _tempVertex = characters[k];
                w += _tempVertex.charWidth;
            }

            _tempVertex = characters[startIndex];
            var rect = new Rect(
                new Vector2(_tempVertex.cursorPos.x / pixelsPerUnit,
                    (line.topY - line.height + line.leading) / pixelsPerUnit),
                new Vector2(w / pixelsPerUnit, h / pixelsPerUnit));

            info.rect = rect;
        }

        linkInfoList.Add(info);
    }

    public class Info
    {
        /// <summary>
        /// 开始索引值
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// 结束索引值
        /// </summary>
        public int EndIndex;

        /// <summary>
        /// 文本
        /// </summary>
        public string StringValue;

        /// <summary>
        /// 碰撞盒范围
        /// </summary>
        public Rect rect;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        text = _text;
    }

    //辅助线框
    Vector3[] _textWolrdVertexs = new Vector3[4];

    private void OnDrawGizmos()
    {
        if (linkInfoList == null || linkInfoList.Count <= 0)
        {
            return;
        }

        Rect rect = new Rect();
        //href
        for (int i = 0; i < linkInfoList.Count; i++)
        {
            rect = linkInfoList[i].rect;
            _textWolrdVertexs[0] = TransformPoint2World(transform, rect.position);
            _textWolrdVertexs[1] = TransformPoint2World(transform, new Vector3(rect.x + rect.width, rect.y));
            _textWolrdVertexs[2] =
                TransformPoint2World(transform, new Vector3(rect.x + rect.width, rect.y + rect.height));
            _textWolrdVertexs[3] = TransformPoint2World(transform, new Vector3(rect.x, rect.y + rect.height));

            GizmosDrawLine(Color.green, _textWolrdVertexs);
        }
    }

    //划线
    private void GizmosDrawLine(Color32 color, Vector3[] pos)
    {
        Gizmos.color = color;

        Gizmos.DrawLine(pos[0], pos[1]);
        Gizmos.DrawLine(pos[1], pos[2]);
        Gizmos.DrawLine(pos[2], pos[3]);
        Gizmos.DrawLine(pos[3], pos[0]);
    }

    /// <summary>
    /// 获取Transform的世界坐标
    /// </summary>
    /// <param name="rectTransform"></param>
    /// <param name="canvas"></param>
    /// <returns></returns>
    public static Vector3 TransformPoint2World(Transform transform, Vector3 point)
    {
        return transform.localToWorldMatrix.MultiplyPoint(point);
    }
#endif
}