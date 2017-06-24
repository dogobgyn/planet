using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace TwoDee
{
    public class Resistances : MonoBehaviour, IResistance
    {
        public Resistance[] m_Resistances;

        void IResistance.GetResistances(ResistanceData data)
        {
            data.m_Resistances = new List<Resistance>(m_Resistances);
        }
    }

}