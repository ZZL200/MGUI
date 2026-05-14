using UnityEngine;

public class CircleArray<T>
{
    private T[] array;
    private int head;  //头指针
    private int count;

    public CircleArray(int count)
    {
        array = new T[count];
        head = 0;
        this.count = count;
    }

    // 删除头部并移动到尾部
    public void MoveHeadToTail()
    {
        if (count <= 0) return;

        // 不需要实际移动数据，只需调整头指针
        head = (head + 1) % count;
    }
    
    // 删除尾部并添加到首部
    public void MoveTailToHead()
    {
        if (count <= 0) return;

        // 不需要实际移动数据，只需调整头指针
        head = (head - 1 + count) % count;
    }
    
    public T this[int index]
    {
        get
        {
            if (index<count && index < count)
            {
                return array[(head + index) % count];
            }
            else
            {
                Debug.LogError("get数组越界");
                return default;
            }
        }
        set
        {
            if (index >= 0 && index<count)
            {
                array[(head + index) % count] = value;
            }
            else
            {
                Debug.LogError("set数组越界");
            }
        }
    }
    
    public int Count => count;
    
    public int Length => count;
}