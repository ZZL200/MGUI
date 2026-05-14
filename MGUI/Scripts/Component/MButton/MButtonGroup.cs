using System.Collections.Generic;
using UnityEngine;

namespace MGUI
{
    public class MButtonGroup : MonoBehaviour
    {
        [HideInInspector] public List<MButton> buttonGroup;

        [HideInInspector] public MButton currMButton = null;
    }
}